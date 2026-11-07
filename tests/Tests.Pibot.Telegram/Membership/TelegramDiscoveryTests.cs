using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PiBot.Telegram.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Tests.Pibot.Telegram.Membership;

public sealed class TelegramDiscoveryTests
{
    [Fact]
    public async Task RepeatedMessagesDoNotWriteOrRediscoverTheUser()
    {
        await using var testServices = new TelegramTestServices();
        var messageDate = testServices.TimeProvider.GetUtcNow().UtcDateTime;
        var newMemberships = await testServices.MembershipService.TrackMembershipsAsync(CreateMessageUpdate(messageDate));
        newMemberships.Should().ContainSingle();
        var dbContext = testServices.DbContext.Should().BeOfType<InMemoryTelegramContext>().Which;
        var initialSavedEntityCount = dbContext.SavedEntityCount;
        var initialUpdatedAt = await testServices.DbContext.TelegramUserChats.Select(x => x.MembershipUpdatedAt).SingleAsync(TestContext.Current.CancellationToken);

        for (var index = 1; index <= 3; index++)
        {
            var newlyDiscovered = await testServices.MembershipService.TrackMembershipsAsync(CreateMessageUpdate(messageDate.AddMinutes(index)));
            newlyDiscovered.Should().BeEmpty();
        }

        var membership = await testServices.DbContext.TelegramUserChats.SingleAsync(TestContext.Current.CancellationToken);
        dbContext.SavedEntityCount.Should().Be(initialSavedEntityCount);
        membership.MembershipUpdatedAt.Should().Be(initialUpdatedAt);
        testServices.ApiStub.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task ChangedNamesAreSavedWithoutChangingMembershipTimestamp()
    {
        await using var testServices = new TelegramTestServices();
        var messageDate = testServices.TimeProvider.GetUtcNow().UtcDateTime;
        await testServices.MembershipService.TrackMembershipsAsync(CreateMessageUpdate(messageDate));
        var initialUpdatedAt = await testServices.DbContext.TelegramUserChats.Select(x => x.MembershipUpdatedAt).SingleAsync(TestContext.Current.CancellationToken);
        var renamed = CreateMessageUpdate(messageDate.AddMinutes(1));
        renamed.Message!.From = new User { Id = 42, FirstName = "New name", Username = "new_login" };
        await testServices.MembershipService.TrackMembershipsAsync(renamed);

        var user = await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        var membership = await testServices.DbContext.TelegramUserChats.SingleAsync(TestContext.Current.CancellationToken);
        user.FirstName.Should().Be("New name");
        user.Username.Should().Be("new_login");
        membership.MembershipUpdatedAt.Should().Be(initialUpdatedAt);
    }

    [Fact]
    public async Task MessagesDoNotReviveDepartedMembers()
    {
        await using var testServices = new TelegramTestServices();
        var messageDate = testServices.TimeProvider.GetUtcNow().UtcDateTime;
        await testServices.MembershipService.TrackMembershipsAsync(CreateMessageUpdate(messageDate));
        var departure = CreateMessageUpdate(messageDate.AddMinutes(1));
        departure.Message!.From = null;
        departure.Message.LeftChatMember = new User { Id = 42, FirstName = "Member", Username = "member" };
        await testServices.MembershipService.TrackMembershipsAsync(departure);
        await testServices.MembershipService.TrackMembershipsAsync(CreateMessageUpdate(messageDate.AddMinutes(2)));
        var afterMessage = await testServices.DbContext.TelegramUserChats.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        afterMessage.IsMember.Should().BeFalse();

        var rejoin = CreateMessageUpdate(messageDate.AddMinutes(3));
        rejoin.Message!.NewChatMembers = [new User { Id = 42, FirstName = "Member", Username = "member" }];
        await testServices.MembershipService.TrackMembershipsAsync(rejoin);
        var afterRejoin = await testServices.DbContext.TelegramUserChats.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        afterRejoin.IsMember.Should().BeTrue();
    }

    [Fact]
    public async Task MissingBirthdayIsRetriedByTheDailyScanInsteadOfEveryMessage()
    {
        await using var testServices = new TelegramTestServices();
        var originalResponseHandler = testServices.ApiStub.ResponseHandler;
        testServices.ApiStub.ResponseHandler = (method, body) => method == "getChat" && body.GetProperty("chat_id").GetInt64() > 0
            ? TelegramApiStub.CreatePrivateChat() : originalResponseHandler(method, body);
        using var serviceProvider = new ServiceCollection().AddScoped(_ => testServices.MembershipService)
            .AddScoped(_ => testServices.ProfileImporter).BuildServiceProvider();
        var updateHandler = new UpdateHandlingService(NullLogger<UpdateHandlingService>.Instance,
            new CommandHandlingService(NullLogger<CommandHandlingService>.Instance, serviceProvider),
            serviceProvider.GetRequiredService<IServiceScopeFactory>());
        var messageDate = testServices.TimeProvider.GetUtcNow().UtcDateTime;
        for (var index = 0; index < 3; index++)
            await updateHandler.HandleUpdateAsync(testServices.BotClient, CreateMessageUpdate(messageDate.AddMinutes(index)), TestContext.Current.CancellationToken);

        testServices.ApiStub.Requests.Should().ContainSingle();
        await testServices.ProfileImporter.ImportMissingBirthdaysAsync();
        testServices.ApiStub.Requests.Should().HaveCount(2);
    }

    private static Update CreateMessageUpdate(DateTime messageDate) => new()
    {
        Message = new Message
        {
            Chat = new Chat { Id = TelegramTestServices.BirthdayChatId, Type = ChatType.Supergroup },
            From = new User { Id = 42, FirstName = "Member", Username = "member" }, Date = messageDate
        }
    };

    [Fact]
    public async Task SavingKnownUsersUpdatesTheirNamesAndPreservesBirthdaySettings()
    {
        await using var testServices = new TelegramTestServices();
        await testServices.SeedBirthdayUserAsync(manualBirthdayDay: 1, manualBirthdayMonth: 11, notificationsEnabled: false);

        await testServices.MembershipService.SaveUserAsync(
            new User
            {
                Id = TelegramTestServices.BirthdayUserId,
                FirstName = "Updated",
                LastName = "Name",
                Username = "updated_login"
            });

        var user = await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        user.FirstName.Should().Be("Updated");
        user.LastName.Should().Be("Name");
        user.Username.Should().Be("updated_login");
        user.ManualBirthdayDay.Should().Be(1);
        user.ManualBirthdayMonth.Should().Be(11);
        user.ProfileBirthdayDay.Should().Be(29);
        user.ProfileBirthdayMonth.Should().Be(10);
        user.BirthdayNotificationsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task MembershipChangesUseProcessingTimeInUtc()
    {
        await using var testServices = new TelegramTestServices();
        var beforeUpdate = DateTime.UtcNow;

        await testServices.MembershipService.TrackMembershipsAsync(CreateMessageUpdate(DateTime.UnixEpoch));

        var afterUpdate = DateTime.UtcNow;
        var membership = await testServices.DbContext.TelegramUserChats.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        membership.IsMember.Should().BeTrue();
        membership.MembershipUpdatedAt.Should().NotBeNull();
        var updatedAt = membership.MembershipUpdatedAt.GetValueOrDefault();
        updatedAt.Kind.Should().Be(DateTimeKind.Utc);
        updatedAt.Should().BeOnOrAfter(beforeUpdate).And.BeOnOrBefore(afterUpdate);
    }
}
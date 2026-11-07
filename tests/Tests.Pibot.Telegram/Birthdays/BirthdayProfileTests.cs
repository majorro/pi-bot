using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PiBot.Telegram.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Tests.Pibot.Telegram.Birthdays;

public sealed class BirthdayProfileTests
{
    [Fact]
    public async Task ProfileImportIsFillOnlyAndKeepsManualOverrideAndOldProfile()
    {
        await using var testServices = new TelegramTestServices();
        await testServices.SeedBirthdayUserAsync(manualBirthdayDay: 1, manualBirthdayMonth: 11, profileBirthdayDay: null, profileBirthdayMonth: null);
        await testServices.ProfileImporter.ImportMissingBirthdaysAsync();
        var user = await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        user.ManualBirthdayDay.Should().Be(1);
        user.ProfileBirthdayDay.Should().Be(29);
        user.ProfileBirthdayMonth.Should().Be(10);
        var originalResponseHandler = testServices.ApiStub.ResponseHandler;
        foreach (var profileResponse in new object[]
        {
            TelegramApiStub.CreatePrivateChat(birthdate: new { day = 1, month = 12 }),
            TelegramApiStub.CreatePrivateChat(), new TelegramApiStub.ApiError(400)
        })
        {
            testServices.ApiStub.ResponseHandler = (method, body) => method == "getChat" && body.GetProperty("chat_id").GetInt64() > 0 ? profileResponse : originalResponseHandler(method, body);
            await testServices.ProfileImporter.ImportMissingBirthdaysAsync();
            var retained = await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
            retained.ProfileBirthdayDay.Should().Be(29);
            retained.ProfileBirthdayMonth.Should().Be(10);
            retained.ManualBirthdayDay.Should().Be(1);
        }
        testServices.ApiStub.Requests.Should().ContainSingle(call => call.Method == "getChat" && call.Body.GetProperty("chat_id").GetInt64() > 0);
    }

    [Fact]
    public async Task MissingOrInaccessibleProfileStaysEmptyAndCanBeDiscoveredLater()
    {
        await using var testServices = new TelegramTestServices();
        await testServices.SeedBirthdayUserAsync(profileBirthdayDay: null, profileBirthdayMonth: null);
        var originalResponseHandler = testServices.ApiStub.ResponseHandler;
        foreach (var profileResponse in new object[] { TelegramApiStub.CreatePrivateChat(), new TelegramApiStub.ApiError(400) })
        {
            testServices.ApiStub.ResponseHandler = (method, body) => method == "getChat" && body.GetProperty("chat_id").GetInt64() > 0 ? profileResponse : originalResponseHandler(method, body);
            await testServices.ProfileImporter.ImportMissingBirthdaysAsync();
            (await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).ProfileBirthdayDay.Should().BeNull();
        }
        testServices.ApiStub.ResponseHandler = originalResponseHandler;
        await testServices.ProfileImporter.ImportMissingBirthdaysAsync();
        (await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).ProfileBirthdayDay.Should().Be(29);
    }

    [Fact]
    public async Task NewParticipantsAreImportedImmediatelyThroughUpdateHandling()
    {
        await using var testServices = new TelegramTestServices();
        using var serviceProvider = new ServiceCollection().AddScoped(_ => testServices.MembershipService).AddScoped(_ => testServices.ProfileImporter).BuildServiceProvider();
        var updateHandler = new UpdateHandlingService(Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateHandlingService>.Instance,
            new CommandHandlingService(Microsoft.Extensions.Logging.Abstractions.NullLogger<CommandHandlingService>.Instance, serviceProvider),
            serviceProvider.GetRequiredService<IServiceScopeFactory>());
        await updateHandler.HandleUpdateAsync(testServices.BotClient, new Update
        {
            Message = new Message
            {
                Chat = new Chat { Id = TelegramTestServices.BirthdayChatId, Type = ChatType.Supergroup },
                From = new User { Id = 42, FirstName = "Member" }, Date = testServices.TimeProvider.GetUtcNow().UtcDateTime
            }
        }, TestContext.Current.CancellationToken);
        var user = await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        user.ProfileBirthdayDay.Should().Be(29);
        user.ProfileBirthdayMonth.Should().Be(10);
        testServices.ApiStub.Requests.Should().ContainSingle(x => x.Method == "getChat" && x.Body.GetProperty("chat_id").GetInt64() == 42);
    }

    [Fact]
    public async Task KnownProfilesAreImportedWithoutAnUnrelatedMembershipLookup()
    {
        await using var testServices = new TelegramTestServices();
        await testServices.SeedBirthdayUserAsync(profileBirthdayDay: null, profileBirthdayMonth: null);
        var originalResponseHandler = testServices.ApiStub.ResponseHandler;
        testServices.ApiStub.ResponseHandler = (method, body) => method == "getChatMember"
            ? new TelegramApiStub.ApiError(400) : originalResponseHandler(method, body);
        await testServices.ProfileImporter.ImportMissingBirthdaysAsync();
        (await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).ProfileBirthdayDay.Should().Be(29);
        testServices.ApiStub.Requests.Should().NotContain(x => x.Method == "getChatMember");
    }
}
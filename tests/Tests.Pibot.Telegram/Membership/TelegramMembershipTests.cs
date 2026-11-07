using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PiBot.Telegram.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Tests.Pibot.Telegram.Membership;

public sealed class TelegramMembershipTests
{
    [Fact]
    public async Task MessagesSetCommunityNameOnlyWhenCommunityIsCreated()
    {
        await using var testServices = new TelegramTestServices();
        var chat = new Chat { Id = TelegramTestServices.BirthdayChatId, Type = ChatType.Supergroup, Title = "Original name" };
        var user = new User { Id = 42, FirstName = "Member" };
        var message = new Message { Chat = chat, From = user, Date = DateTime.UtcNow };
        await testServices.MembershipService.TrackMembershipsAsync(new Update { Message = message });
        (await testServices.DbContext.TelegramChats.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).Name.Should().Be("Original name");
        var originalUpdatedAt = await testServices.DbContext.TelegramUserChats
            .Select(x => x.MembershipUpdatedAt).SingleAsync(TestContext.Current.CancellationToken);

        message.Chat.Title = "Renamed group";
        message.NewChatTitle = "Renamed group";
        await testServices.MembershipService.TrackMembershipsAsync(new Update { Message = message });
        await testServices.MembershipService.UpdateMembershipAsync(new User { Id = 43, FirstName = "New member" }, chat.Id, true, "Renamed group");

        var storedChat = await testServices.DbContext.TelegramChats.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        storedChat.Name.Should().Be("Original name");
        var membership = await testServices.DbContext.TelegramUserChats.AsNoTracking().SingleAsync(x => x.TelegramUserId == 42, TestContext.Current.CancellationToken);
        membership.MembershipUpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public async Task UpdaterDiscoversNontextMessagesAndTracksLeaveRejoinWithoutDeletingRows()
    {
        await using var testServices = new TelegramTestServices();
        var chat = new Chat { Id = TelegramTestServices.BirthdayChatId, Type = ChatType.Supergroup };
        var user = new User { Id = 42, FirstName = "Member", Username = "old_name" };
        var membershipObservedAt = testServices.TimeProvider.GetUtcNow().UtcDateTime;
        await testServices.MembershipService.TrackMembershipsAsync(new Update
        {
            Message = new Message { Chat = chat, From = user, Date = membershipObservedAt }
        });
        (await testServices.DbContext.TelegramUserChats.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).IsMember.Should().BeTrue();
        await testServices.MembershipService.TrackMembershipsAsync(new Update
        {
            Message = new Message { Chat = chat, LeftChatMember = user, Date = membershipObservedAt.AddSeconds(2) }
        });
        (await testServices.DbContext.TelegramUserChats.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).IsMember.Should().BeFalse();
        // Delayed pending messages must not resurrect a departed membership.
        await testServices.MembershipService.TrackMembershipsAsync(new Update
        {
            Message = new Message { Chat = chat, From = user, Date = membershipObservedAt.AddSeconds(1) }
        });
        (await testServices.DbContext.TelegramUserChats.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).IsMember.Should().BeFalse();
        await testServices.MembershipService.TrackMembershipsAsync(new Update
        {
            ChatMember = new ChatMemberUpdated
            {
                Chat = chat, From = user, Date = membershipObservedAt.AddSeconds(3),
                OldChatMember = new ChatMemberLeft { User = user },
                NewChatMember = new ChatMemberMember { User = new User { Id = 42, FirstName = "New Name", Username = "new_name" } }
            }
        });
        (await testServices.DbContext.TelegramUserChats.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).IsMember.Should().BeTrue();
        (await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).Username.Should().Be("new_name");
    }

    [Fact]
    public async Task UpdaterIgnoresBotsAndAnonymousSenders()
    {
        await using var testServices = new TelegramTestServices();
        var user = new User { Id = 42, FirstName = "Member" };
        await testServices.MembershipService.TrackMembershipsAsync(new Update
        {
            Message = new Message { Chat = new Chat { Id = -999, Type = ChatType.Channel }, From = user, Date = DateTime.UtcNow }
        });
        await testServices.MembershipService.TrackMembershipsAsync(new Update
        {
            Message = new Message
            {
                Chat = new Chat { Id = TelegramTestServices.BirthdayChatId, Type = ChatType.Supergroup },
                From = user, SenderChat = new Chat { Id = -888, Type = ChatType.Channel }, Date = DateTime.UtcNow
            }
        });
        await testServices.MembershipService.TrackMembershipsAsync(new Update
        {
            Message = new Message
            {
                Chat = new Chat { Id = TelegramTestServices.BirthdayChatId, Type = ChatType.Supergroup },
                From = new User { Id = 999, IsBot = true, FirstName = "Bot" }, Date = DateTime.UtcNow
            }
        });
        (await testServices.DbContext.TelegramUsers.ToListAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
    }

    [Theory]
    [InlineData(-999, ChatType.Supergroup)]
    [InlineData(42, ChatType.Private)]
    public async Task UpdateHandlingRegistersMembershipOutsideBirthdayGroups(long chatId, ChatType type)
    {
        await using var testServices = new TelegramTestServices();
        using var serviceProvider = new ServiceCollection().AddScoped(_ => testServices.MembershipService).AddScoped(_ => testServices.ProfileImporter).BuildServiceProvider();
        var commandHandler = new CommandHandlingService(Microsoft.Extensions.Logging.Abstractions.NullLogger<CommandHandlingService>.Instance, serviceProvider);
        var updateHandler = new UpdateHandlingService(Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateHandlingService>.Instance,
            commandHandler, serviceProvider.GetRequiredService<IServiceScopeFactory>());
        await updateHandler.HandleUpdateAsync(testServices.BotClient, new Update
        {
            Message = new Message
            {
                Chat = new Chat { Id = chatId, Type = type },
                From = new User { Id = 42, FirstName = "Member" }, Date = testServices.TimeProvider.GetUtcNow().UtcDateTime
            }
        }, TestContext.Current.CancellationToken);
        var membership = await testServices.DbContext.TelegramUserChats.SingleAsync(TestContext.Current.CancellationToken);
        membership.IsMember.Should().BeTrue();
        membership.TelegramChatId.Should().Be(chatId);
        testServices.ApiStub.Requests.Should().BeEmpty();

        // Grow consumes registration performed by update handling, including in private chats.
        var grow = new PiBot.Telegram.Modules.GrowModule(testServices.BotClient, testServices.DbContext,
            new PiBot.Core.CommandHandlers.GrowHandler(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<PiBot.Core.CommandHandlers.GrowHandler>.Instance, testServices.DbContext))
        {
            Context = new PiBot.Telegram.Modules.ModuleContext(new User { Id = 42, FirstName = "Member" },
                new Chat { Id = chatId, Type = type }, null)
        };
        await grow.ExecuteAsync();
        (await testServices.DbContext.Thingies.SingleAsync(TestContext.Current.CancellationToken)).UserCommunityId.Should().Be(membership.Id);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PiBot.Telegram;
using PiBot.Telegram.Modules;
using PiBot.Telegram.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Tests.Pibot.Telegram.Membership;

public sealed class TelegramForwardingTests
{
    [Theory]
    [InlineData("/birthday set 7 29.10")]
    [InlineData(null)]
    public async Task AdminDmForwardsIdentifyOriginalSenderWithoutExecutingTheirText(string? text)
    {
        await using var testServices = new TelegramTestServices();
        using var serviceProvider = CreateServiceProvider(testServices);
        var updateHandler = CreateUpdateHandler(serviceProvider);
        var forwardedMessage = CreateForwardedMessage(
            new MessageOriginUser { SenderUser = new User { Id = 42, FirstName = "Original sender", Username = "birthday_user" } },
            text: text);

        await updateHandler.HandleUpdateAsync(testServices.BotClient, new Update { Message = forwardedMessage }, TestContext.Current.CancellationToken);

        var user = await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(x => x.TelegramId == 42, TestContext.Current.CancellationToken);
        user.FirstName.Should().Be("Original sender");
        user.Username.Should().Be("birthday_user");
        (await testServices.DbContext.TelegramUserChats.AnyAsync(x => x.TelegramUserId == 42, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await testServices.DbContext.TelegramUsers.ToListAsync(TestContext.Current.CancellationToken)).Should().OnlyContain(x => x.ManualBirthdayDay == null);
        var response = testServices.ApiStub.Requests.Should().ContainSingle(x => x.Method == "sendMessage").Which;
        response.Body.GetProperty("text").GetString().Should().Contain("@birthday_user (42)");
    }

    [Fact]
    public async Task ReplyToDmForwardSetsOriginalSendersBirthdayAndRegistersTheirMembership()
    {
        await using var testServices = new TelegramTestServices();
        using var serviceProvider = CreateServiceProvider(testServices);
        var updateHandler = CreateUpdateHandler(serviceProvider);
        var forwardedMessage = CreateForwardedMessage(
            new MessageOriginUser { SenderUser = new User { Id = 42, FirstName = "Original sender", Username = "birthday_user" } });
        await updateHandler.HandleUpdateAsync(testServices.BotClient, new Update { Message = forwardedMessage }, TestContext.Current.CancellationToken);

        await updateHandler.HandleUpdateAsync(testServices.BotClient, new Update
        {
            Message = new Message
            {
                Chat = forwardedMessage.Chat,
                From = forwardedMessage.From,
                ReplyToMessage = forwardedMessage,
                Text = "/birthday set 29.10",
                Date = DateTime.UtcNow
            }
        }, TestContext.Current.CancellationToken);

        var user = await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(x => x.TelegramId == 42, TestContext.Current.CancellationToken);
        user.ManualBirthdayDay.Should().Be(29);
        user.ManualBirthdayMonth.Should().Be(10);
        var admin = await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(x => x.TelegramId == TelegramTestServices.AdminUserId, TestContext.Current.CancellationToken);
        admin.ManualBirthdayDay.Should().BeNull();
        var membership = await testServices.DbContext.TelegramUserChats.AsNoTracking()
            .SingleAsync(x => x.TelegramUserId == 42, TestContext.Current.CancellationToken);
        membership.TelegramChatId.Should().Be(TelegramTestServices.BirthdayChatId);
        membership.IsMember.Should().BeTrue();
        var shown = await testServices.CreateBirthdayModule().ExecuteAsync(args: ["show", "@birthday_user"]);
        shown.Text.Should().Contain("Manual: 29.10");
    }

    [Fact]
    public async Task HiddenForwardDoesNotUseForwardersIdentity()
    {
        await using var testServices = new TelegramTestServices();
        using var serviceProvider = CreateServiceProvider(testServices);
        var updateHandler = CreateUpdateHandler(serviceProvider);
        var forwardedMessage = CreateForwardedMessage(new MessageOriginHiddenUser { SenderUserName = "Hidden sender" });

        await updateHandler.HandleUpdateAsync(testServices.BotClient, new Update { Message = forwardedMessage }, TestContext.Current.CancellationToken);
        testServices.ApiStub.Requests.Should().ContainSingle(x => x.Method == "sendMessage" && x.Body.GetProperty("text").GetString()!.Contains("doesn't identify a person"));
        await updateHandler.HandleUpdateAsync(testServices.BotClient, new Update
        {
            Message = new Message
            {
                Chat = forwardedMessage.Chat,
                From = forwardedMessage.From,
                ReplyToMessage = forwardedMessage,
                Text = "/birthday set 29.10",
                Date = DateTime.UtcNow
            }
        }, TestContext.Current.CancellationToken);

        var user = await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        user.TelegramId.Should().Be(TelegramTestServices.AdminUserId);
        user.ManualBirthdayDay.Should().BeNull();
    }

    [Theory]
    [InlineData(8, 8)]
    [InlineData(TelegramTestServices.AdminUserId, TelegramTestServices.BirthdayChatId)]
    public async Task IdentificationOnlyAcceptsAdminDmForwards(long senderId, long chatId)
    {
        await using var testServices = new TelegramTestServices();
        using var serviceProvider = CreateServiceProvider(testServices);
        var updateHandler = CreateUpdateHandler(serviceProvider);
        var forwardedMessage = CreateForwardedMessage(
            new MessageOriginUser { SenderUser = new User { Id = 42, FirstName = "Original sender" } },
            senderId,
            chatId);

        await updateHandler.HandleUpdateAsync(testServices.BotClient, new Update { Message = forwardedMessage }, TestContext.Current.CancellationToken);

        (await testServices.DbContext.TelegramUsers.AnyAsync(x => x.TelegramId == 42, TestContext.Current.CancellationToken)).Should().BeFalse();
        testServices.ApiStub.Requests.Should().NotContain(x => x.Method == "sendMessage");
    }

    private static Message CreateForwardedMessage(
        MessageOrigin origin,
        long senderId = TelegramTestServices.AdminUserId,
        long chatId = TelegramTestServices.AdminUserId,
        string? text = "Forwarded message") => new()
    {
        Id = 10,
        Chat = new Chat { Id = chatId, Type = chatId < 0 ? ChatType.Supergroup : ChatType.Private },
        From = new User { Id = senderId, FirstName = "Forwarding user" },
        ForwardOrigin = origin,
        Text = text,
        Date = DateTime.UtcNow
    };

    private static ServiceProvider CreateServiceProvider(TelegramTestServices testServices)
    {
        var botConfiguration = Options.Create(new BotConfiguration { AdminUserIds = [TelegramTestServices.AdminUserId] });
        return new ServiceCollection()
            .AddSingleton(botConfiguration)
            .AddScoped(_ => testServices.MembershipService)
            .AddScoped(_ => testServices.ProfileImporter)
            .AddKeyedScoped<ModuleBase>("birthday", (_, _) => new BirthdayModule(
                testServices.BotClient,
                testServices.DbContext,
                testServices.TargetResolver,
                testServices.BirthdaySender,
                testServices.MembershipService,
                botConfiguration,
                testServices.BirthdayConfiguration,
                testServices.TimeProvider))
            .BuildServiceProvider();
    }

    private static UpdateHandlingService CreateUpdateHandler(IServiceProvider serviceProvider) => new(
        NullLogger<UpdateHandlingService>.Instance,
        new CommandHandlingService(NullLogger<CommandHandlingService>.Instance, serviceProvider),
        serviceProvider.GetRequiredService<IServiceScopeFactory>());
}
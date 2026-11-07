using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PiBot.Data.Telegram;
using PiBot.Telegram;
using PiBot.Telegram.Modules;
using PiBot.Telegram.Services;
using PiBot.Telegram.Services.Birthdays;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Tests.Pibot.Telegram.Infrastructure;

internal sealed class TelegramTestServices : IAsyncDisposable
{
    public const long AdminUserId = 7;
    public const long BirthdayUserId = 42;
    public const long BirthdayChatId = -100123;
    public FakeTimeProvider TimeProvider { get; } = new(new DateTimeOffset(2026, 10, 28, 21, 0, 0, TimeSpan.Zero));
    public TelegramApiStub ApiStub { get; } = new();
    public TelegramPgContext DbContext { get; }
    public TelegramMembershipService MembershipService { get; }
    public BirthdayTargetResolver TargetResolver { get; }
    public BirthdaySender BirthdaySender { get; }
    public ITelegramBotClient BotClient { get; }
    public BirthdayProfileImporter ProfileImporter { get; }
    public IOptions<BirthdayConfiguration> BirthdayConfiguration { get; }
    public Action<long>? BeforeBirthdayMessageSent { get; set; }
    public HashSet<long> ChatsUserHasLeft { get; } = [];
    public List<(long ChatId, string Text)> BirthdayMessages { get; } = [];

    public TelegramTestServices(params long[] chatIds) : this(new InMemoryTelegramContext(), chatIds) { }

    public TelegramTestServices(TelegramPgContext dbContext, params long[] chatIds)
    {
        DbContext = dbContext;
        BotClient = ApiStub.CreateBotClient();
        BirthdayConfiguration = Options.Create(new BirthdayConfiguration { AllowedChatIds = chatIds.Length == 0 ? [BirthdayChatId] : chatIds });
        MembershipService = new TelegramMembershipService(DbContext, BotClient, NullLogger<TelegramMembershipService>.Instance);
        TargetResolver = new BirthdayTargetResolver(DbContext, MembershipService, BotClient, BirthdayConfiguration);
        ProfileImporter = new BirthdayProfileImporter(DbContext, BotClient, BirthdayConfiguration, NullLogger<BirthdayProfileImporter>.Instance);
        BirthdaySender = new BirthdaySender(BotClient, DbContext, MembershipService, BirthdayConfiguration, TimeProvider, NullLogger<BirthdaySender>.Instance);
        ApiStub.ResponseHandler = (apiMethodName, requestBody) => apiMethodName switch
        {
            "getMe" => new { id = 999, is_bot = true, first_name = "PiBot", username = "test_bot" },
            "getChatMember" when requestBody.GetProperty("user_id").GetInt64() == 999 => TelegramApiStub.CreateChatMember(999, "administrator"),
            "getChatMember" => TelegramApiStub.CreateChatMember(requestBody.GetProperty("user_id").GetInt64(),
                ChatsUserHasLeft.Contains(requestBody.GetProperty("chat_id").GetInt64()) ? "left" : "member"),
            "getChat" when requestBody.GetProperty("chat_id").GetInt64() < 0 => TelegramApiStub.CreateGroupChat(requestBody.GetProperty("chat_id").GetInt64()),
            "getChat" => TelegramApiStub.CreatePrivateChat(requestBody.GetProperty("chat_id").GetInt64(), birthdate: new { day = 29, month = 10 }),
            "sendMessage" => CreateSendMessageResponse(requestBody),
            _ => throw new InvalidOperationException($"Unexpected Telegram method {apiMethodName}")
        };
    }

    private object CreateSendMessageResponse(System.Text.Json.JsonElement requestBody)
    {
        var chatId = requestBody.GetProperty("chat_id").GetInt64();
        var messageText = requestBody.GetProperty("text").GetString()!;
        if (chatId < 0)
        {
            BeforeBirthdayMessageSent?.Invoke(chatId);
            BirthdayMessages.Add((chatId, messageText));
        }
        return TelegramApiStub.CreateMessage(chatId, messageText);
    }

    public BirthdayModule CreateBirthdayModule(long commandUserId = AdminUserId, long chatId = AdminUserId, User? replyToUser = null)
    {
        return new BirthdayModule(BotClient, DbContext, TargetResolver, BirthdaySender, MembershipService,
            Options.Create(new BotConfiguration { AdminUserIds = [AdminUserId] }), BirthdayConfiguration, TimeProvider)
        {
            Context = new ModuleContext(new User { Id = commandUserId, FirstName = "Admin" },
                new Chat { Id = chatId, Type = chatId < 0 ? ChatType.Supergroup : ChatType.Private }, replyToUser)
        };
    }

    public async Task SeedBirthdayUserAsync(int? manualBirthdayDay = null, int? manualBirthdayMonth = null, int? profileBirthdayDay = 29, int? profileBirthdayMonth = 10, bool notificationsEnabled = true)
    {
        await MembershipService.UpdateMembershipAsync(new User { Id = BirthdayUserId, FirstName = "Birthday <User>", Username = "birthday_user" },
            BirthdayChatId, true);
        var user = await DbContext.TelegramUsers.SingleAsync(x => x.TelegramId == BirthdayUserId);
        user.ManualBirthdayDay = manualBirthdayDay;
        user.ManualBirthdayMonth = manualBirthdayMonth;
        user.ProfileBirthdayDay = profileBirthdayDay;
        user.ProfileBirthdayMonth = profileBirthdayMonth;
        user.BirthdayNotificationsEnabled = notificationsEnabled;
        await DbContext.SaveChangesAsync();
    }

    public ValueTask DisposeAsync() => DbContext.DisposeAsync();
}
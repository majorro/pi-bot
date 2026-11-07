using PiBot.Core.Configuration;
using PiBot.Telegram.Modules;
using PiBot.Telegram.Services.Birthdays;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PiBot.Telegram.Services;

internal class UpdateHandlingService(
    ILogger<UpdateHandlingService> logger,
    CommandHandlingService commandHandlingService,
    IServiceScopeFactory scopeFactory)
    : IUpdateHandler
{
    private ITelegramBotClient _botClient = null!;

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken = default)
    {
        _botClient = botClient;

        logger.LogError("{Exception}", exception);
        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken = default)
    {
        _botClient = botClient;
        cancellationToken.ThrowIfCancellationRequested();
        await OnUpdate(update);
        await (update switch
        {
            { Message: { } message } => OnMessage(message),
            { ChatMember: { } chatMember } => OnChatMember(chatMember),
            _ => UnknownUpdateHandlerAsync(update)
        });
    }

    private async Task OnUpdate(Update update)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var membershipService = scope.ServiceProvider.GetRequiredService<TelegramMembershipService>();
        var birthdayImportingService = scope.ServiceProvider.GetRequiredService<BirthdayProfileImporter>();

        var newMemberships = await membershipService.TrackMembershipsAsync(update);
        await birthdayImportingService.ImportBirthdaysForNewMembersAsync(newMemberships);
    }

    private async Task OnMessage(Message msg)
    {
        if (msg.ForwardOrigin is { } forwardOrigin)
        {
            if (msg.Chat.Type != ChatType.Private || msg.From is not { IsBot: false } forwardingUser || msg.SenderChat is not null)
                return;

            await using var scope = scopeFactory.CreateAsyncScope();
            if (!scope.ServiceProvider.GetConfiguration<BotConfiguration>().AdminUserIds.Contains(forwardingUser.Id))
                return;

            if (forwardOrigin is not MessageOriginUser { SenderUser: { IsBot: false } originalSender })
            {
                await _botClient.SendMessage(msg.Chat, "This forward doesn't identify a person. If Telegram hides the sender, reply to their message in an allowed group instead.");
                return;
            }

            var membershipService = scope.ServiceProvider.GetRequiredService<TelegramMembershipService>();
            var user = await membershipService.SaveUserAsync(originalSender);
            var displayName = string.IsNullOrEmpty(user.Username) ? user.FullName : $"@{user.Username}";
            await _botClient.SendMessage(
                chatId: msg.Chat,
                text: $"User: {displayName} ({user.TelegramId})\nReply to the forwarded message with /birthday set DD.MM.",
                replyParameters: new ReplyParameters { MessageId = msg.Id });
            return;
        }

        if (msg.Text is not { } messageText)
            return;

        if (messageText.StartsWith('/'))
        {
            if (msg.From is not { IsBot: false } || msg.SenderChat is not null)
                return;

            var success = true;

            var replyToUser = msg.ReplyToMessage switch
            {
                { ForwardOrigin: MessageOriginUser origin } => origin.SenderUser,
                { ForwardOrigin: null, SenderChat: null, From: { } repliedUser } => repliedUser,
                _ => null
            };
            var ctx = new ModuleContext(msg.From, msg.Chat, replyToUser);
            try
            {
                await commandHandlingService.HandleCommandAsync(messageText[1..], ctx);
            }
            catch
            {
                await _botClient.SendMessage(msg.Chat, "@majorro228");
                await _botClient.SendMessage(msg.Chat, "https://media1.tenor.com/m/GWvoxXpr7VsAAAAC/%D0%BC%D1%8B-%D0%B2-%D0%B4%D0%B5%D1%80%D1%8C%D0%BC%D0%B5.gif");
                success = false;
                throw;
            }
            finally
            {
                using var _ = logger.BeginScope(ctx.Meta);
                logger.LogInformation("[{Chat}] [{User}] [{Command}]: {Result}", msg.Chat, msg.From!, messageText, success ? "Success" : "Fail");
            }
        }
    }

    private Task OnChatMember(ChatMemberUpdated chatMember)
    {
        return Task.CompletedTask;
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        logger.LogWarning("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}
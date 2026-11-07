using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PiBot.Data.Telegram;
using PiBot.Data.Telegram.Models;
using PiBot.Telegram.Services;
using PiBot.Telegram.Services.Birthdays;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PiBot.Telegram.Modules;

internal sealed class BirthdayModule(
    ITelegramBotClient botClient,
    TelegramPgContext dbContext,
    BirthdayTargetResolver resolver,
    BirthdaySender sender,
    TelegramMembershipService membership,
    IOptions<BotConfiguration> botConfiguration,
    IOptions<BirthdayConfiguration> birthdayConfiguration,
    TimeProvider clock) : ModuleBase(botClient)
{
    public override string CommandName => "birthday";

    // Module discovery constructs modules without dependencies to read command metadata.
    [UsedImplicitly] private BirthdayModule() : this(null!, null!, null!, null!, null!, null!, null!, null!) { }

    private const string UsageMessage = "Usage: /birthday set <userId|@login> DD.MM; show [userId|@login]; clear|off|on|trigger <userId|@login>. Replying to a user's message or their forwarded message replaces the user argument.";

    public override async Task<Message> ExecuteAsync(string subCommand = "", params string[] args)
    {
        if (Context.User.IsBot || !botConfiguration.Value.AdminUserIds.Contains(Context.User.Id))
            return await ReplyAsync("This command is restricted to bot admins.");

        var operation = args.FirstOrDefault()?.ToLowerInvariant();
        var isSetOp = operation == "set";
        if (Context.Chat.Type != ChatType.Private && !(IsBirthdayGroup() && isSetOp))
            return await ReplyAsync("Use birthday commands in a private message to the bot. Only set is available in allowed groups.");
        if (operation is not ("set" or "clear" or "show" or "off" or "on" or "trigger"))
            return await ReplyAsync(UsageMessage);
        if (operation == "show" && args.Length == 1 && Context.ReplyToUser is null)
            return await ShowUpcomingBirthdaysAsync();

        var hasTargetArg = args.Length == (isSetOp ? 3 : 2);
        var usesReply = args.Length == (isSetOp ? 2 : 1) && Context.ReplyToUser is not null;
        if (!hasTargetArg && !usesReply)
            return await ReplyAsync(UsageMessage);

        DateOnly date = default;
        if (isSetOp && !BirthdayCalendarHelper.TryParseBirthdayDate(args[^1], out date))
            return await ReplyAsync("Invalid birthday. Use DD.MM, for example 29.02.");

        var targetUser = await resolver.ResolveUserAsync(hasTargetArg ? args[1] : null, Context.ReplyToUser, CancellationToken.None);
        if (targetUser is null)
            return await ReplyAsync(hasTargetArg && !long.TryParse(args[1], out _)
                ? "Username could not be resolved. Telegram cannot look up an unseen user by username. Use a numeric Telegram user ID, reply to their message in an allowed group, or reply to their forwarded message in the bot's DM."
                : "User could not be verified. Use a numeric Telegram user ID, reply to their message in an allowed group, or reply to their forwarded message in the bot's DM.");

        if (isSetOp && IsBirthdayGroup())
        {
            var chatMember = await membership.RefreshChatMembershipAsync(Context.Chat.Id, targetUser.TelegramId);
            if (chatMember is null || !TelegramMembershipService.IsMember(chatMember))
                return await ReplyAsync($"{FormatUser(targetUser)} must be a current member of this allowed group, and the bot must be an administrator.");
        }

        return operation switch
        {
            "set" => await SetManualBirthdayAsync(targetUser, date),
            "clear" => await ClearManualBirthdayAsync(targetUser),
            "off" or "on" => await SetNotificationsEnabledAsync(targetUser, operation == "on"),
            "show" => await ReplyAsync(FormatBirthdayDetails(targetUser)),
            "trigger" => await SendBirthdayMessagesAsync(targetUser),
            _ => throw new InvalidOperationException("Unknown birthday operation")
        };
    }

    private async Task<Message> SetManualBirthdayAsync(TelegramUser user, DateOnly date)
    {
        user.ManualBirthdayDay = date.Day;
        user.ManualBirthdayMonth = date.Month;
        await dbContext.SaveChangesAsync();
        if (Context.Chat.Type == ChatType.Private)
        {
            foreach (var chatId in birthdayConfiguration.Value.AllowedChatIds.Distinct())
                await membership.RefreshChatMembershipAsync(chatId, user.TelegramId);
        }
        return await ReplyAsync($"Manual birthday for {FormatUser(user)}: {date:dd.MM}.");
    }

    private async Task<Message> ClearManualBirthdayAsync(TelegramUser user)
    {
        user.ManualBirthdayDay = null;
        user.ManualBirthdayMonth = null;
        await dbContext.SaveChangesAsync();
        return await ReplyAsync($"Manual birthday for {FormatUser(user)} cleared. Profile fallback: {FormatBirthdayDate(user.ProfileBirthdayDay, user.ProfileBirthdayMonth)}.");
    }

    private async Task<Message> SetNotificationsEnabledAsync(TelegramUser user, bool enabled)
    {
        user.BirthdayNotificationsEnabled = enabled;
        await dbContext.SaveChangesAsync();
        return await ReplyAsync($"Birthday notifications for {FormatUser(user)}: {(enabled ? "on" : "off")}.");
    }

    private async Task<Message> SendBirthdayMessagesAsync(TelegramUser user)
    {
        if (!user.BirthdayNotificationsEnabled)
            return await ReplyAsync($"Birthday notifications are disabled for {FormatUser(user)}.");
        var today = BirthdayCalendarHelper.GetTodayInMoscow(clock);
        if (!BirthdayCalendarHelper.IsBirthdayOnDate(user, today))
            return await ReplyAsync($"The effective birthday of {FormatUser(user)} is not today in Moscow.");
        await sender.SendBirthdayMessagesAsync(user.TelegramId);
        return await ReplyAsync($"Sent birthday messages for {FormatUser(user)} across allowed groups.");
    }

    private async Task<Message> ShowUpcomingBirthdaysAsync()
    {
        var chatIds = birthdayConfiguration.Value.AllowedChatIds.ToList();
        var users = await dbContext.TelegramUsers.AsNoTracking()
            .Where(user => (user.ManualBirthdayDay != null || user.ProfileBirthdayDay != null)
                && dbContext.TelegramUserChats.Any(member => member.TelegramUserId == user.TelegramId
                    && chatIds.Contains(member.TelegramChatId) && member.IsMember != false))
            .ToListAsync();
        var today = BirthdayCalendarHelper.GetTodayInMoscow(clock);
        var lines = users.Select(user => (User: user, Date: BirthdayCalendarHelper.GetNextBirthdayDate(user, today)))
            .OrderBy(item => item.Date).ThenBy(item => item.User.TelegramId)
            .Select((item, index) => $"{index + 1}. {item.Date:dd.MM.yyyy} — {FormatUser(item.User)}{(item.User.BirthdayNotificationsEnabled ? "" : " [off]")}")
            .ToList();
        if (lines.Count == 0)
            return await ReplyAsync("No known birthdays in allowed groups.");
        var text = "";
        foreach (var line in lines)
        {
            if (text.Length + line.Length + 1 > 4000)
            {
                await ReplyAsync(text);
                text = "";
            }
            text += (text.Length == 0 ? "" : "\n") + line;
        }
        return await ReplyAsync(text);
    }

    private static string FormatBirthdayDetails(TelegramUser user) =>
        $"User: {FormatUser(user)}\nManual: {FormatBirthdayDate(user.ManualBirthdayDay, user.ManualBirthdayMonth)}\n" +
        $"Profile: {FormatBirthdayDate(user.ProfileBirthdayDay, user.ProfileBirthdayMonth)}\n" +
        $"Effective: {FormatBirthdayDate(user.ManualBirthdayDay ?? user.ProfileBirthdayDay, user.ManualBirthdayMonth ?? user.ProfileBirthdayMonth)} " +
        $"({(user.ManualBirthdayDay is not null ? "manual" : user.ProfileBirthdayDay is not null ? "profile" : "none")})\n" +
        $"Notifications: {(user.BirthdayNotificationsEnabled ? "on" : "off")}";

    private static string FormatUser(TelegramUser user) =>
        $"{(string.IsNullOrEmpty(user.Username) ? user.FullName : $"@{user.Username}")} ({user.TelegramId})";

    private static string FormatBirthdayDate(int? day, int? month) => day is null || month is null ? "not set" : $"{day:00}.{month:00}";

    private bool IsBirthdayGroup() => Context.Chat.Type is ChatType.Group or ChatType.Supergroup
        && birthdayConfiguration.Value.AllowedChatIds.Contains(Context.Chat.Id);

    private Task<Message> ReplyAsync(string text) => BotClient.SendMessage(Context.Chat, text);
}
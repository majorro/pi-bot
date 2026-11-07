using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PiBot.Data.Telegram;
using PiBot.Data.Telegram.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PiBot.Telegram.Services.Birthdays;

internal sealed class BirthdayTargetResolver(
    TelegramPgContext dbContext,
    TelegramMembershipService membership,
    ITelegramBotClient botClient,
    IOptions<BirthdayConfiguration> birthdayConfiguration)
{
    public async Task<TelegramUser?> ResolveUserAsync(string? targetArgument, User? replyToUser, CancellationToken cancellationToken)
    {
        if (targetArgument is null)
            return replyToUser is null || replyToUser.IsBot
                ? null
                : await membership.SaveUserAsync(replyToUser);

        if (long.TryParse(targetArgument, out var telegramUserId))
        {
            if (telegramUserId <= 0)
                return null;
            var storedUser = await dbContext.TelegramUsers.SingleOrDefaultAsync(x => x.TelegramId == telegramUserId, cancellationToken);
            if (storedUser is not null)
                return storedUser;
            var groupUser = await FindUserInBirthdayGroupsAsync(telegramUserId, cancellationToken);
            return groupUser is null ? null : await membership.SaveUserAsync(groupUser);
        }

        var username = targetArgument.TrimStart('@');
        if (username.Length == 0 || username.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '_'))
            return null;
        var normalizedUsername = username.ToLowerInvariant();
        var matchingUsers = await dbContext.TelegramUsers
            .Where(x => x.Username != null && x.Username.ToLower() == normalizedUsername)
            .ToListAsync(cancellationToken);
        if (matchingUsers.Count != 1)
            return null;

        var telegramUser = await FetchTelegramUserAsync(matchingUsers[0].TelegramId, cancellationToken);
        if (telegramUser is null)
            return null;
        var resolvedUser = await membership.SaveUserAsync(telegramUser);
        return string.Equals(telegramUser.Username, username, StringComparison.OrdinalIgnoreCase) ? resolvedUser : null;
    }

    private async Task<User?> FetchTelegramUserAsync(long userId, CancellationToken cancellationToken)
    {
        try
        {
            var privateChat = await botClient.GetChat(userId, cancellationToken);
            if (privateChat.Type == ChatType.Private)
                return new User { Id = privateChat.Id, FirstName = privateChat.FirstName ?? "", LastName = privateChat.LastName, Username = privateChat.Username };
        }
        catch (ApiRequestException) { /* A group member may not have an accessible private chat. */ }
        return await FindUserInBirthdayGroupsAsync(userId, cancellationToken);
    }

    private async Task<User?> FindUserInBirthdayGroupsAsync(long userId, CancellationToken cancellationToken)
    {
        foreach (var chatId in birthdayConfiguration.Value.AllowedChatIds.Distinct())
        {
            var chatMember = await membership.RefreshChatMembershipAsync(chatId, userId);
            if (chatMember is not null && TelegramMembershipService.IsMember(chatMember))
                return chatMember.User;
        }
        return null;
    }
}
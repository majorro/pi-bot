using Microsoft.EntityFrameworkCore;
using PiBot.Data.Telegram;
using PiBot.Data.Telegram.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PiBot.Telegram.Services;

internal sealed class TelegramMembershipService(
    TelegramPgContext dbContext,
    ITelegramBotClient botClient,
    ILogger<TelegramMembershipService> logger)
{
    public static bool IsMember(ChatMember chatMember) =>
        chatMember.Status is ChatMemberStatus.Creator or ChatMemberStatus.Administrator or ChatMemberStatus.Member
        || chatMember is ChatMemberRestricted { IsMember: true };

    public async Task<List<TelegramUserChat>> TrackMembershipsAsync(Update update)
    {
        var newMemberships = new List<TelegramUserChat>();
        if (update.ChatMember is { } membershipUpdate && IsGroup(membershipUpdate.Chat))
        {
            await TrackUserMembershipAsync(
                membershipUpdate.NewChatMember.User,
                membershipUpdate.Chat.Id,
                IsMember(membershipUpdate.NewChatMember));
        }
        else if (update.Message is { } message && message.Chat.Type != ChatType.Channel)
        {
            if (message.SenderChat is null && message.From is { } messageSender)
                await TrackUserMembershipAsync(messageSender, message.Chat.Id, null);
            foreach (var user in message.NewChatMembers ?? [])
                await TrackUserMembershipAsync(user, message.Chat.Id, true);
            if (message.LeftChatMember is { } departedUser)
                await TrackUserMembershipAsync(departedUser, message.Chat.Id, false);
        }
        return newMemberships;

        async Task TrackUserMembershipAsync(User user, long chatId, bool? isMember)
        {
            if (user.IsBot)
                return;
            var (membership, isNewMembership) = await UpdateMembershipAsync(user, chatId, isMember);
            if (isNewMembership)
                newMemberships.Add(membership);
        }
    }

    public async Task RefreshKnownUsersAndMembershipsAsync()
    {
        var userIds = await dbContext.TelegramUsers.Select(x => x.TelegramId).ToListAsync();
        var groupIds = await dbContext.TelegramChats.Where(x => x.TelegramId < 0).Select(x => x.TelegramId).ToListAsync();
        foreach (var userId in userIds)
            foreach (var groupId in groupIds)
                await RefreshChatMembershipAsync(groupId, userId);
    }

    public async Task<TelegramUser> SaveUserAsync(User user)
    {
        var storedUser = await AddOrUpdateUserAsync(user);
        await dbContext.SaveChangesAsync();
        return storedUser;
    }

    public async Task<(TelegramUserChat Membership, bool IsNewMembership)> UpdateMembershipAsync(
        User user,
        long chatId,
        bool? isMember)
    {
        var storedUser = await AddOrUpdateUserAsync(user);
        var membership = await dbContext.TelegramUserChats.SingleOrDefaultAsync(
            x => x.TelegramUserId == user.Id && x.TelegramChatId == chatId);
        var isNewMembership = membership is null;
        if (membership is null)
        {
            var chat = await dbContext.TelegramChats.SingleOrDefaultAsync(x => x.TelegramId == chatId);
            if (chat is null)
            {
                chat = new TelegramChat { TelegramId = chatId };
                dbContext.TelegramChats.Add(chat);
            }
            membership = new TelegramUserChat
            {
                User = storedUser,
                Community = chat,
                TelegramUserId = user.Id,
                TelegramChatId = chatId
            };
            dbContext.TelegramUserChats.Add(membership);
        }

        // Regular messages initialize unknown membership without replacing a known status.
        if (isMember is not null || membership.IsMember is null)
        {
            membership.IsMember = isMember ?? true;
            membership.MembershipUpdatedAt = DateTime.UtcNow;
        }
        await dbContext.SaveChangesAsync();
        return (membership, isNewMembership);
    }

    private async Task<TelegramUser> AddOrUpdateUserAsync(User user)
    {
        var storedUser = await dbContext.TelegramUsers.SingleOrDefaultAsync(x => x.TelegramId == user.Id);
        if (storedUser is null)
        {
            storedUser = new TelegramUser { TelegramId = user.Id };
            dbContext.TelegramUsers.Add(storedUser);
        }
        storedUser.FirstName = user.FirstName;
        storedUser.LastName = user.LastName;
        storedUser.Username = user.Username;
        return storedUser;
    }

    public async Task<ChatMember?> RefreshChatMembershipAsync(long chatId, long userId)
    {
        try
        {
            var chat = await botClient.GetChat(chatId);
            if (!IsGroup(chat))
                return null;
            var chatMember = await botClient.GetChatMember(chatId, userId);
            if (chatMember.User.IsBot)
                return null;
            await UpdateMembershipAsync(chatMember.User, chatId, IsMember(chatMember));
            return chatMember;
        }
        catch (ApiRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Membership lookup failed in {ChatId} for {UserId}: API code {ErrorCode}",
                chatId,
                userId,
                exception.ErrorCode);
            return null;
        }
    }

    private static bool IsGroup(Chat chat) => chat.Type is ChatType.Group or ChatType.Supergroup;
}
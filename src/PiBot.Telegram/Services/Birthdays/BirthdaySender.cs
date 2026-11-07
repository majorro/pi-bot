using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PiBot.Data.Telegram;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using static PiBot.Telegram.Utils.HtmlFormatHelper;

namespace PiBot.Telegram.Services.Birthdays;

internal sealed class BirthdaySender(
    ITelegramBotClient botClient,
    TelegramPgContext dbContext,
    TelegramMembershipService membershipService,
    IOptions<BirthdayConfiguration> birthdayConfiguration,
    TimeProvider timeProvider,
    ILogger<BirthdaySender> logger)
{
    public async Task SendBirthdayMessagesAsync(long userId)
    {
        var todayMsc = BirthdayCalendarHelper.GetTodayInMoscow(timeProvider);
        var user = await dbContext.TelegramUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.TelegramId == userId);

        if (user is null || !user.BirthdayNotificationsEnabled || !BirthdayCalendarHelper.IsBirthdayOnDate(user, todayMsc))
            return;

        foreach (var chatId in birthdayConfiguration.Value.AllowedChatIds.Distinct())
        {
            try
            {
                var chatMember = await membershipService.RefreshChatMembershipAsync(chatId, userId);
                if (chatMember is null || !TelegramMembershipService.IsMember(chatMember))
                    continue;
                for (var i = 0; i < 3; i++)
                {
                    await botClient.SendMessage(
                        chatId,
                        $"{Mention(user)} s dr 🥳 🥳 🥳",
                        parseMode: ParseMode.Html);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Birthday delivery failed in {ChatId} for {UserId}", chatId, userId);
            }
        }
    }

    public async Task SendTodaysBirthdayMessagesAsync()
    {
        var todayMsc = BirthdayCalendarHelper.GetTodayInMoscow(timeProvider);
        var birthdayCandidates = await dbContext.TelegramUsers.AsNoTracking()
            .Where(x => x.BirthdayNotificationsEnabled && (x.ManualBirthdayDay != null || x.ProfileBirthdayDay != null))
            .ToListAsync();
        foreach (var user in birthdayCandidates.Where(user => BirthdayCalendarHelper.IsBirthdayOnDate(user, todayMsc)))
            await SendBirthdayMessagesAsync(user.TelegramId);
    }
}
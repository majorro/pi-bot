using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PiBot.Data.Telegram;
using PiBot.Data.Telegram.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace PiBot.Telegram.Services.Birthdays;

internal sealed class BirthdayProfileImporter(
    TelegramPgContext dbContext,
    ITelegramBotClient botClient,
    IOptions<BirthdayConfiguration> birthdayConfiguration,
    ILogger<BirthdayProfileImporter> logger)
{
    public async Task ImportBirthdaysForNewMembersAsync(IEnumerable<TelegramUserChat> newMemberships)
    {
        var memberUserIds = newMemberships
            .Where(x => x.IsMember != false && birthdayConfiguration.Value.AllowedChatIds.Contains(x.TelegramChatId))
            .Select(x => x.TelegramUserId)
            .Distinct();

        foreach (var userId in memberUserIds)
            await ImportMissingBirthdayAsync(userId);
    }

    public async Task ImportMissingBirthdaysAsync()
    {
        var allowedChatIds = birthdayConfiguration.Value.AllowedChatIds.ToList();
        var memberUserIds = await dbContext.TelegramUserChats
            .Where(x => allowedChatIds.Contains(x.TelegramChatId))
            .Select(x => x.TelegramUserId)
            .Distinct()
            .ToListAsync();

        foreach (var userId in memberUserIds)
            await ImportMissingBirthdayAsync(userId);
    }

    private async Task ImportMissingBirthdayAsync(long userId)
    {
        var user = await dbContext.TelegramUsers.SingleOrDefaultAsync(x =>
            x.TelegramId == userId &&
            x.ProfileBirthdayDay == null &&
            x.ProfileBirthdayMonth == null);
        if (user is null)
            return;

        try
        {
            var privateChat = await botClient.GetChat(userId);
            if (privateChat.Birthdate is not { } profileBirthday)
            {
                logger.LogDebug("Telegram returned no birthday for {UserId}", userId);
                return;
            }
            user.ProfileBirthdayDay = profileBirthday.Day;
            user.ProfileBirthdayMonth = profileBirthday.Month;
            await dbContext.SaveChangesAsync();
        }
        catch (ApiRequestException exception)
        {
            logger.LogWarning(exception, "Birthday profile lookup failed for {UserId}: API code {ErrorCode}", userId, exception.ErrorCode);
        }
        catch (RequestException exception)
        {
            logger.LogWarning(exception, "Birthday profile lookup failed for {UserId}: {ErrorType}", userId, exception.GetType().Name);
        }
    }
}
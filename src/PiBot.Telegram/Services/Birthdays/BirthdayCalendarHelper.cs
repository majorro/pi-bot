using System.Globalization;
using PiBot.Data.Telegram.Models;

namespace PiBot.Telegram.Services.Birthdays;

internal static class BirthdayCalendarHelper
{
    private static readonly TimeSpan MoscowUtcOffset = TimeSpan.FromHours(3);

    public static DateOnly GetTodayInMoscow(TimeProvider timeProvider) => DateOnly.FromDateTime(timeProvider.GetUtcNow().ToOffset(MoscowUtcOffset).DateTime);

    public static DateTimeOffset GetNextNotificationTime(DateTimeOffset utcNow)
    {
        var nextNotificationTime = new DateTimeOffset(utcNow.UtcDateTime.Date.AddHours(21), TimeSpan.Zero);
        return nextNotificationTime > utcNow ? nextNotificationTime : nextNotificationTime.AddDays(1);
    }

    public static bool IsBirthdayOnDate(TelegramUser user, DateOnly candidateDate)
        => (user.ManualBirthdayDay ?? user.ProfileBirthdayDay) is not null && GetBirthdayDate(user, candidateDate.Year) == candidateDate;

    public static DateOnly GetNextBirthdayDate(TelegramUser user, DateOnly todayInMoscow)
    {
        var birthdayDate = GetBirthdayDate(user, todayInMoscow.Year);
        return birthdayDate >= todayInMoscow ? birthdayDate : GetBirthdayDate(user, todayInMoscow.Year + 1);
    }

    private static DateOnly GetBirthdayDate(TelegramUser user, int year)
    {
        var birthdayDay = user.ManualBirthdayDay ?? user.ProfileBirthdayDay;
        var birthdayMonth = user.ManualBirthdayMonth ?? user.ProfileBirthdayMonth;
        if (birthdayDay == 29 && birthdayMonth == 2 && !DateTime.IsLeapYear(year))
            return new DateOnly(year, 3, 1);
        return new DateOnly(year, birthdayMonth!.Value, birthdayDay!.Value);
    }

    public static bool TryParseBirthdayDate(string dateText, out DateOnly birthdayDate) =>
        DateOnly.TryParseExact($"{dateText}.2000", "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out birthdayDate);
}
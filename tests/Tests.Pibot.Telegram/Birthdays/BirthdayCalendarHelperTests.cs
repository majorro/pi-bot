using Microsoft.Extensions.Time.Testing;
using PiBot.Data.Telegram.Models;
using PiBot.Telegram.Services.Birthdays;
using PiBot.Telegram.Utils;

namespace Tests.Pibot.Telegram.Birthdays;

public sealed class BirthdayCalendarHelperTests
{
    [Theory]
    [InlineData("2026-12-31T20:59:59Z", "2026-12-31", "2026-12-31T21:00:00Z")]
    [InlineData("2026-12-31T21:00:00Z", "2027-01-01", "2027-01-01T21:00:00Z")]
    [InlineData("2027-01-01T02:00:00Z", "2027-01-01", "2027-01-01T21:00:00Z")]
    public void MoscowDateAndNextRunDoNotCatchUp(string now, string expectedMoscowDate, string expectedNotificationTime)
    {
        var utcNow = DateTimeOffset.Parse(now);
        BirthdayCalendarHelper.GetTodayInMoscow(new FakeTimeProvider(utcNow)).Should().Be(DateOnly.Parse(expectedMoscowDate));
        BirthdayCalendarHelper.GetNextNotificationTime(utcNow).Should().Be(DateTimeOffset.Parse(expectedNotificationTime));
    }

    [Theory]
    [InlineData("29.02", true)]
    [InlineData("31.04", false)]
    [InlineData("00.01", false)]
    [InlineData("01.13", false)]
    [InlineData("1.1", false)]
    [InlineData("01.01.2000", false)]
    public void BirthdayInputIsAValidDayAndMonth(string input, bool valid) =>
        BirthdayCalendarHelper.TryParseBirthdayDate(input, out _).Should().Be(valid);

    [Fact]
    public void ManualDateWinsAndLeapDayUsesMarchFirstOnlyInCommonYears()
    {
        var user = new TelegramUser { ManualBirthdayDay = 29, ManualBirthdayMonth = 2, ProfileBirthdayDay = 10, ProfileBirthdayMonth = 5 };
        BirthdayCalendarHelper.IsBirthdayOnDate(user, new DateOnly(2027, 3, 1)).Should().BeTrue();
        BirthdayCalendarHelper.IsBirthdayOnDate(user, new DateOnly(2027, 2, 28)).Should().BeFalse();
        BirthdayCalendarHelper.IsBirthdayOnDate(user, new DateOnly(2028, 2, 29)).Should().BeTrue();
        BirthdayCalendarHelper.IsBirthdayOnDate(user, new DateOnly(2028, 3, 1)).Should().BeFalse();
        BirthdayCalendarHelper.IsBirthdayOnDate(user, new DateOnly(2027, 5, 10)).Should().BeFalse();
    }

    [Fact]
    public void MentionUsesTelegramIdAndEscapesBothUserTypes()
    {
        const string expected = "<a href=\"tg://user?id=42\">A &lt;B&gt; &amp; C</a>";
        var storedUser = new TelegramUser { Id = 1, TelegramId = 42, FirstName = "A <B> & C" };
        var telegramUser = new global::Telegram.Bot.Types.User { Id = 42, FirstName = "A <B> & C" };

        HtmlFormatHelper.Mention(storedUser).Should().Be(expected);
        HtmlFormatHelper.Mention(telegramUser).Should().Be(expected);
    }

    [Theory]
    [InlineData("2027-02-28", "2027-03-01")]
    [InlineData("2027-03-01", "2027-03-01")]
    [InlineData("2027-03-02", "2028-02-29")]
    [InlineData("2028-03-01", "2029-03-01")]
    public void UpcomingLeapDayUsesTheCelebrationDate(string today, string expected)
    {
        var user = new TelegramUser { ProfileBirthdayDay = 29, ProfileBirthdayMonth = 2 };
        BirthdayCalendarHelper.GetNextBirthdayDate(user, DateOnly.Parse(today)).Should().Be(DateOnly.Parse(expected));
    }

    [Fact]
    public void MissingBirthdayDoesNotMatchToday() =>
        BirthdayCalendarHelper.IsBirthdayOnDate(new TelegramUser(), new DateOnly(2026, 10, 29)).Should().BeFalse();
}
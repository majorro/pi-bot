using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PiBot.Telegram.Services.Birthdays;

namespace Tests.Pibot.Telegram.Birthdays;

public sealed class BirthdayDeliveryTests
{
    private static TelegramTestServices CreateTestServices(params long[] chatIds) => new(chatIds);

    [Fact]
    public async Task TriggerSendsThreeGreetingsToEachCurrentGroupAndAllowsRepeats()
    {
        await using var testServices = CreateTestServices(-100123, -100124, -100125);
        await testServices.SeedBirthdayUserAsync();
        testServices.ChatsUserHasLeft.Add(-100125);
        var result = await testServices.CreateBirthdayModule().ExecuteAsync(args: ["trigger", "@birthday_user"]);
        result.Text.Should().Contain("Sent birthday messages");
        testServices.BirthdayMessages.Where(x => x.ChatId == -100123).Should().HaveCount(3);
        testServices.BirthdayMessages.Where(x => x.ChatId == -100124).Should().HaveCount(3);
        testServices.BirthdayMessages.Should().NotContain(x => x.ChatId == -100125);
        testServices.BirthdayMessages.Should().OnlyContain(greeting => greeting.Text == "<a href=\"tg://user?id=42\">birthday_user</a> s dr 🥳 🥳 🥳");

        await testServices.CreateBirthdayModule().ExecuteAsync(args: ["trigger", "42"]);
        testServices.BirthdayMessages.Should().HaveCount(12);
    }

    [Fact]
    public async Task TriggerRejectsDatesBeforeAndAfterTheBirthdayInMoscow()
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync(profileBirthdayDay: 30);
        (await testServices.CreateBirthdayModule().ExecuteAsync(args: ["trigger", "42"])).Text.Should().Contain("not today in Moscow");
        testServices.TimeProvider.SetUtcNow(new DateTimeOffset(2026, 10, 30, 21, 0, 0, TimeSpan.Zero));
        (await testServices.CreateBirthdayModule().ExecuteAsync(args: ["trigger", "42"])).Text.Should().Contain("not today in Moscow");
        testServices.BirthdayMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task StartedBirthdayBatchCompletesAfterMoscowMidnight()
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync();
        testServices.TimeProvider.SetUtcNow(new DateTimeOffset(2026, 10, 29, 20, 59, 59, TimeSpan.Zero));
        testServices.BeforeBirthdayMessageSent = _ => testServices.TimeProvider.Advance(TimeSpan.FromSeconds(4));
        await testServices.BirthdaySender.SendBirthdayMessagesAsync(42);

        testServices.BirthdayMessages.Should().HaveCount(3);
    }

    [Fact]
    public async Task AFailedGroupBatchDoesNotStopOtherGroups()
    {
        await using var testServices = CreateTestServices(-100123, -100124);
        await testServices.SeedBirthdayUserAsync();
        var originalResponseHandler = testServices.ApiStub.ResponseHandler;
        var sendAttemptCount = 0;
        testServices.ApiStub.ResponseHandler = (method, body) => method == "sendMessage" && body.GetProperty("chat_id").GetInt64() == -100123 && ++sendAttemptCount == 2
            ? new TelegramApiStub.ApiError(400) : originalResponseHandler(method, body);
        await testServices.BirthdaySender.SendBirthdayMessagesAsync(42);

        testServices.BirthdayMessages.Where(x => x.ChatId == -100123).Should().HaveCount(1);
        testServices.BirthdayMessages.Where(x => x.ChatId == -100124).Should().HaveCount(3);
    }

    [Fact]
    public async Task FailedMembershipLookupPreventsDelivery()
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync();
        var originalResponseHandler = testServices.ApiStub.ResponseHandler;
        testServices.ApiStub.ResponseHandler = (method, body) => method == "getChatMember" && body.GetProperty("user_id").GetInt64() == 42
            ? new TelegramApiStub.ApiError(400) : originalResponseHandler(method, body);
        await testServices.BirthdaySender.SendBirthdayMessagesAsync(42);
        testServices.BirthdayMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ConfiguringAChannelIdCannotPublishABirthdayToIt()
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync();
        var originalResponseHandler = testServices.ApiStub.ResponseHandler;
        testServices.ApiStub.ResponseHandler = (method, body) => method == "getChat" && body.GetProperty("chat_id").GetInt64() < 0
            ? TelegramApiStub.CreateGroupChat(body.GetProperty("chat_id").GetInt64(), "channel") : originalResponseHandler(method, body);
        await testServices.BirthdaySender.SendBirthdayMessagesAsync(42);
        testServices.BirthdayMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ScheduledScanUsesEffectiveDatesAndDisabledUsersAreSkipped()
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync(manualBirthdayDay: 30, manualBirthdayMonth: 10);
        await testServices.BirthdaySender.SendTodaysBirthdayMessagesAsync();
        testServices.BirthdayMessages.Should().BeEmpty();
        await testServices.SeedBirthdayUserAsync(notificationsEnabled: false);
        await testServices.BirthdaySender.SendTodaysBirthdayMessagesAsync();
        testServices.BirthdayMessages.Should().BeEmpty();
        await testServices.SeedBirthdayUserAsync();
        await testServices.BirthdaySender.SendTodaysBirthdayMessagesAsync();
        testServices.BirthdayMessages.Should().HaveCount(3);
    }

    [Fact]
    public async Task SchedulerStartupWaitsForFutureMidnightWithoutCatchup()
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync();
        testServices.TimeProvider.Advance(TimeSpan.FromMinutes(10));
        await using var serviceProvider = new ServiceCollection().AddScoped(_ => testServices.BirthdaySender).AddScoped(_ => testServices.ProfileImporter).BuildServiceProvider();
        using var notificationService = new BirthdayNotificationWorker(serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            testServices.TimeProvider, NullLogger<BirthdayNotificationWorker>.Instance);
        await notificationService.StartAsync(TestContext.Current.CancellationToken);
        // Allow BackgroundService's ExecuteAsync to register its first timer.
        await Task.Delay(50, TestContext.Current.CancellationToken);
        testServices.BirthdayMessages.Should().BeEmpty();
        testServices.TimeProvider.Advance(TimeSpan.FromHours(23));
        await Task.Delay(50, TestContext.Current.CancellationToken);
        testServices.BirthdayMessages.Should().BeEmpty();
        await notificationService.StopAsync(TestContext.Current.CancellationToken);
    }
}
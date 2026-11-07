using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PiBot.Telegram;
using PiBot.Telegram.Modules;
using PiBot.Telegram.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Tests.Pibot.Telegram.Birthdays;

public sealed class BirthdayCommandTests
{
    private static TelegramTestServices CreateTestServices() => new();

    [Fact]
    public async Task StartedCommandCompletesWhenPollingIsCancelled()
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var originalResponseHandler = testServices.ApiStub.ResponseHandler;
        testServices.ApiStub.ResponseHandler = (method, body) =>
        {
            if (method == "getChat" && body.GetProperty("chat_id").GetInt64() == TelegramTestServices.BirthdayUserId)
                cancellation.Cancel();
            return originalResponseHandler(method, body);
        };
        using var serviceProvider = new ServiceCollection()
            .AddScoped(_ => testServices.MembershipService)
            .AddScoped(_ => testServices.ProfileImporter)
            .AddKeyedScoped<ModuleBase>("birthday", (_, _) => new BirthdayModule(
                testServices.BotClient, testServices.DbContext, testServices.TargetResolver, testServices.BirthdaySender, testServices.MembershipService,
                Options.Create(new BotConfiguration { AdminUserIds = [TelegramTestServices.AdminUserId] }),
                testServices.BirthdayConfiguration, testServices.TimeProvider))
            .BuildServiceProvider();
        var updateHandler = new UpdateHandlingService(NullLogger<UpdateHandlingService>.Instance,
            new CommandHandlingService(NullLogger<CommandHandlingService>.Instance, serviceProvider),
            serviceProvider.GetRequiredService<IServiceScopeFactory>());

        await updateHandler.HandleUpdateAsync(testServices.BotClient, new Update
        {
            Message = new Message
            {
                From = new User { Id = TelegramTestServices.AdminUserId, FirstName = "Admin" },
                Chat = new Chat { Id = TelegramTestServices.AdminUserId, Type = ChatType.Private },
                Text = "/birthday off @birthday_user", Date = testServices.TimeProvider.GetUtcNow().UtcDateTime
            }
        }, cancellation.Token);

        cancellation.IsCancellationRequested.Should().BeTrue();
        var user = await testServices.DbContext.TelegramUsers.AsNoTracking()
            .SingleAsync(x => x.TelegramId == TelegramTestServices.BirthdayUserId, TestContext.Current.CancellationToken);
        user.BirthdayNotificationsEnabled.Should().BeFalse();
        testServices.ApiStub.Requests.Should().ContainSingle(call => call.Method == "sendMessage"
            && call.Body.GetProperty("text").GetString()!.Contains("@birthday_user (42): off"));
    }

    [Theory]
    [InlineData("set", "42", "29.10")]
    [InlineData("show", "42")]
    [InlineData("show")]
    [InlineData("clear", "42")]
    [InlineData("off", "42")]
    [InlineData("on", "42")]
    [InlineData("trigger", "42")]
    public async Task NonAdminCannotUseCommands(params string[] args)
    {
        await using var testServices = CreateTestServices();
        var response = await testServices.CreateBirthdayModule(commandUserId: 8, chatId: 8).ExecuteAsync(args: args);
        response.Text.Should().Contain("restricted");
        testServices.ApiStub.Requests.Should().NotContain(call => call.Method != "sendMessage");
    }

    [Theory]
    [InlineData("show")]
    [InlineData("clear")]
    [InlineData("off")]
    [InlineData("on")]
    [InlineData("trigger")]
    public async Task OtherOperationsRequireAdminDm(string operation)
    {
        await using var testServices = CreateTestServices();
        var response = await testServices.CreateBirthdayModule(chatId: TelegramTestServices.BirthdayChatId).ExecuteAsync(args: [operation, "42"]);
        response.Text.Should().Contain("private message");
        testServices.ApiStub.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task SetAndClearPreserveProfileBirthday()
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync();
        var replyToUser = new User { Id = 42, FirstName = "Birthday User", Username = "birthday_user" };
        var group = testServices.CreateBirthdayModule(chatId: TelegramTestServices.BirthdayChatId, replyToUser: replyToUser);
        (await group.ExecuteAsync(args: ["set", "01.11"])).Text.Should().Contain("01.11");
        (await testServices.CreateBirthdayModule().ExecuteAsync(args: ["set", "42", "02.11"])).Text.Should().Contain("02.11");
        var shown = await testServices.CreateBirthdayModule().ExecuteAsync(args: ["show", "42"]);
        shown.Text.Should().Contain("Manual: 02.11");
        shown.Text.Should().Contain("Profile: 29.10");
        await testServices.CreateBirthdayModule().ExecuteAsync(args: ["clear", "42"]);
        var user = await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        user.ManualBirthdayDay.Should().BeNull();
        user.ManualBirthdayMonth.Should().BeNull();
        user.ProfileBirthdayDay.Should().Be(29);
        user.ProfileBirthdayMonth.Should().Be(10);
    }

    [Fact]
    public async Task SetRejectsInvalidDateGroupAndTarget()
    {
        await using var testServices = CreateTestServices();
        (await testServices.CreateBirthdayModule().ExecuteAsync(args: ["set", "42", "31.04"])).Text.Should().Contain("Invalid birthday");
        (await testServices.CreateBirthdayModule(chatId: -999).ExecuteAsync(args: ["set", "42", "01.01"])).Text.Should().Contain("allowed groups");
        var bot = new User { Id = 999, FirstName = "Bot", IsBot = true };
        (await testServices.CreateBirthdayModule(replyToUser: bot).ExecuteAsync(args: ["set", "01.01"])).Text.Should().Contain("could not be verified");
        (await testServices.DbContext.TelegramUsers.ToListAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
    }

    [Fact]
    public async Task TriggerRespectsNotificationPreference()
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync();
        await testServices.CreateBirthdayModule().ExecuteAsync(args: ["off", "42"]);
        (await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).BirthdayNotificationsEnabled.Should().BeFalse();
        (await testServices.CreateBirthdayModule().ExecuteAsync(args: ["trigger", "42"])).Text.Should().Contain("disabled");
        testServices.BirthdayMessages.Should().BeEmpty();
        await testServices.CreateBirthdayModule().ExecuteAsync(args: ["on", "42"]);
        (await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).BirthdayNotificationsEnabled.Should().BeTrue();
    }

    [Theory]
    [InlineData("@BIRTHDAY_USER")]
    [InlineData("birthday_user")]
    public async Task UsernameMatchingIsCaseInsensitive(string target)
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync();
        var response = await testServices.CreateBirthdayModule().ExecuteAsync(args: ["show", target]);
        response.Text.Should().Contain("User: @birthday_user (42)");
        testServices.ApiStub.Requests.Should().Contain(call => call.Method == "getChat");
    }

    [Theory]
    [InlineData("43")]
    [InlineData(null)]
    public async Task SetDiscoversMemberMissingFromLocalDatabase(string? targetId)
    {
        await using var testServices = CreateTestServices();
        var replyToUser = targetId is null ? new User { Id = 43, FirstName = "New member", Username = "birthday_user" } : null;
        var module = testServices.CreateBirthdayModule(replyToUser: replyToUser);

        var response = await module.ExecuteAsync(args: targetId is null ? ["set", "29.10"] : ["set", targetId, "29.10"]);

        response.Text.Should().Contain("Manual birthday");
        var user = await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        user.TelegramId.Should().Be(43);
        user.ManualBirthdayDay.Should().Be(29);
        user.ManualBirthdayMonth.Should().Be(10);
        var membership = await testServices.DbContext.TelegramUserChats.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        membership.TelegramUserId.Should().Be(43);
        membership.TelegramChatId.Should().Be(TelegramTestServices.BirthdayChatId);
        membership.IsMember.Should().BeTrue();
        var shown = await testServices.CreateBirthdayModule().ExecuteAsync(args: ["show"]);
        shown.Text.Should().Contain("@birthday_user (43)");
    }

    [Fact]
    public async Task InvalidOrAmbiguousUsernamesAreRejected()
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync();
        await testServices.MembershipService.SaveUserAsync(new User { Id = 43, FirstName = "Other", Username = "birthday_user" });
        (await testServices.TargetResolver.ResolveUserAsync("birthday_user", null, TestContext.Current.CancellationToken)).Should().BeNull();
        var otherUser = await testServices.DbContext.TelegramUsers.SingleAsync(x => x.TelegramId == 43, TestContext.Current.CancellationToken);
        otherUser.Username = "another_user";
        await testServices.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var originalResponseHandler = testServices.ApiStub.ResponseHandler;
        testServices.ApiStub.ResponseHandler = (method, body) => method == "getChat" && body.GetProperty("chat_id").GetInt64() > 0
            ? TelegramApiStub.CreatePrivateChat(body.GetProperty("chat_id").GetInt64(), "renamed_user") : originalResponseHandler(method, body);
        (await testServices.TargetResolver.ResolveUserAsync("birthday_user", null, TestContext.Current.CancellationToken)).Should().BeNull();
        (await testServices.TargetResolver.ResolveUserAsync("unknown_user", null, TestContext.Current.CancellationToken)).Should().BeNull();
        (await testServices.TargetResolver.ResolveUserAsync("%", null, TestContext.Current.CancellationToken)).Should().BeNull();
        (await testServices.TargetResolver.ResolveUserAsync("42", null, TestContext.Current.CancellationToken)).Should().NotBeNull();
    }

    [Fact]
    public async Task UsernameLookupFallsBackToGroupMembership()
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync();
        var originalResponseHandler = testServices.ApiStub.ResponseHandler;
        testServices.ApiStub.ResponseHandler = (method, body) => method == "getChat" && body.GetProperty("chat_id").GetInt64() > 0
            ? new TelegramApiStub.ApiError(400) : originalResponseHandler(method, body);
        (await testServices.TargetResolver.ResolveUserAsync("@birthday_user", null, TestContext.Current.CancellationToken)).Should().NotBeNull();
    }

    [Theory]
    [InlineData("set", "42", "29.10")]
    [InlineData("clear", "42")]
    [InlineData("show", "42")]
    [InlineData("off", "42")]
    [InlineData("on", "42")]
    [InlineData("trigger", "42")]
    public async Task CommandOutputsIncludeUsernameAndId(params string[] args)
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync();
        var response = await testServices.CreateBirthdayModule().ExecuteAsync(args: args);
        response.Text.Should().Contain("@birthday_user (42)");
    }

    [Fact]
    public async Task ShowListsUpcomingBirthdaysStartingToday()
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync();
        await AddBirthdayAsync(testServices, 43, 29, 10, "november");
        var userWithManualBirthday = await testServices.DbContext.TelegramUsers.SingleAsync(x => x.TelegramId == 43, TestContext.Current.CancellationToken);
        userWithManualBirthday.ManualBirthdayDay = 2;
        userWithManualBirthday.ManualBirthdayMonth = 11;
        await testServices.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await AddBirthdayAsync(testServices, 44, 3, 1, "january");
        await AddBirthdayAsync(testServices, 45, 29, 10, "disabled", notificationsEnabled: false);
        await AddBirthdayAsync(testServices, 46, 28, 10, null);
        await AddBirthdayAsync(testServices, 47, 29, 10, "departed", isMember: false);
        await AddBirthdayAsync(testServices, 48, 29, 10, "other_group", chatId: -999);
        // A second group association must not duplicate the user's birthday.
        await testServices.MembershipService.UpdateMembershipAsync(new User { Id = 42, FirstName = "Member", Username = "birthday_user" },
            -999, true);

        var response = await testServices.CreateBirthdayModule().ExecuteAsync(args: ["show"]);
        response.Text!.Split('\n').Should().Equal(new[]
        {
            "1. 29.10.2026 — @birthday_user (42)",
            "2. 29.10.2026 — @disabled (45) [off]",
            "3. 02.11.2026 — @november (43)",
            "4. 03.01.2027 — @january (44)",
            "5. 28.10.2027 — Member (46)"
        });
    }

    [Fact]
    public async Task ShowIncludesManualBirthdaySetInDmForUndiscoveredGroupMember()
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync(profileBirthdayDay: 30, profileBirthdayMonth: 10);
        var originalResponseHandler = testServices.ApiStub.ResponseHandler;
        testServices.ApiStub.ResponseHandler = (method, body) =>
            method == "getChatMember" && body.GetProperty("user_id").GetInt64() == TelegramTestServices.AdminUserId
                ? TelegramApiStub.CreateChatMember(TelegramTestServices.AdminUserId, username: "admin")
                : originalResponseHandler(method, body);
        await testServices.MembershipService.UpdateMembershipAsync(
            new User { Id = TelegramTestServices.AdminUserId, FirstName = "Admin", Username = "admin" },
            TelegramTestServices.AdminUserId,
            null);
        testServices.TimeProvider.SetUtcNow(new DateTimeOffset(2026, 10, 29, 20, 13, 0, TimeSpan.FromHours(3)).ToUniversalTime());

        var setResponse = await testServices.CreateBirthdayModule().ExecuteAsync(args: ["set", "7", "29.10"]);
        setResponse.Text.Should().Contain("Manual birthday");
        var response = await testServices.CreateBirthdayModule().ExecuteAsync(args: ["show"]);

        response.Text!.Split('\n').Should().Equal(
            "1. 29.10.2026 — @admin (7)",
            "2. 30.10.2026 — @birthday_user (42)");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    [InlineData(23)]
    public async Task ShowIncludesTodaysBirthdayThroughoutTheDay(int hourInMoscow)
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync();
        testServices.TimeProvider.SetUtcNow(new DateTimeOffset(2026, 10, 29, hourInMoscow, 30, 0, TimeSpan.FromHours(3)).ToUniversalTime());

        var response = await testServices.CreateBirthdayModule().ExecuteAsync(args: ["show"]);

        response.Text.Should().Be("1. 29.10.2026 — @birthday_user (42)");
    }

    [Fact]
    public async Task ShowUsesMoscowDateAtTheYearBoundary()
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync(profileBirthdayDay: 1, profileBirthdayMonth: 1);
        testServices.TimeProvider.SetUtcNow(new DateTimeOffset(2026, 12, 31, 21, 0, 0, TimeSpan.Zero));
        (await testServices.CreateBirthdayModule().ExecuteAsync(args: ["show"])).Text.Should().Contain("01.01.2027");
    }

    [Fact]
    public async Task ShowHandlesAnEmptyListAndStillRequiresAnAdminDm()
    {
        await using var testServices = CreateTestServices();
        (await testServices.CreateBirthdayModule().ExecuteAsync(args: ["show"])).Text.Should().Contain("No known birthdays");
        (await testServices.CreateBirthdayModule(chatId: TelegramTestServices.BirthdayChatId).ExecuteAsync(args: ["show"])).Text.Should().Contain("private message");
    }

    [Fact]
    public async Task ShowWithAReplyStillShowsOneUser()
    {
        await using var testServices = CreateTestServices();
        await testServices.SeedBirthdayUserAsync();
        var response = await testServices.CreateBirthdayModule(replyToUser: new User { Id = 42, FirstName = "Member", Username = "birthday_user" })
            .ExecuteAsync(args: ["show"]);
        response.Text.Should().Contain("User: @birthday_user (42)");
        response.Text.Should().Contain("Manual:");
    }

    [Fact]
    public async Task LongBirthdayListsAreSplitAtMessageBoundaries()
    {
        await using var testServices = CreateTestServices();
        for (var userId = 1; userId <= 40; userId++)
        {
            await AddBirthdayAsync(testServices, userId, 29, 10, null);
            var user = await testServices.DbContext.TelegramUsers.SingleAsync(x => x.TelegramId == userId, TestContext.Current.CancellationToken);
            user.FirstName = new string('A', 64);
            user.LastName = new string('B', 64);
        }
        await testServices.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await testServices.CreateBirthdayModule().ExecuteAsync(args: ["show"]);

        var messages = testServices.ApiStub.Requests.Where(call => call.Method == "sendMessage")
            .Select(call => call.Body.GetProperty("text").GetString()!).ToList();
        messages.Should().HaveCountGreaterThan(1);
        messages.Should().OnlyContain(text => text.Length >= 1 && text.Length <= 4000);
        var lines = string.Join("\n", messages).Split('\n');
        lines.Should().HaveCount(40);
        lines[0].Should().StartWith("1. 29.10.2026");
        lines[^1].Should().StartWith("40. 29.10.2026");
    }

    private static async Task AddBirthdayAsync(TelegramTestServices testServices, long userId, int birthdayDay, int birthdayMonth,
        string? username, bool notificationsEnabled = true, long chatId = TelegramTestServices.BirthdayChatId, bool isMember = true)
    {
        var user = new User { Id = userId, FirstName = "Member", Username = username };
        await testServices.MembershipService.UpdateMembershipAsync(user, chatId, isMember);
        var stored = await testServices.DbContext.TelegramUsers.SingleAsync(x => x.TelegramId == userId, TestContext.Current.CancellationToken);
        stored.ProfileBirthdayDay = birthdayDay;
        stored.ProfileBirthdayMonth = birthdayMonth;
        stored.BirthdayNotificationsEnabled = notificationsEnabled;
        await testServices.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
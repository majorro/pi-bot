using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;

namespace Tests.Pibot.Telegram.Membership;

public sealed class TelegramMembershipRefreshTests
{
    [Fact]
    public async Task CommunityNameIsRefreshedOnStartupRatherThanOnMembershipLookups()
    {
        await using var testServices = new TelegramTestServices();
        await testServices.MembershipService.UpdateMembershipAsync(
            new User { Id = 42, FirstName = "Member" },
            TelegramTestServices.BirthdayChatId,
            true,
            "Original name");

        await testServices.MembershipService.RefreshChatMembershipAsync(TelegramTestServices.BirthdayChatId, 42);

        (await testServices.DbContext.TelegramChats.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).Name.Should().Be("Original name");
        await testServices.MembershipService.RefreshKnownUsersAndMembershipsAsync();
        (await testServices.DbContext.TelegramChats.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).Name.Should().Be("Birthday test group");
    }

    [Fact]
    public async Task StartupRefreshOnlyUpdatesExistingGroupMemberships()
    {
        await using var testServices = new TelegramTestServices();
        await testServices.SeedBirthdayUserAsync(manualBirthdayDay: 1, manualBirthdayMonth: 11, notificationsEnabled: false);
        await testServices.MembershipService.UpdateMembershipAsync(new User { Id = 43, FirstName = "Departed member" }, -999, false);
        await testServices.MembershipService.UpdateMembershipAsync(new User { Id = 44, FirstName = "Private user" }, 44, null);
        var originalResponseHandler = testServices.ApiStub.ResponseHandler;
        testServices.ApiStub.ResponseHandler = (method, body) =>
        {
            if (method != "getChatMember")
                return originalResponseHandler(method, body);
            var userId = body.GetProperty("user_id").GetInt64();
            var chatId = body.GetProperty("chat_id").GetInt64();
            return new
            {
                user = new
                {
                    id = userId,
                    is_bot = false,
                    first_name = $"Updated {userId}",
                    last_name = "Surname",
                    username = $"updated_{userId}"
                },
                status = userId == 42 && chatId == TelegramTestServices.BirthdayChatId ? "left" : "member"
            };
        };

        await testServices.MembershipService.RefreshKnownUsersAndMembershipsAsync();

        var users = await testServices.DbContext.TelegramUsers.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        users.Where(x => x.TelegramId != 44).Should().OnlyContain(x => x.FirstName == $"Updated {x.TelegramId}" && x.LastName == "Surname" && x.Username == $"updated_{x.TelegramId}");
        users.Single(x => x.TelegramId == 44).FirstName.Should().Be("Private user");
        var birthdayUser = users.Single(x => x.TelegramId == 42);
        birthdayUser.ManualBirthdayDay.Should().Be(1);
        birthdayUser.ManualBirthdayMonth.Should().Be(11);
        birthdayUser.ProfileBirthdayDay.Should().Be(29);
        birthdayUser.ProfileBirthdayMonth.Should().Be(10);
        birthdayUser.BirthdayNotificationsEnabled.Should().BeFalse();
        var memberships = await testServices.DbContext.TelegramUserChats.AsNoTracking()
            .Where(x => x.TelegramChatId < 0).ToListAsync(TestContext.Current.CancellationToken);
        memberships.Should().HaveCount(2);
        memberships.Single(x => x.TelegramUserId == 42 && x.TelegramChatId == TelegramTestServices.BirthdayChatId).IsMember.Should().BeFalse();
        memberships.Single(x => x.TelegramUserId == 43 && x.TelegramChatId == -999).IsMember.Should().BeTrue();
        memberships.Should().NotContain(x => x.TelegramUserId == 44);
        (await testServices.DbContext.TelegramChats.Where(x => x.TelegramId < 0).ToListAsync(TestContext.Current.CancellationToken))
            .Should().OnlyContain(x => x.Name == "Birthday test group");
        testServices.ApiStub.Requests.Where(x => x.Method == "getChatMember").Should().HaveCount(2);
        testServices.ApiStub.Requests.Should().NotContain(x => x.Method == "getChat" && x.Body.GetProperty("chat_id").GetInt64() > 0);
    }

    [Fact]
    public async Task StartupRefreshKeepsExistingDataWhenLookupsFailAndContinuesWithOtherUsers()
    {
        await using var testServices = new TelegramTestServices();
        await testServices.SeedBirthdayUserAsync();
        await testServices.MembershipService.UpdateMembershipAsync(new User { Id = 43, FirstName = "Other member" }, TelegramTestServices.BirthdayChatId, false);
        var originalUpdatedAt = await testServices.DbContext.TelegramUserChats
            .Where(x => x.TelegramUserId == 42).Select(x => x.MembershipUpdatedAt).SingleAsync(TestContext.Current.CancellationToken);
        var originalResponseHandler = testServices.ApiStub.ResponseHandler;
        testServices.ApiStub.ResponseHandler = (method, body) =>
            method == "getChatMember" && body.GetProperty("user_id").GetInt64() == 42
                ? new TelegramApiStub.ApiError(400)
                : originalResponseHandler(method, body);

        await testServices.MembershipService.RefreshKnownUsersAndMembershipsAsync();

        var user = await testServices.DbContext.TelegramUsers.AsNoTracking().SingleAsync(x => x.TelegramId == 42, TestContext.Current.CancellationToken);
        user.Username.Should().Be("birthday_user");
        user.FirstName.Should().Be("Birthday <User>");
        var memberships = await testServices.DbContext.TelegramUserChats.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        memberships.Single(x => x.TelegramUserId == 42).IsMember.Should().BeTrue();
        memberships.Single(x => x.TelegramUserId == 42).MembershipUpdatedAt.Should().Be(originalUpdatedAt);
        memberships.Single(x => x.TelegramUserId == 43).IsMember.Should().BeTrue();
        testServices.ApiStub.Requests.Should().NotContain(x => x.Method == "getChat" && x.Body.GetProperty("chat_id").GetInt64() > 0);
    }
}
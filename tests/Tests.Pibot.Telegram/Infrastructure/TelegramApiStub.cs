using System.Net;
using System.Text;
using System.Text.Json;
using Telegram.Bot;

namespace Tests.Pibot.Telegram.Infrastructure;

internal sealed class TelegramApiStub : HttpMessageHandler
{
    public Func<string, JsonElement, object> ResponseHandler { get; set; } = (apiMethodName, _) =>
        throw new InvalidOperationException($"Unexpected Telegram API call: {apiMethodName}");
    public List<(string Method, JsonElement Body)> Requests { get; } = [];

    public ITelegramBotClient CreateBotClient() => new TelegramBotClient(
        new TelegramBotClientOptions("123456:TEST_TOKEN"), new HttpClient(this));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var apiMethodName = request.RequestUri!.Segments[^1];
        var requestBody = request.Content is null ? "{}" : await request.Content.ReadAsStringAsync(cancellationToken);
        using var requestDocument = JsonDocument.Parse(string.IsNullOrWhiteSpace(requestBody) ? "{}" : requestBody);
        var requestPayload = requestDocument.RootElement.Clone();
        Requests.Add((apiMethodName, requestPayload));
        var result = ResponseHandler(apiMethodName, requestPayload);
        var apiResponse = result is ApiError responseError
            ? new { ok = false, error_code = responseError.Code, description = "test API failure", parameters = responseError.RetryAfter is null ? null : new { retry_after = responseError.RetryAfter } } as object
            : new { ok = true, result };
        return new HttpResponseMessage(result is ApiError apiError ? (HttpStatusCode)apiError.Code : HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(apiResponse), Encoding.UTF8, "application/json")
        };
    }

    public static object CreateUser(long id = 42, string? username = "birthday_user") => new { id, first_name = "Birthday <User>", is_bot = false, username };
    public static object CreatePrivateChat(long id = 42, string? username = "birthday_user", object? birthdate = null) =>
        new { id, type = "private", first_name = "Birthday <User>", username, accent_color_id = 0, max_reaction_count = 0, birthdate };
    public static object CreateGroupChat(long id, string type = "supergroup") =>
        new { id, type, title = "Birthday test group", accent_color_id = 0, max_reaction_count = 0 };
    public static object CreateChatMember(long id = 42, string status = "member", string? username = "birthday_user", bool isMember = true) =>
        new { user = CreateUser(id, username), status, is_member = isMember };
    public static object CreateMessage(long chatId, string text) => new { message_id = 1, date = 1793221200, chat = new { id = chatId, type = chatId < 0 ? "supergroup" : "private" }, text };
    public sealed record ApiError(int Code, int? RetryAfter = null);
}
using PiBot.Data.Telegram.Models;
using Telegram.Bot.Types;
using System.Net;

namespace PiBot.Telegram.Utils;

internal static class HtmlFormatHelper
{
    internal static string Mention(User user) =>
        $"<a href=\"tg://user?id={user.Id}\">{WebUtility.HtmlEncode(user.Username ?? $"{user.FirstName} {user.LastName}".TrimEnd())}</a>";
    internal static string Mention(TelegramUser user) =>
        $"<a href=\"tg://user?id={user.TelegramId}\">{WebUtility.HtmlEncode(user.Username ?? $"{user.FirstName} {user.LastName}".TrimEnd())}</a>";

    internal static string Bold<T>(T input) where T : notnull => $"<b>{input.ToString()}</b>";
    internal static string Italics<T>(T input) where T : notnull => $"<i>{input.ToString()}</i>";
}
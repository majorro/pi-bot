using Telegram.Bot.Types;

namespace PiBot.Telegram.Modules;

internal class ModuleContext(User user, Chat chat, User? replyToUser)
{
    public User User { get; } = user;
    public Chat Chat { get; } = chat;
    public User? ReplyToUser { get; } = replyToUser;

    public IDictionary<string, object?> Meta { get; } = new Dictionary<string, object?>();
}
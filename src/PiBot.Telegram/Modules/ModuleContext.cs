using Telegram.Bot.Types;

namespace PiBot.Telegram.Modules;

internal class ModuleContext(User user, Chat chat)
{
    public User User { get; } = user;
    public Chat Chat { get; } = chat;
}
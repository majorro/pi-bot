using Telegram.Bot.Types;

namespace PiBot.Telegram.Modules;

internal class ModuleContext
{
    public ModuleContext(User user, Chat chat)
    {
        User = user;
        Chat = chat;
    }

    public User User { get; }
    public Chat Chat { get; }
}
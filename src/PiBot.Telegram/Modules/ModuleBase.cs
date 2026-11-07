using Telegram.Bot;
using Telegram.Bot.Types;

namespace PiBot.Telegram.Modules;

internal abstract class ModuleBase(ITelegramBotClient botClient)
{
    public abstract string CommandName { get; }
    public virtual string[] Aliases => [];

    public ModuleContext Context
    {
        get => field ?? throw new NullReferenceException($"{nameof(Context)} must be set");
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (field != null) throw new ArgumentException($"{nameof(Context)} already set");
            field = value;
        }
    }

    protected readonly ITelegramBotClient BotClient = botClient;

    public abstract Task<Message> ExecuteAsync(string subCommand = "", params string[] args);
}
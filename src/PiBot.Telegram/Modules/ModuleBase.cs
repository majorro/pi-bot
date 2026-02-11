using Telegram.Bot;
using Telegram.Bot.Types;

namespace PiBot.Telegram.Modules;

internal abstract class ModuleBase(ITelegramBotClient botClient)
{
    public abstract string CommandName { get; }
    public virtual string[] Aliases => [];
    protected virtual int ArgumentCount => 0;

    public ModuleContext Context
    {
        get => _context ?? throw new NullReferenceException($"{nameof(Context)} must be set");
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_context != null) throw new ArgumentException($"{nameof(Context)} already set");
            _context = value;
        }
    }

    protected readonly ITelegramBotClient BotClient = botClient;
    private ModuleContext? _context;

    public abstract Task<Message> ExecuteAsync(string subCommand = "", params string[] args);
}
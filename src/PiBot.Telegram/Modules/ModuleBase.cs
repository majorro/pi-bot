using Telegram.Bot;
using Telegram.Bot.Types;

namespace PiBot.Telegram.Modules;

internal abstract class ModuleBase
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

    protected readonly ITelegramBotClient _botClient;
    private ModuleContext? _context;

    protected ModuleBase(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public abstract Task<Message> ExecuteAsync(string subCommand = "", params string[] args);
}
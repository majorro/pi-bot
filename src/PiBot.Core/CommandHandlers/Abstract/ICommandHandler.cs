namespace PiBot.Core.CommandHandlers.Abstract;

public interface ICommandHandler
{
    public string[] Commands { get; }
    public Task HandleAsync(CancellationToken cancellationToken);
}
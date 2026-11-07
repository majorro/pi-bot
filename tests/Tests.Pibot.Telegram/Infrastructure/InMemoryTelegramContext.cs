using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PiBot.Data.Telegram;

namespace Tests.Pibot.Telegram.Infrastructure;

internal sealed class InMemoryTelegramContext() : TelegramPgContext(
    new DbContextOptionsBuilder<TelegramPgContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
    Options.Create(new TelegramPgConfig()))
{
    public int SavedEntityCount { get; private set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var savedEntityCount = await base.SaveChangesAsync(cancellationToken);
        SavedEntityCount += savedEntityCount;
        return savedEntityCount;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
}
using Microsoft.EntityFrameworkCore;
using PiBot.Core.Entities;

namespace PiBot.Core.Contracts;

public interface IReadonlyDbContext
{
    DbSet<Megaprikol> Megaprikols { get; init; }

    /// <inheritdoc cref="DbContext.SaveChangesAsync(CancellationToken)"/>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

}
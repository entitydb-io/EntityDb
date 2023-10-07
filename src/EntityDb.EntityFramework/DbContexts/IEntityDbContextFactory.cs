using EntityDb.EntityFramework.Sessions;
using Microsoft.EntityFrameworkCore;

namespace EntityDb.EntityFramework.DbContexts;

/// <summary>
///     Represents a type used to create instances of <typeparamref name="TDbContext"/>.
/// </summary>
/// <typeparam name="TDbContext">The type of the <see cref="DbContext"/>.</typeparam>
public interface IEntityDbContextFactory<out TDbContext>
{
    internal TDbContext Create(EntityFrameworkSnapshotSessionOptions snapshotSessionOptions);

    /// <summary>
    ///     Create a new instance of <typeparamref name="TDbContext"/>.
    /// </summary>
    /// <param name="snapshotSessionOptionsName">The agent's use case for the <see cref="DbContext"/>.</param>
    /// <returns>A new instance of <typeparamref name="TDbContext"/>.</returns>
    TDbContext Create(string snapshotSessionOptionsName);
}

using EntityDb.EntityFramework.Sessions;
using Microsoft.EntityFrameworkCore;

namespace EntityDb.EntityFramework.DbContexts;

/// <summary>
///     A type of a <see cref="DbContext"/> that can be used for EntityDb purposes.
/// </summary>
/// <typeparam name="TDbContext">The type of the <see cref="DbContext"/></typeparam>
public interface IEntityDbContext<out TDbContext>
    where TDbContext : DbContext, IEntityDbContext<TDbContext>
{
    /// <summary>
    ///     Returns a new <typeparamref name="TDbContext"/> that will be configured using <paramref name="entityFrameworkSnapshotSessionOptions"/>.
    /// </summary>
    /// <param name="entityFrameworkSnapshotSessionOptions">The options for the database</param>
    /// <returns>A new <typeparamref name="TDbContext"/> that will be configured using <paramref name="entityFrameworkSnapshotSessionOptions"/>.</returns>
    static abstract TDbContext Construct(EntityFrameworkSnapshotSessionOptions entityFrameworkSnapshotSessionOptions);
}

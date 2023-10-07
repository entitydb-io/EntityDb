using EntityDb.Abstractions.Snapshots;
using EntityDb.Common.Extensions;
using EntityDb.EntityFramework.DbContexts;
using EntityDb.EntityFramework.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace EntityDb.EntityFramework.Extensions;

/// <summary>
///     Extensions for service collections.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Don't need coverage for non-test mode.")]
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds a production-ready implementation of <see cref="ISnapshotRepositoryFactory{TSnapshot}" /> to a service
    ///     collection.
    /// </summary>
    /// <typeparam name="TSnapshot">The type of the snapshot stored in the repository.</typeparam>
    /// <typeparam name="TDbContext">The type of the snapshot stored in the repository.</typeparam>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="testMode">Modifies the behavior of the repository to accomodate tests.</param>
    public static void AddEntityFrameworkSnapshots<TSnapshot, TDbContext>(this IServiceCollection serviceCollection, bool testMode = false)
        where TSnapshot : class, IEntityFrameworkSnapshot<TSnapshot>
        where TDbContext : DbContext, IEntityDbContext<TDbContext>
    {
        serviceCollection.Add<IEntityDbContextFactory<TDbContext>, EntityDbContextFactory<TDbContext>>
        (
            testMode ? ServiceLifetime.Singleton : ServiceLifetime.Transient
        );

        serviceCollection.Add<EntityFrameworkSnapshotRepositoryFactory<TSnapshot, TDbContext>>
        (
            testMode ? ServiceLifetime.Singleton : ServiceLifetime.Transient
        );

        serviceCollection.Add<ISnapshotRepositoryFactory<TSnapshot>>
        (
            testMode ? ServiceLifetime.Singleton : ServiceLifetime.Transient,
            serviceProvider => serviceProvider
                .GetRequiredService<EntityFrameworkSnapshotRepositoryFactory<TSnapshot, TDbContext>>()
                .UseTestMode(serviceProvider, testMode)
        );
    }
}

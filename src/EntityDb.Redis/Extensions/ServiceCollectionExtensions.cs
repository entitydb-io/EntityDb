using EntityDb.Abstractions.Snapshots;
using EntityDb.Common.Envelopes;
using EntityDb.Common.Extensions;
using EntityDb.Json.Envelopes;
using EntityDb.Redis.ConnectionMultiplexers;
using EntityDb.Redis.Snapshots;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace EntityDb.Redis.Extensions;

/// <summary>
///     Extensions for service collections.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Don't need coverage for non-test mode.")]
public static class ServiceCollectionExtensions
{
    internal static void AddJsonElementEnvelopeService(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IEnvelopeService<byte[]>, JsonBytesEnvelopeService>();
    }

    /// <summary>
    ///     Adds a production-ready implementation of <see cref="ISnapshotRepositoryFactory{TSnapshot}" /> to a service
    ///     collection.
    /// </summary>
    /// <typeparam name="TSnapshot">The type of the snapshot stored in the repository.</typeparam>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="testMode">Modifies the behavior of the repository to accomodate tests.</param>
    public static void AddRedisSnapshots<TSnapshot>(this IServiceCollection serviceCollection, bool testMode = false)
    {
        serviceCollection.AddJsonElementEnvelopeService();

        serviceCollection.AddSingleton<ConnectionMultiplexerFactory>();

        serviceCollection.Add<RedisSnapshotRepositoryFactory<TSnapshot>>
        (
            testMode ? ServiceLifetime.Singleton : ServiceLifetime.Transient
        );

        serviceCollection.Add
        (
            testMode ? ServiceLifetime.Singleton : ServiceLifetime.Transient,
            serviceProvider => serviceProvider
                .GetRequiredService<RedisSnapshotRepositoryFactory<TSnapshot>>()
                .UseTestMode(testMode)
        );
    }
}

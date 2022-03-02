﻿using EntityDb.Abstractions.Snapshots;
using EntityDb.Common.Extensions;
using EntityDb.Redis.Snapshots;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace EntityDb.Redis.Extensions;

/// <summary>
///     Extensions for service collections.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds a production-ready implementation of <see cref="ISnapshotRepositoryFactory{TEntity}" /> to a service
    ///     collection.
    /// </summary>
    /// <typeparam name="TSnapshot">The type of the snapshot stored in the repository.</typeparam>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="keyNamespace">The namespace used to build a Redis key.</param>
    /// <param name="getConnectionString">A function that retrieves the Redis connection string.</param>
    /// <param name="testMode">Modifies the behavior of the repository to accomodate tests.</param>
    public static void AddRedisSnapshots<TSnapshot>(this IServiceCollection serviceCollection, string keyNamespace,
        Func<IConfiguration, string> getConnectionString, bool testMode = false)
    {
        serviceCollection.Add
        (
            testMode ? ServiceLifetime.Singleton : ServiceLifetime.Scoped,
            serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();

                var connectionString = getConnectionString.Invoke(configuration);

                return RedisSnapshotRepositoryFactory<TSnapshot>
                    .Create(serviceProvider, connectionString, keyNamespace)
                    .UseTestMode(testMode);
            }
        );
    }
}

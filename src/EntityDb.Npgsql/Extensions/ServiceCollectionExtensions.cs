using EntityDb.Abstractions.Transactions;
using EntityDb.Common.Envelopes;
using EntityDb.Common.Extensions;
using EntityDb.Json.Envelopes;
using EntityDb.Npgsql.Converters;
using EntityDb.Npgsql.Queries;
using EntityDb.Npgsql.Transactions;
using EntityDb.SqlDb.Converters;
using EntityDb.SqlDb.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace EntityDb.Npgsql.Extensions;

/// <summary>
///     Extensions for service collections.
/// </summary>
public static class ServiceCollectionExtensions
{
    internal static void AddSqlDbEnvelopeService(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IEnvelopeService<string>, JsonStringEnvelopeService>();
    }

    /// <summary>
    ///     Adds a production-ready implementation of <see cref="ITransactionRepositoryFactory" /> to a service
    ///     collection.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="testMode">Modifies the behavior of the repository to accomodate tests.</param>
    /// <param name="autoProvision">Modifies the behavior of the repository to auto-provision collections.</param>
    public static void AddNpgsqlTransactions(this IServiceCollection serviceCollection,
        bool testMode = false, bool autoProvision = false)
    {
        serviceCollection.AddSqlDbEnvelopeService();

        serviceCollection.Add<ISqlConverter<NpgsqlQueryOptions>, NpgsqlConverter>
        (
            testMode ? ServiceLifetime.Singleton : ServiceLifetime.Transient
        );

        serviceCollection.Add<NpgsqlTransactionRepositoryFactory>
        (
            testMode ? ServiceLifetime.Singleton : ServiceLifetime.Transient
        );

        serviceCollection.Add<ITransactionRepositoryFactory>
        (
            testMode ? ServiceLifetime.Singleton : ServiceLifetime.Transient,
            serviceProvider => serviceProvider
                .GetRequiredService<NpgsqlTransactionRepositoryFactory>()
                .UseTestMode(testMode)
                .UseAutoProvision(serviceProvider, autoProvision)
        );
    }
}

using EntityDb.Abstractions.Transactions;
using EntityDb.Common.Extensions;
using EntityDb.MongoDb.Envelopes;
using EntityDb.MongoDb.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace EntityDb.MongoDb.Extensions;

/// <summary>
///     Extensions for service collections.
/// </summary>
public static class ServiceCollectionExtensions
{
    internal static void AddBsonDocumentEnvelopeService(this IServiceCollection serviceCollection,
        bool removeTypeDiscriminatorProperty)
    {
        serviceCollection.AddSingleton(serviceProvider =>
            MongoDbEnvelopeService.Create(serviceProvider, removeTypeDiscriminatorProperty));
    }

    /// <summary>
    ///     Adds a production-ready implementation of <see cref="ITransactionRepositoryFactory" /> to a service
    ///     collection.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="testMode">Modifies the behavior of the repository to accomodate tests.</param>
    /// <param name="autoProvision">Modifies the behavior of the repository to auto-provision collections.</param>
    public static void AddMongoDbTransactions(this IServiceCollection serviceCollection,
        bool testMode = false, bool autoProvision = false)
    {
        serviceCollection.AddBsonDocumentEnvelopeService(true);

        serviceCollection.Add<MongoDbTransactionRepositoryFactory>
        (
            testMode ? ServiceLifetime.Singleton : ServiceLifetime.Transient
        );

        serviceCollection.Add<ITransactionRepositoryFactory>
        (
            testMode ? ServiceLifetime.Singleton : ServiceLifetime.Transient,
            serviceProvider => serviceProvider
                .GetRequiredService<MongoDbTransactionRepositoryFactory>()
                .UseTestMode(testMode)
                .UseAuthProvision(serviceProvider, autoProvision)
        );
    }
}

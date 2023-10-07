using EntityDb.Abstractions.Agents;
using EntityDb.Abstractions.Entities;
using EntityDb.Abstractions.Projections;
using EntityDb.Abstractions.Sources;
using EntityDb.Abstractions.Transactions.Builders;
using EntityDb.Common.Entities;
using EntityDb.Common.Projections;
using EntityDb.Common.Sources.Processors;
using EntityDb.Common.Sources.Processors.Queues;
using EntityDb.Common.Sources.ReprocessorQueues;
using EntityDb.Common.Sources.Subscribers;
using EntityDb.Common.Transactions.Builders;
using EntityDb.Common.TypeResolvers;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace EntityDb.Common.Extensions;

/// <summary>
///     Extensions for service collections.
/// </summary>
public static class ServiceCollectionExtensions
{
    internal static void Add<TService>(this IServiceCollection serviceCollection, ServiceLifetime serviceLifetime,
        Func<IServiceProvider, TService> serviceFactory)
        where TService : class
    {
        serviceCollection.Add(new ServiceDescriptor(typeof(TService), serviceFactory, serviceLifetime));
    }

    internal static void Add<TService, TImplementation>(this IServiceCollection serviceCollection, ServiceLifetime serviceLifetime)
        where TService : class
        where TImplementation : TService
    {
        serviceCollection.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), serviceLifetime));
    }

    internal static void Add<TService>(this IServiceCollection serviceCollection, ServiceLifetime serviceLifetime)
        where TService : class
    {
        serviceCollection.Add(new ServiceDescriptor(typeof(TService), typeof(TService), serviceLifetime));
    }

    /// <ignore />
    [Obsolete("Please register your TTransactionProcessor yourself. You may use any scope you want. You will need to call services.AddSourceProcessorQueue(), and you may enqueue source processing by injecting ISourceProcessorQueue and calling Enqueue. There is a generic extension method available if you don't want to implement ISourceProcessorQueueItem.", true)]
    public static void AddTransactionProcessorSubscriber<TTransactionProcessor>(this IServiceCollection serviceCollection,
        bool testMode, Func<IServiceProvider, TTransactionProcessor> transactionProcessorFactory)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Registers a queue for processing sources (e.g., transactions) as they are committed.
    ///     For test mode, the queue is not actually a queue and will immediately process the source.
    ///     For non-test mode, the queue uses a buffer block.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="testMode">Whether or not to run in test mode.</param>
    [ExcludeFromCodeCoverage(Justification = "Tests are only meant to run in test mode.")]
    public static void AddSourceProcessorQueue(this IServiceCollection serviceCollection,
        bool testMode)
    {
        if (testMode)
        {
            serviceCollection.AddSingleton<ISourceProcessorQueue, TestModeSourceProcessorQueue>();
        }
        else
        {
            serviceCollection.AddSingleton<BufferBlockSourceProcessorQueue>();

            serviceCollection.AddSingleton<ISourceProcessorQueue>(serviceProvider =>
                serviceProvider.GetRequiredService<BufferBlockSourceProcessorQueue>());

            serviceCollection.AddHostedService(serviceProvider =>
                serviceProvider.GetRequiredService<BufferBlockSourceProcessorQueue>());
        }
    }

    /// <summary>
    ///     Registers a queue for re-processing sources (e.g., transactions) after they have
    ///     already been committed (and potentially processed before). For test mode, the queue is
    ///     not actually a queue and will immediately reprocess sources. For non-test mode, the
    ///     queue uses a buffer block.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="testMode">Whether or not to run in test mode.</param>
    [ExcludeFromCodeCoverage(Justification = "Tests are only meant to run in test mode.")]
    public static void AddSourceReprocessorQueue(this IServiceCollection serviceCollection, bool testMode = false)
    {
        if (testMode)
        {
            serviceCollection.AddSingleton<ITransactionReprocessorQueue, TestModeTransactionReprocessorQueue>();
        }
        else
        {
            serviceCollection.AddSingleton<BufferBlockTransactionReprocessorQueue>();

            serviceCollection.AddHostedService(serviceProvider =>
                serviceProvider.GetRequiredService<BufferBlockTransactionReprocessorQueue>());

            serviceCollection.AddSingleton<ITransactionReprocessorQueue>(serviceProvider =>
                serviceProvider.GetRequiredService<BufferBlockTransactionReprocessorQueue>());
        }
    }

    /// <summary>
    ///     Adds an internal implementation of <see cref="IPartialTypeResolver" /> which resolves types by using assembly
    ///     information.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    public static void AddDefaultPartialTypeResolver(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IPartialTypeResolver, DefaultPartialTypeResolver>();
    }

    /// <summary>
    ///     Adds an internal implementation of <see cref="IPartialTypeResolver" /> which resolves the given types based on
    ///     their
    ///     <see cref="MemberInfo.Name" />.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="types">The types that can be resolved by <see cref="MemberInfo.Name" />.</param>
    public static void AddMemberInfoNamePartialTypeResolver(this IServiceCollection serviceCollection, Type[] types)
    {
        serviceCollection.AddSingleton<IPartialTypeResolver>(_ =>
            new MemberInfoNamePartialTypeResolver(types));
    }

    /// <summary>
    ///     Adds an internal implementation of <see cref="ITypeResolver" /> to a service collection.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <remarks>
    ///     LIFO means Last In, First Out. In other words, the last registered implementation of
    ///     <see cref="IPartialTypeResolver" /> will be the first to attempt to resolve the desired type.
    /// </remarks>
    public static void AddLifoTypeResolver(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ITypeResolver, LifoTypeResolver>();
    }

    /// <summary>
    ///     Adds a custom implementation of <see cref="IAgentAccessor" /> to a service collection.
    /// </summary>
    /// <typeparam name="TAgentAccessor">The type of the agent accessor.</typeparam>
    /// <param name="serviceCollection">The service collection</param>
    public static void AddAgentAccessor<TAgentAccessor>(this IServiceCollection serviceCollection)
        where TAgentAccessor : class, IAgentAccessor
    {
        serviceCollection.AddSingleton<IAgentAccessor, TAgentAccessor>();
    }

    /// <summary>
    ///     Adds a transient <see cref="TransactionBuilder{TEntity}" /> and a transient implementation of
    ///     <see cref="IEntityRepositoryFactory{TEntity}" /> to a service collection.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public static void AddEntity<TEntity>(this IServiceCollection serviceCollection)
        where TEntity : IEntity<TEntity>
    {
        serviceCollection.AddTransient<ITransactionBuilderFactory<TEntity>, TransactionBuilderFactory<TEntity>>();

        serviceCollection.AddTransient<IEntityRepositoryFactory<TEntity>, EntityRepositoryFactory<TEntity>>();
    }

    /// <ignore />
    [Obsolete("Please use AddEntitySnapshotSourceSubscriber instead. This will be removed in a future version.")]
    public static void AddEntitySnapshotTransactionSubscriber<TEntity>(this IServiceCollection serviceCollection,
        string transactionSessionOptionsName, string snapshotSessionOptionsName)
        where TEntity : IEntity<TEntity>
    {
        serviceCollection.AddEntitySnapshotSourceSubscriber<TEntity>(transactionSessionOptionsName, snapshotSessionOptionsName);
    }

    /// <summary>
    ///     Adds a source subscriber that records snapshots of entities.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="transactionSessionOptionsName">The agent's intent for the transaction repository.</param>
    /// <param name="snapshotSessionOptionsName">The agent's intent for the snapshot repository.</param>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public static void AddEntitySnapshotSourceSubscriber<TEntity>(this IServiceCollection serviceCollection,
        string transactionSessionOptionsName, string snapshotSessionOptionsName)
        where TEntity : IEntity<TEntity>
    {
        serviceCollection.AddSingleton<ISourceSubscriber, EntitySnapshotSourceSubscriber<TEntity>>();
        serviceCollection.AddScoped(serviceProvider => EntitySnapshotSourceProcessor<TEntity>.Create(serviceProvider, transactionSessionOptionsName, snapshotSessionOptionsName));
    }

    /// <summary>
    ///     Adds projections for <typeparamref name="TProjection" />.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <typeparam name="TProjection">The type of the projection.</typeparam>
    public static void AddProjection<TProjection>(
        this IServiceCollection serviceCollection)
        where TProjection : IProjection<TProjection>
    {
        serviceCollection
            .AddTransient<IProjectionRepositoryFactory<TProjection>, ProjectionRepositoryFactory<TProjection>>();
    }

    /// <ignore />
    [Obsolete("Please use AddProjectionSnapshotSourceSubscriber instead. This will be removed in a future version.")]
    public static void AddProjectionSnapshotTransactionSubscriber<TProjection>(
        this IServiceCollection serviceCollection,
        string transactionSessionOptionsName, string snapshotSessionOptionsName)
        where TProjection : IProjection<TProjection>
    {
        serviceCollection.AddProjectionSnapshotSourceSubscriber<TProjection>(transactionSessionOptionsName, snapshotSessionOptionsName);
    }

    /// <summary>
    ///     Adds a transaction subscriber that records snapshots of projections.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="transactionSessionOptionsName">The agent's intent for the transaction repository.</param>
    /// <param name="snapshotSessionOptionsName">The agent's intent for the snapshot repository.</param>
    /// <typeparam name="TProjection">The type of the projection.</typeparam>
    public static void AddProjectionSnapshotSourceSubscriber<TProjection>(
        this IServiceCollection serviceCollection,
        string transactionSessionOptionsName, string snapshotSessionOptionsName)
        where TProjection : IProjection<TProjection>
    {

        serviceCollection.AddSingleton<ISourceSubscriber, ProjectionSnapshotSourceSubscriber<TProjection>>();
        serviceCollection.AddScoped(serviceProvider => ProjectionSnapshotSourceProcessor<TProjection>.Create(serviceProvider, transactionSessionOptionsName, snapshotSessionOptionsName));
    }
}

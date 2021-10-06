﻿using EntityDb.Abstractions.Entities;
using EntityDb.Abstractions.Loggers;
using EntityDb.Abstractions.Snapshots;
using EntityDb.Abstractions.Strategies;
using EntityDb.Abstractions.Transactions;
using EntityDb.Common.Extensions;
using EntityDb.Common.Queries;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace EntityDb.Common.Entities
{
    internal class EntityRepository<TEntity> : IEntityRepository<TEntity>
    {
        private readonly IConstructingStrategy<TEntity> _constructingStrategy;
        private readonly IVersioningStrategy<TEntity> _versioningStrategy;
        private readonly ILogger _logger;
        private readonly IEnumerable<ITransactionSubscriber<TEntity>> _transactionSubscribers;
        private readonly ITransactionRepository<TEntity> _transactionRepository;
        private readonly ISnapshotRepository<TEntity>? _snapshotRepository;

        public EntityRepository
        (
            ILoggerFactory loggerFactory,
            IConstructingStrategy<TEntity> constructingStrategy,
            IVersioningStrategy<TEntity> versioningStrategy,
            IEnumerable<ITransactionSubscriber<TEntity>> transactionSubscribers,
            ITransactionRepository<TEntity> transactionRepository,
            ISnapshotRepository<TEntity>? snapshotRepository = null
        )
        {
            _logger = loggerFactory.CreateLogger<EntityRepository<TEntity>>();
            _constructingStrategy = constructingStrategy;
            _versioningStrategy = versioningStrategy;
            _transactionSubscribers = transactionSubscribers;
            _transactionRepository = transactionRepository;
            _snapshotRepository = snapshotRepository;
        }
        
        private void Publish(ITransaction<TEntity> transaction)
        {
            foreach (var transactionSubscriber in _transactionSubscribers)
            {
                try
                {
                    transactionSubscriber.Notify(transaction);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"{transactionSubscriber.GetType()}.{nameof(transactionSubscriber.Notify)}({transaction.Id})");
                }
            }
        }
        
        public async Task<TEntity?> GetSnapshotOrDefault(Guid entityId)
        {
            if (_snapshotRepository != null)
            {
                return await _snapshotRepository.GetSnapshot(entityId);
            }

            return default;
        }

        public async Task<TEntity> GetCurrentOrConstruct(Guid entityId)
        {
            var snapshot = await GetSnapshotOrDefault(entityId);

            var entity = snapshot ?? _constructingStrategy.Construct(entityId);
            
            var versionNumber = _versioningStrategy.GetVersionNumber(entity);

            var factQuery = new GetEntityQuery(entityId, versionNumber);

            var facts = await _transactionRepository.GetFacts(factQuery);

            entity = entity.Reduce(facts);

            return entity;
        }

        public async Task<bool> PutTransaction(ITransaction<TEntity> transaction)
        {
            var success = await _transactionRepository.PutTransaction(transaction);

            if (success == false)
            {
                return false;
            }

            Publish(transaction);
            
            return true;
        }

        [ExcludeFromCodeCoverage]
        public void Dispose()
        {
            DisposeAsync().AsTask().Wait();
        }

        public async ValueTask DisposeAsync()
        {
            await _transactionRepository.DisposeAsync();

            if (_snapshotRepository != null)
            {
                await _snapshotRepository.DisposeAsync();
            }
        }

        public static EntityRepository<TEntity> Create
        (
            IServiceProvider serviceProvider,
            ITransactionRepository<TEntity> transactionRepository,
            ISnapshotRepository<TEntity>? snapshotRepository = null
        )
        {
            if (snapshotRepository == null)
            {
                return ActivatorUtilities.CreateInstance<EntityRepository<TEntity>>(serviceProvider, transactionRepository);
            }
            
            return ActivatorUtilities.CreateInstance<EntityRepository<TEntity>>(serviceProvider, transactionRepository,
                snapshotRepository);
        }
    }
}

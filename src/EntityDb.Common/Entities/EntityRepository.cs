﻿using EntityDb.Abstractions.Entities;
using EntityDb.Abstractions.Snapshots;
using EntityDb.Abstractions.Transactions;
using EntityDb.Common.Extensions;
using EntityDb.Common.Queries;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace EntityDb.Common.Entities
{
    internal class EntityRepository<TEntity> : IEntityRepository<TEntity>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITransactionRepository<TEntity> _transactionRepository;
        private readonly ISnapshotRepository<TEntity>? _snapshotRepository;

        public ITransactionRepository<TEntity> TransactionRepository => _transactionRepository;
        public ISnapshotRepository<TEntity>? SnapshotRepository => _snapshotRepository;

        public EntityRepository
        (
            IServiceProvider serviceProvider,
            ITransactionRepository<TEntity> transactionRepository,
            ISnapshotRepository<TEntity>? snapshotRepository = null
        )
        {
            _serviceProvider = serviceProvider;
            _transactionRepository = transactionRepository;
            _snapshotRepository = snapshotRepository;
        }

        public async Task<TEntity> Get(Guid entityId)
        {
            TEntity? snapshot = default;

            if (_snapshotRepository != null)
            {
                snapshot = await _snapshotRepository.GetSnapshot(entityId);
            }

            var entity = snapshot ?? _serviceProvider.Construct<TEntity>(entityId);

            var versionNumber = _serviceProvider.GetVersionNumber(entity);

            var factQuery = new GetEntityQuery(entityId, versionNumber);

            var facts = await _transactionRepository.GetFacts(factQuery);

            entity = entity.Reduce(facts);

            return entity;
        }

        public Task<bool> Put(ITransaction<TEntity> transaction)
        {
            if (_snapshotRepository != null)
            {
                var lastCommands = transaction.Commands
                    .GroupBy(command => command.EntityId)
                    .Select(group => group.Last());

                foreach (var lastCommand in lastCommands)
                {
                    if (_serviceProvider.ShouldPutSnapshot(lastCommand.PreviousSnapshot, lastCommand.NextSnapshot))
                    {
                        _snapshotRepository.PutSnapshot(lastCommand.EntityId, lastCommand.NextSnapshot);
                    }
                }
            }

            return _transactionRepository.PutTransaction(transaction);
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
    }
}

﻿using EntityDb.Abstractions.Leases;
using EntityDb.Abstractions.Loggers;
using EntityDb.Abstractions.Queries;
using EntityDb.Abstractions.Transactions;
using EntityDb.Common.Queries;
using EntityDb.MongoDb.Envelopes;
using EntityDb.MongoDb.Queries;
using EntityDb.MongoDb.Queries.FilterBuilders;
using EntityDb.MongoDb.Queries.SortBuilders;
using EntityDb.MongoDb.Sessions;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EntityDb.MongoDb.Documents
{
    internal sealed record LeaseDocument : DocumentBase, IEntityDocument
    {
        public Guid EntityId { get; init; }
        public ulong EntityVersionNumber { get; init; }
        public string Scope { get; init; } = default!;
        public string Label { get; init; } = default!;
        public string Value { get; init; } = default!;

        private static readonly LeaseFilterBuilder _leaseFilterBuilder = new();
        private static readonly LeaseSortBuilder _leaseSortBuilder = new();

        public const string CollectionName = "Leases";

        public static readonly string[] HoistedFieldNames = new[]
        {
            nameof(Scope),
            nameof(Label),
            nameof(Value),
        };

        public static Task ProvisionCollection
        (
            IMongoDatabase mongoDatabase
        )
        {
            return ProvisionCollection
            (
                mongoDatabase,
                CollectionName,
                new[]
                {
                    new CreateIndexModel<BsonDocument>
                    (
                        keys: IndexKeys.Combine
                        (
                            IndexKeys.Descending(nameof(Scope)),
                            IndexKeys.Descending(nameof(Label)),
                            IndexKeys.Descending(nameof(Value))
                        ),
                        options: new CreateIndexOptions
                        {
                            Name = $"Uniqueness Constraint",
                            Unique = true,
                        }
                    ),
                }
            );
        }

        public static IReadOnlyCollection<LeaseDocument> BuildMany<TEntity>
        (
            ILogger logger,
            ITransaction<TEntity> transaction,
            ITransactionCommand<TEntity> transactionCommand
        )
        {
            return transactionCommand.InsertLeases
                .Select(insertLease => new LeaseDocument
                {
                    TransactionTimeStamp = transaction.TimeStamp,
                    TransactionId = transaction.Id,
                    EntityId = transactionCommand.EntityId,
                    EntityVersionNumber = transactionCommand.ExpectedPreviousVersionNumber + 1,
                    Scope = insertLease.Scope,
                    Label = insertLease.Label,
                    Value = insertLease.Value,
                    Data = BsonDocumentEnvelope.Deconstruct(insertLease, logger)
                })
                .ToArray();
        }

        public static async Task InsertMany
        (
            IClientSessionHandle clientSessionHandle,
            IMongoDatabase mongoDatabase,
            IReadOnlyCollection<LeaseDocument> leaseDocuments
        )
        {
            await InsertMany
            (
                clientSessionHandle,
                GetMongoCollection(mongoDatabase, CollectionName),
                leaseDocuments
            );
        }

        public static Task<Guid[]> GetTransactionIds
        (
            IMongoDbSession mongoDbSession,
            ILeaseQuery leaseQuery
        )
        {
            return mongoDbSession.ExecuteGuidQuery
            (
                (clientSessionHandle, mongoDatabase) => new TransactionIdQuery<LeaseDocument>
                {
                    ClientSessionHandle = clientSessionHandle,
                    MongoCollection = GetMongoCollection(mongoDatabase, CollectionName),
                    Filter = leaseQuery.GetFilter(_leaseFilterBuilder),
                    Sort = leaseQuery.GetSort(_leaseSortBuilder),
                    DistinctSkip = leaseQuery.Skip,
                    DistinctLimit = leaseQuery.Take
                }
            );
        }

        public static Task<Guid[]> GetEntityIds
        (
            IMongoDbSession mongoDbSession,
            ILeaseQuery leaseQuery
        )
        {
            return mongoDbSession.ExecuteGuidQuery
            (
                (clientSessionHandle, mongoDatabase) => new EntityIdQuery<LeaseDocument>
                {
                    ClientSessionHandle = clientSessionHandle,
                    MongoCollection = GetMongoCollection(mongoDatabase, CollectionName),
                    Filter = leaseQuery.GetFilter(_leaseFilterBuilder),
                    Sort = leaseQuery.GetSort(_leaseSortBuilder),
                    DistinctSkip = leaseQuery.Skip,
                    DistinctLimit = leaseQuery.Take
                }
            );
        }

        public static Task<ILease[]> GetData
        (
            IMongoDbSession mongoDbSession,
            ILeaseQuery leaseQuery
        )
        {
            return mongoDbSession.ExecuteDataQuery<LeaseDocument, ILease>
            (
                (clientSessionHandle, mongoDatabase) => new DataQuery<LeaseDocument>
                {
                    ClientSessionHandle = clientSessionHandle,
                    MongoCollection = GetMongoCollection(mongoDatabase, CollectionName),
                    Filter = leaseQuery.GetFilter(_leaseFilterBuilder),
                    Sort = leaseQuery.GetSort(_leaseSortBuilder),
                    Skip = leaseQuery.Skip,
                    Limit = leaseQuery.Take
                }
            );
        }

        public static async Task DeleteMany
        (
            IClientSessionHandle clientSessionHandle,
            IMongoDatabase mongoDatabase,
            Guid entityId,
            IReadOnlyCollection<ILease> deleteLeases
        )
        {
            if (deleteLeases.Count == 0)
            {
                return;
            }

            var deleteLeasesQuery = new DeleteLeasesQuery(entityId, deleteLeases);

            await DeleteMany
            (
                clientSessionHandle,
                GetMongoCollection(mongoDatabase, CollectionName),
                deleteLeasesQuery.GetFilter(_leaseFilterBuilder)
            );
        }
    }
}

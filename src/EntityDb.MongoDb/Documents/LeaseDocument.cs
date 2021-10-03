﻿using EntityDb.Abstractions.Leases;
using EntityDb.Abstractions.Queries;
using EntityDb.Common.Queries;
using EntityDb.MongoDb.Envelopes;
using EntityDb.MongoDb.Queries;
using EntityDb.MongoDb.Queries.FilterBuilders;
using EntityDb.MongoDb.Queries.SortBuilders;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EntityDb.MongoDb.Documents
{
    internal sealed record LeaseDocument
    (
        DateTime TransactionTimeStamp,
        Guid TransactionId,
        Guid EntityId,
        ulong EntityVersionNumber,
        string Scope,
        string Label,
        string Value,
        BsonDocumentEnvelope Data,
        ObjectId? _id = null
    ) : DocumentBase
    (
        TransactionTimeStamp,
        TransactionId,
        Data,
        _id
    ), IEntityDocument
    {
        private static readonly LeaseFilterBuilder _leaseFilterBuilder = new();
        private static readonly LeaseSortBuilder _leaseSortBuilder = new();

        public const string CollectionName = "Leases";

        public static readonly string[] HoistedFieldNames = new[]
        {
            nameof(Scope),
            nameof(Label),
            nameof(Value),
        };

        private static IMongoCollection<BsonDocument> GetCollection(IMongoDatabase mongoDatabase)
        {
            return mongoDatabase.GetCollection<BsonDocument>(CollectionName);
        }

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

        public static async Task InsertMany
        (
            IClientSessionHandle clientSessionHandle,
            IMongoDatabase mongoDatabase,
            IEnumerable<LeaseDocument> leaseDocuments
        )
        {
            await InsertMany
            (
                clientSessionHandle,
                GetCollection(mongoDatabase),
                leaseDocuments
            );
        }

        public static TransactionIdQuery<LeaseDocument> GetTransactionIds
        (
            IClientSessionHandle? clientSessionHandle,
            IMongoDatabase mongoDatabase,
            ILeaseQuery leaseQuery
        )
        {
            return new TransactionIdQuery<LeaseDocument>
            {
                ClientSessionHandle = clientSessionHandle,
                MongoCollection = GetCollection(mongoDatabase),
                Filter = leaseQuery.GetFilter(_leaseFilterBuilder),
                Sort = leaseQuery.GetSort(_leaseSortBuilder),
                DistinctSkip = leaseQuery.Skip,
                DistinctLimit = leaseQuery.Take
            };
        }

        public static EntityIdQuery<LeaseDocument> GetEntityIds
        (
            IClientSessionHandle? clientSessionHandle,
            IMongoDatabase mongoDatabase,
            ILeaseQuery leaseQuery
        )
        {
            return new EntityIdQuery<LeaseDocument>
            {
                ClientSessionHandle = clientSessionHandle,
                MongoCollection = GetCollection(mongoDatabase),
                Filter = leaseQuery.GetFilter(_leaseFilterBuilder),
                Sort = leaseQuery.GetSort(_leaseSortBuilder),
                DistinctSkip = leaseQuery.Skip,
                DistinctLimit = leaseQuery.Take
            };
        }

        public static DataQuery<LeaseDocument> GetData
        (
            IClientSessionHandle? clientSessionHandle,
            IMongoDatabase mongoDatabase,
            ILeaseQuery leaseQuery
        )
        {
            return new DataQuery<LeaseDocument>
            {
                ClientSessionHandle = clientSessionHandle,
                MongoCollection = GetCollection(mongoDatabase),
                Filter = leaseQuery.GetFilter(_leaseFilterBuilder),
                Sort = leaseQuery.GetSort(_leaseSortBuilder),
                Skip = leaseQuery.Skip,
                Limit = leaseQuery.Take
            };
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
                GetCollection(mongoDatabase),
                deleteLeasesQuery.GetFilter(_leaseFilterBuilder)
            );
        }
    }
}

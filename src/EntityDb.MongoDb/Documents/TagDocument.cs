using EntityDb.Abstractions.Loggers;
using EntityDb.Abstractions.Queries;
using EntityDb.Abstractions.Transactions;
using EntityDb.Abstractions.Transactions.Steps;
using EntityDb.Common.Queries;
using EntityDb.MongoDb.Commands;
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

namespace EntityDb.MongoDb.Documents
{
    internal sealed record TagDocument : DocumentBase, IEntityDocument
    {
        public const string CollectionName = "Tags";

        public static readonly TagFilterBuilder _filterBuilder = new();

        public static readonly TagSortBuilder _sortBuilder = new();

        public static readonly string[] HoistedFieldNames = { nameof(Label), nameof(Value) };

        public string Label { get; init; } = default!;
        public string Value { get; init; } = default!;
        public Guid EntityId { get; init; }
        public ulong EntityVersionNumber { get; init; }

        public static IReadOnlyCollection<TagDocument>? BuildInsert<TEntity>
        (
            ITransaction<TEntity> transaction,
            ITagTransactionStep<TEntity> tagTransactionStep,
            ILogger logger
        )
        {
            var insertTags = tagTransactionStep.Tags.Insert;

            if (insertTags.Length == 0)
            {
                return null;
            }

            return insertTags
                .Select(insertTag => new TagDocument
                {
                    TransactionTimeStamp = transaction.TimeStamp,
                    TransactionId = transaction.Id,
                    EntityId = tagTransactionStep.EntityId,
                    EntityVersionNumber = tagTransactionStep.TaggedAtEntityVersionNumber,
                    Label = insertTag.Label,
                    Value = insertTag.Value,
                    Data = BsonDocumentEnvelope.Deconstruct(insertTag, logger)
                })
                .ToArray();
        }

        public static FilterDefinition<BsonDocument>? BuildDelete<TEntity>
        (
            ITransaction<TEntity> transaction,
            ITagTransactionStep<TEntity> tagTransactionStep
        )
        {
            var deleteTags = tagTransactionStep.Tags.Delete;

            if (deleteTags.Length == 0)
            {
                return null;
            }

            var deleteTagsQuery = new DeleteTagsQuery(tagTransactionStep.EntityId, deleteTags);

            return deleteTagsQuery.GetFilter(_filterBuilder);
        }

        public static InsertDocumentsCommand<TEntity, ITagTransactionStep<TEntity>, TagDocument> GetInsertCommand<TEntity>
        (
            IMongoSession mongoSession
        )
        {
            return new InsertDocumentsCommand<TEntity, ITagTransactionStep<TEntity>, TagDocument>
            (
                mongoSession,
                CollectionName,
                BuildInsert<TEntity>
            );
        }

        public static DocumentQuery<TagDocument> GetQuery
        (
            IMongoSession mongoSession,
            ITagQuery tagQuery
        )
        {
            return new DocumentQuery<TagDocument>
            (
                mongoSession,
                CollectionName,
                tagQuery.GetFilter(_filterBuilder),
                tagQuery.GetSort(_sortBuilder),
                tagQuery.Skip,
                tagQuery.Take
            );
        }

        public static DeleteDocumentsCommand<TEntity, ITagTransactionStep<TEntity>> GetDeleteCommand<TEntity>
        (
            IMongoSession mongoSession
        )
        {
            return new DeleteDocumentsCommand<TEntity, ITagTransactionStep<TEntity>>
            (
                mongoSession,
                CollectionName,
                BuildDelete<TEntity>
            );
        }
    }
}

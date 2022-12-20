using EntityDb.Abstractions.Commands;
using EntityDb.Abstractions.Queries;
using EntityDb.Abstractions.Transactions;
using EntityDb.Abstractions.ValueObjects;
using EntityDb.Common.Envelopes;
using EntityDb.Common.Queries;
using EntityDb.MongoDb.Commands;
using EntityDb.MongoDb.Queries;
using EntityDb.MongoDb.Queries.FilterBuilders;
using EntityDb.MongoDb.Queries.SortBuilders;
using MongoDB.Bson;

namespace EntityDb.MongoDb.Documents;

internal sealed record TagDocument : DocumentBase, IEntityDocument
{
    public const string CollectionName = "Tags";

    private static readonly TagFilterBuilder FilterBuilder = new();

    private static readonly TagSortBuilder SortBuilder = new();

    public string Label { get; init; } = default!;
    public string Value { get; init; } = default!;
    public Id EntityId { get; init; }
    public VersionNumber EntityVersionNumber { get; init; }

    public static InsertDocumentsCommand<TagDocument> GetInsertCommand
    (
        IEnvelopeService<BsonDocument> envelopeService,
        ITransaction transaction,
        ITransactionCommand transactionCommand,
        IAddTagsCommand addTagsCommand
    )
    {
        var tagDocuments = addTagsCommand.GetTags()
            .Select(insertTag => new TagDocument
            {
                TransactionTimeStamp = transaction.TimeStamp,
                TransactionId = transaction.Id,
                EntityId = transactionCommand.EntityId,
                EntityVersionNumber = transactionCommand.EntityVersionNumber,
                DataType = insertTag.GetType().Name,
                Data = envelopeService.Serialize(insertTag),
                Label = insertTag.Label,
                Value = insertTag.Value
            })
            .ToArray();

        return new InsertDocumentsCommand<TagDocument>
        (
            CollectionName,
            tagDocuments
        );
    }

    public static DocumentQuery<TagDocument> GetQuery
    (
        ITagQuery tagQuery
    )
    {
        return new DocumentQuery<TagDocument>
        (
            CollectionName,
            tagQuery.GetFilter(FilterBuilder),
            tagQuery.GetSort(SortBuilder),
            tagQuery.Skip,
            tagQuery.Take,
            tagQuery.Options as MongoDbQueryOptions
        );
    }

    public static DeleteDocumentsCommand GetDeleteCommand
    (
        ITransactionCommand transactionCommand,
        IDeleteTagsCommand deleteTagsCommand
    )
    {
        var deleteTagsQuery = new DeleteTagsQuery(transactionCommand.EntityId, deleteTagsCommand.GetTags().ToArray());

        return new DeleteDocumentsCommand
        (
            CollectionName,
            deleteTagsQuery.GetFilter(FilterBuilder)
        );
    }
}

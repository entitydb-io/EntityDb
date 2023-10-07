using EntityDb.Abstractions.Commands;
using EntityDb.Abstractions.Queries;
using EntityDb.Abstractions.Transactions;
using EntityDb.Abstractions.ValueObjects;
using EntityDb.Common.Envelopes;
using EntityDb.Common.Queries;
using EntityDb.SqlDb.Commands;
using EntityDb.SqlDb.Queries;
using EntityDb.SqlDb.Queries.FilterBuilders;
using EntityDb.SqlDb.Queries.SortBuilders;

namespace EntityDb.SqlDb.Documents.Tag;

internal sealed record TagDocument : DocumentBase, IEntityDocument<TagDocument>
{
    public static string TableName => "Tags";

    private static readonly TagFilterBuilder FilterBuilder = new();

    private static readonly TagSortBuilder SortBuilder = new();

    public string Label { get; init; } = default!;
    public string Value { get; init; } = default!;
    public Id EntityId { get; init; }
    public VersionNumber EntityVersionNumber { get; init; }

    public static IDocumentReader<TagDocument> DocumentReader { get; } = new TagDocumentReader();

    public static IDocumentReader<TagDocument> TransactionIdDocumentReader { get; } = new TagTransactionIdDocumentReader();

    public static IDocumentReader<TagDocument> DataDocumentReader { get; } = new TagDataDocumentReader();

    public static IDocumentReader<TagDocument> EntityIdDocumentReader { get; } = new TagEntityIdDocumentReader();

    public static InsertDocumentsCommand<TagDocument> GetInsertCommand
    (
        IEnvelopeService<string> envelopeService,
        ITransaction transaction,
        ITransactionCommand transactionCommand,
        IAddTagsCommand addTagsCommand
    )
    {
        return new InsertDocumentsCommand<TagDocument>
        (
            TableName,
            addTagsCommand.GetTags()
                .Select(insertTag => new TagDocument
                {
                    TransactionTimeStamp = transaction.TimeStamp,
                    TransactionId = transaction.Id,
                    EntityId = transactionCommand.EntityId,
                    EntityVersionNumber = transactionCommand.EntityVersionNumber,
                    Label = insertTag.Label,
                    Value = insertTag.Value,
                    DataType = insertTag.GetType().Name,
                    Data = envelopeService.Serialize(insertTag)
                })
                .ToArray()
        );
    }

    public static DocumentQuery<TagDocument> GetQuery
    (
        ITagQuery tagQuery
    )
    {
        return new DocumentQuery<TagDocument>
        (
            tagQuery.GetFilter(FilterBuilder),
            tagQuery.GetSort(SortBuilder),
            tagQuery.Skip,
            tagQuery.Take,
            tagQuery.Options
        );
    }

    public static DeleteDocumentsCommand GetDeleteCommand
    (
        ITransactionCommand transactionCommand, IDeleteTagsCommand deleteTagsCommand
    )
    {
        var deleteTagsQuery = new DeleteTagsQuery(transactionCommand.EntityId, deleteTagsCommand.GetTags().ToArray());

        return new DeleteDocumentsCommand
        (
            TableName,
            deleteTagsQuery.GetFilter(FilterBuilder)
        );
    }

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            [nameof(TransactionId)] = TransactionId,
            [nameof(TransactionTimeStamp)] = TransactionTimeStamp,
            [nameof(EntityId)] = EntityId,
            [nameof(EntityVersionNumber)] = EntityVersionNumber,
            [nameof(DataType)] = DataType,
            [nameof(Data)] = Data,
            [nameof(Label)] = Label,
            [nameof(Value)] = Value,
        };
    }
}

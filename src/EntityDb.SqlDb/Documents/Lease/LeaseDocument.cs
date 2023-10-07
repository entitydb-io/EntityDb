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

namespace EntityDb.SqlDb.Documents.Lease;

internal sealed record LeaseDocument : DocumentBase, IEntityDocument<LeaseDocument>
{
    public static string TableName => "Leases";

    private static readonly LeaseFilterBuilder FilterBuilder = new();

    private static readonly LeaseSortBuilder SortBuilder = new();

    public string Scope { get; init; } = default!;
    public string Label { get; init; } = default!;
    public string Value { get; init; } = default!;
    public Id EntityId { get; init; }
    public VersionNumber EntityVersionNumber { get; init; }

    public static IDocumentReader<LeaseDocument> DocumentReader { get; } = new LeaseDocumentReader();

    public static IDocumentReader<LeaseDocument> TransactionIdDocumentReader { get; } = new LeaseTransactionIdDocumentReader();

    public static IDocumentReader<LeaseDocument> DataDocumentReader { get; } = new LeaseDataDocumentReader();

    public static IDocumentReader<LeaseDocument> EntityIdDocumentReader { get; } = new LeaseEntityIdDocumentReader();

    public static InsertDocumentsCommand<LeaseDocument> GetInsertCommand
    (
        IEnvelopeService<string> envelopeService,
        ITransaction transaction,
        ITransactionCommand transactionCommand,
        IAddLeasesCommand addLeasesCommand
    )
    {
        return new InsertDocumentsCommand<LeaseDocument>
        (
            TableName,
            addLeasesCommand.GetLeases()
                .Select(insertLease => new LeaseDocument
                {
                    TransactionTimeStamp = transaction.TimeStamp,
                    TransactionId = transaction.Id,
                    EntityId = transactionCommand.EntityId,
                    EntityVersionNumber = transactionCommand.EntityVersionNumber,
                    Scope = insertLease.Scope,
                    Label = insertLease.Label,
                    Value = insertLease.Value,
                    DataType = insertLease.GetType().Name,
                    Data = envelopeService.Serialize(insertLease)
                })
                .ToArray()
        );
    }

    public static DocumentQuery<LeaseDocument> GetQuery
    (
        ILeaseQuery leaseQuery
    )
    {
        return new DocumentQuery<LeaseDocument>
        (
            leaseQuery.GetFilter(FilterBuilder),
            leaseQuery.GetSort(SortBuilder),
            leaseQuery.Skip,
            leaseQuery.Take,
            leaseQuery.Options
        );
    }

    public static DeleteDocumentsCommand GetDeleteCommand
    (
        ITransactionCommand transactionCommand, IDeleteLeasesCommand deleteLeasesCommand
    )
    {
        var deleteLeasesQuery =
            new DeleteLeasesQuery(transactionCommand.EntityId, deleteLeasesCommand.GetLeases().ToArray());

        return new DeleteDocumentsCommand
        (
            TableName,
            deleteLeasesQuery.GetFilter(FilterBuilder)
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
            [nameof(Scope)] = Scope,
            [nameof(Label)] = Label,
            [nameof(Value)] = Value,
        };
    }
}

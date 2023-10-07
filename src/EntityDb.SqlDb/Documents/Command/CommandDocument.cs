using EntityDb.Abstractions.Queries;
using EntityDb.Abstractions.Transactions;
using EntityDb.Abstractions.ValueObjects;
using EntityDb.Common.Envelopes;
using EntityDb.Common.Queries;
using EntityDb.SqlDb.Commands;
using EntityDb.SqlDb.Queries;
using EntityDb.SqlDb.Queries.FilterBuilders;
using EntityDb.SqlDb.Queries.SortBuilders;
using EntityDb.SqlDb.Sessions;

namespace EntityDb.SqlDb.Documents.Command;

internal sealed record CommandDocument : DocumentBase, IEntityDocument<CommandDocument>
{
    public static string TableName => "Commands";

    private static readonly CommandFilterBuilder FilterBuilder = new();

    private static readonly CommandSortBuilder SortBuilder = new();

    private static readonly IDocumentReader<CommandDocument> EntityVersionNumberDocumentReader = new CommandEntityVersionNumberDocumentReader();

    public static IDocumentReader<CommandDocument> DocumentReader { get; } = new CommandDocumentReader();

    public static IDocumentReader<CommandDocument> TransactionIdDocumentReader { get; } = new CommandTransactionIdDocumentReader();

    public static IDocumentReader<CommandDocument> DataDocumentReader { get; } = new CommandDataDocumentReader();

    public static IDocumentReader<CommandDocument> EntityIdDocumentReader { get; } = new CommandEntityIdDocumentReader();

    public Id EntityId { get; init; }
    public VersionNumber EntityVersionNumber { get; init; }

    public static InsertDocumentsCommand<CommandDocument> GetInsertCommand
    (
        IEnvelopeService<string> envelopeService,
        ITransaction transaction,
        ITransactionCommand transactionCommand
    )
    {
        return new InsertDocumentsCommand<CommandDocument>
        (
            TableName,
            new[]
            {
                new CommandDocument
                {
                    TransactionTimeStamp = transaction.TimeStamp,
                    TransactionId = transaction.Id,
                    EntityId = transactionCommand.EntityId,
                    EntityVersionNumber = transactionCommand.EntityVersionNumber,
                    DataType = transactionCommand.Data.GetType().Name,
                    Data = envelopeService.Serialize(transactionCommand.Data)
                }
            }
        );
    }

    public static DocumentQuery<CommandDocument> GetQuery
    (
        ICommandQuery commandQuery
    )
    {
        return new DocumentQuery<CommandDocument>
        (
            commandQuery.GetFilter(FilterBuilder),
            commandQuery.GetSort(SortBuilder),
            commandQuery.Skip,
            commandQuery.Take,
            commandQuery.Options
        );
    }

    public static async Task<VersionNumber> GetLastEntityVersionNumber<TOptions>
    (
        ISqlDbSession<TOptions> sqlDbSession,
        Id entityId,
        CancellationToken cancellationToken
    )
        where TOptions : class
    {
        var commandQuery = new GetLastEntityCommandQuery(entityId);

        var documentQuery = GetQuery(commandQuery);

        var document = await documentQuery
            .Execute(sqlDbSession, EntityVersionNumberDocumentReader, cancellationToken)
            .SingleOrDefaultAsync(cancellationToken);

        return document?.EntityVersionNumber ?? default;
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
        };
    }
}

using EntityDb.Abstractions.Queries;
using EntityDb.Abstractions.Transactions;
using EntityDb.Abstractions.ValueObjects;
using EntityDb.Common.Envelopes;
using EntityDb.Common.Queries;
using EntityDb.MongoDb.Commands;
using EntityDb.MongoDb.Extensions;
using EntityDb.MongoDb.Queries;
using EntityDb.MongoDb.Queries.FilterBuilders;
using EntityDb.MongoDb.Queries.SortBuilders;
using EntityDb.MongoDb.Sessions;
using MongoDB.Bson;

namespace EntityDb.MongoDb.Documents;

internal sealed record CommandDocument : DocumentBase, IEntityDocument
{
    public const string CollectionName = "Commands";

    private static readonly CommandFilterBuilder FilterBuilder = new();

    private static readonly CommandSortBuilder SortBuilder = new();

    public Id EntityId { get; init; }
    public VersionNumber EntityVersionNumber { get; init; }

    public static InsertDocumentsCommand<CommandDocument> GetInsertCommand
    (
        IEnvelopeService<BsonDocument> envelopeService,
        ITransaction transaction,
        ITransactionCommand transactionCommand
    )
    {
        var documents = new[]
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
        };

        return new InsertDocumentsCommand<CommandDocument>
        (
            CollectionName,
            documents
        );
    }

    public static DocumentQuery<CommandDocument> GetQuery
    (
        ICommandQuery commandQuery
    )
    {
        return new DocumentQuery<CommandDocument>
        (
            CollectionName,
            commandQuery.GetFilter(FilterBuilder),
            commandQuery.GetSort(SortBuilder),
            commandQuery.Skip,
            commandQuery.Take,
            commandQuery.Options as MongoDbQueryOptions
        );
    }

    public static async Task<VersionNumber> GetLastEntityVersionNumber
    (
        IMongoSession mongoSession,
        Id entityId,
        CancellationToken cancellationToken
    )
    {
        var commandQuery = new GetLastEntityCommandQuery(entityId);

        var documentQuery = GetQuery(commandQuery);

        var document = await documentQuery
            .Execute(mongoSession, DocumentQueryExtensions.EntityVersionNumberProjection, cancellationToken)
            .SingleOrDefaultAsync(cancellationToken);

        return document?.EntityVersionNumber ?? default;
    }
}

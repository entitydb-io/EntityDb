using EntityDb.Abstractions.Disposables;
using EntityDb.SqlDb.Documents;
using EntityDb.SqlDb.Queries.Definitions.Filter;
using EntityDb.SqlDb.Queries.Definitions.Sort;
using System.Data.Common;

namespace EntityDb.SqlDb.Sessions;

internal interface ISqlDbSession<in TOptions> : IDisposableResource
    where TOptions : class
{
    DbConnection DbConnection { get; }

    Task Insert<TDocument>
    (

        string tableName,
        TDocument[] documents,
        CancellationToken cancellationToken
    ) where TDocument : ITransactionDocument;

    IAsyncEnumerable<TDocument> Find<TDocument>
    (
        IDocumentReader<TDocument> documentReader,
        IFilterDefinition filterDefinition,
        ISortDefinition? sortDefinition,
        int? skip,
        int? limit,
        TOptions? options,
        CancellationToken cancellationToken
    )
        where TDocument : ITransactionDocument;

    Task Delete
    (
        string tableName,
        IFilterDefinition filterDefinition,
        CancellationToken cancellationToken
    );

    Task StartTransaction(CancellationToken cancellationToken = default);
    Task CommitTransaction(CancellationToken cancellationToken = default);
    Task AbortTransaction(CancellationToken cancellationToken = default);

    ISqlDbSession<TOptions> WithTransactionSessionOptions(SqlDbTransactionSessionOptions transactionSessionOptions);
}

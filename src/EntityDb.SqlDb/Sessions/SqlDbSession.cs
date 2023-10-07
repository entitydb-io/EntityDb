using EntityDb.Common.Disposables;
using EntityDb.Common.Exceptions;
using EntityDb.SqlDb.Converters;
using EntityDb.SqlDb.Documents;
using EntityDb.SqlDb.Queries.Definitions.Filter;
using EntityDb.SqlDb.Queries.Definitions.Sort;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace EntityDb.SqlDb.Sessions;

internal class SqlDbSession<TOptions> : DisposableResourceBaseClass,
    ISqlDbSession<TOptions>
        where TOptions : class
{
    private readonly ILogger<SqlDbSession<TOptions>> _logger;
    private readonly ISqlConverter<TOptions> _sqlConverter;
    private readonly SqlDbTransactionSessionOptions _options;

    private DbTransaction? _dbTransaction;

    public DbConnection DbConnection { get; }

    public SqlDbSession
    (
        ILogger<SqlDbSession<TOptions>> logger,
        ISqlConverter<TOptions> sqlConverter,
        SqlDbTransactionSessionOptions options,
        DbConnection dbConnection
    )
    {
        _logger = logger;
        _sqlConverter = sqlConverter;
        _options = options;

        DbConnection = dbConnection;
    }

    public async Task Insert<TDocument>
    (
        string tableName,
        TDocument[] documents,
        CancellationToken cancellationToken
    )
        where TDocument : ITransactionDocument
    {
        AssertNotReadOnly();

        var dbCommand = _sqlConverter.ConvertInsert(tableName, documents);

        dbCommand.Connection = DbConnection;
        dbCommand.Transaction = _dbTransaction;

        _logger
            .LogInformation
            (
                "Started Running {SqlType} Insert on `{Database}.{TableName}`\n\nCommand: {Command}\n\nDocuments Inserted: {DocumentsInserted}",
                _sqlConverter.SqlType,
                DbConnection.Database,
                tableName,
                dbCommand.CommandText,
                documents.Length
            );

        await dbCommand.PrepareAsync(cancellationToken);

        var insertCount = await dbCommand.ExecuteNonQueryAsync(cancellationToken);

        _logger
            .LogInformation
            (
                "Finished Running {SqlType} Insert on `{Database}.{TableName}`\n\nCommand: {Command}\n\nDocuments Inserted: {DocumentsInserted}",
                _sqlConverter.SqlType,
                DbConnection.Database,
                tableName,
                dbCommand.CommandText,
                insertCount
            );
    }

    public async IAsyncEnumerable<TDocument> Find<TDocument>
    (
        IDocumentReader<TDocument> documentReader,
        IFilterDefinition filterDefinition,
        ISortDefinition? sortDefinition,
        int? skip,
        int? limit,
        TOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
        where TDocument : ITransactionDocument
    {
        var tableName = TDocument.TableName;

        var dbQuery = _sqlConverter.ConvertQuery(tableName, documentReader, filterDefinition, sortDefinition, skip, limit, options);

        dbQuery.Connection = DbConnection;
        dbQuery.Transaction = _dbTransaction;

        _logger
            .LogInformation
            (
                "Started Enumerating {SqlType} Query on `{Database}.{TableName}`\n\nQuery: {Query}",
                _sqlConverter.SqlType,
                DbConnection.Database,
                tableName,
                dbQuery.CommandText
            );

        ulong documentCount = 0;

        await dbQuery.PrepareAsync(cancellationToken);

        await using var reader = await dbQuery.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            documentCount += 1;

            yield return await documentReader.Read(reader, cancellationToken);
        }

        _logger
            .LogInformation
            (
                "Finished Enumerating {SqlType} Query on `{Database}.{CollectionName}`\n\nQuery: {Query}\n\nDocuments Returned: {DocumentsReturned}",
                _sqlConverter.SqlType,
                DbConnection.Database,
                tableName,
                dbQuery.CommandText,
                documentCount
            );
    }

    public async Task Delete
    (
        string tableName,
        IFilterDefinition filterDefinition,
        CancellationToken cancellationToken
    )
    {
        AssertNotReadOnly();

        var dbCommand = _sqlConverter.ConvertDelete(tableName, filterDefinition);

        dbCommand.Connection = DbConnection;
        dbCommand.Transaction = _dbTransaction;

        _logger
            .LogInformation
            (
                "Started Running {SqlType} Delete on `{Database}.{TableName}`\n\nCommand: {Command}",
                _sqlConverter.SqlType,
                DbConnection.Database,
                tableName,
                dbCommand.CommandText
            );

        await dbCommand.PrepareAsync(cancellationToken);

        var deletedCount = await dbCommand.ExecuteNonQueryAsync(cancellationToken);

        _logger
            .LogInformation(
                "Finished Running {SqlType} Delete on `{Database}.{TableName}`\n\nCommand: {Command}\n\nRows Deleted: {DocumentsDeleted}",
                _sqlConverter.SqlType,
                DbConnection.Database,
                tableName,
                dbCommand.CommandText,
                deletedCount
            );
    }

    public override async ValueTask DisposeAsync()
    {
        if (_dbTransaction != null)
        {
            await _dbTransaction.DisposeAsync();
        }

        await DbConnection.CloseAsync();
        await DbConnection.DisposeAsync();
    }

    private void AssertNotReadOnly()
    {
        if (_options.ReadOnly)
        {
            throw new CannotWriteInReadOnlyModeException();
        }
    }

    public async Task StartTransaction(CancellationToken cancellationToken)
    {
        _dbTransaction = await DbConnection.BeginTransactionAsync(_options.IsolationLevel, cancellationToken);
    }

    public async Task CommitTransaction(CancellationToken cancellationToken)
    {
        await _dbTransaction!.CommitAsync(cancellationToken);
    }

    public async Task AbortTransaction(CancellationToken cancellationToken)
    {
        await _dbTransaction!.RollbackAsync(cancellationToken);
    }

    public static ISqlDbSession<TOptions> Create
    (
        IServiceProvider serviceProvider,
        DbConnection dbConnection,
        SqlDbTransactionSessionOptions options
    )
    {
        return ActivatorUtilities.CreateInstance<SqlDbSession<TOptions>>
        (
            serviceProvider,
            dbConnection,
            options
        );
    }

    public ISqlDbSession<TOptions> WithTransactionSessionOptions(SqlDbTransactionSessionOptions transactionSessionOptions)
    {
        return new SqlDbSession<TOptions>(_logger, _sqlConverter, transactionSessionOptions, DbConnection);
    }
}

using EntityDb.SqlDb.Documents;
using EntityDb.SqlDb.Queries.Definitions.Filter;
using EntityDb.SqlDb.Queries.Definitions.Sort;
using System.Data.Common;

namespace EntityDb.SqlDb.Converters;

internal interface ISqlConverter<in TOptions>
{
    string SqlType { get; }

    DbCommand ConvertInsert<TDocument>(string tableName, TDocument[] documents) where TDocument : ITransactionDocument;

    DbCommand ConvertQuery
    (
        string tableName,
        IDocumentReader documentReader,
        IFilterDefinition filterDefinition,
        ISortDefinition? sortDefinition,
        int? skip,
        int? limit,
        TOptions? options
    );

    DbCommand ConvertDelete
    (
        string tableName,
        IFilterDefinition filterDefinition
    );
}

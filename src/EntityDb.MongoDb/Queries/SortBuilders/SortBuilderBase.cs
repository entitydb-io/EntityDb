using EntityDb.Abstractions.Queries.SortBuilders;
using EntityDb.MongoDb.Documents;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EntityDb.MongoDb.Queries.SortBuilders;

internal abstract class SortBuilderBase : BuilderBase, ISortBuilder<SortDefinition<BsonDocument>>
{
    private static readonly SortDefinitionBuilder<BsonDocument> SortBuilder = Builders<BsonDocument>.Sort;

    public SortDefinition<BsonDocument> SourceTimeStamp(bool ascending)
    {
        return Sort(ascending, nameof(DocumentBase.TransactionTimeStamp));

    }

    public SortDefinition<BsonDocument> SourceId(bool ascending)
    {
        return Sort(ascending, nameof(DocumentBase.TransactionId));
    }

    public SortDefinition<BsonDocument> Combine(params SortDefinition<BsonDocument>[] sorts)
    {
        return SortBuilder.Combine(sorts);
    }

    protected static SortDefinition<BsonDocument> Sort(bool ascending, string fieldName)
    {
        return ascending
            ? SortBuilder.Ascending(fieldName)
            : SortBuilder.Descending(fieldName);
    }

    protected static SortDefinition<BsonDocument> SortDataType(bool ascending)
    {
        return Sort(ascending, DataTypeNameFieldName);
    }
}

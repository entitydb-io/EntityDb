using EntityDb.Common.Envelopes;
using EntityDb.MongoDb.Documents;
using MongoDB.Bson;

namespace EntityDb.MongoDb.Queries;

internal abstract class BuilderBase
{
    protected const string DataTypeNameFieldName =
        $"{nameof(DocumentBase.Data)}.{nameof(Envelope<BsonDocument>.Headers)}.{EnvelopeHelper.Type}";
}

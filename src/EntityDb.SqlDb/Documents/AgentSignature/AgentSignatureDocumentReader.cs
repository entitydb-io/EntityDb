using EntityDb.Abstractions.ValueObjects;
using System.Data.Common;

namespace EntityDb.SqlDb.Documents.AgentSignature;

internal class AgentSignatureDocumentReader : AgentSignatureDocumentReaderBase, IDocumentReader<AgentSignatureDocument>
{
    private static readonly string[] PropertyNames =
    {
        nameof(AgentSignatureDocument.TransactionId),
        nameof(AgentSignatureDocument.TransactionTimeStamp),
        nameof(AgentSignatureDocument.EntityIds),
        nameof(AgentSignatureDocument.DataType),
        nameof(AgentSignatureDocument.Data),
    };

    public AgentSignatureDocumentReader() : base(PropertyNames)
    {
    }

    public async Task<AgentSignatureDocument> Read(DbDataReader dbDataReader, CancellationToken cancellationToken)
    {
        return new AgentSignatureDocument
        {
            TransactionId = new Id(await dbDataReader.GetFieldValueAsync<Guid>(TransactionIdOrdinal, cancellationToken)),
            TransactionTimeStamp = new TimeStamp(await dbDataReader.GetFieldValueAsync<DateTime>(TransactionTimeStampOrdinal, cancellationToken)),
            EntityIds = (await dbDataReader.GetFieldValueAsync<Guid[]>(EntityIdsOrdinal, cancellationToken))
                .Select(guid => new Id(guid))
                .ToArray(),
            DataType = await dbDataReader.GetFieldValueAsync<string>(DataTypeOrdinal, cancellationToken),
            Data = await dbDataReader.GetFieldValueAsync<string>(DataOrdinal, cancellationToken),
        };
    }
}

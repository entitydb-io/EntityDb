using EntityDb.Abstractions.ValueObjects;
using System.Data.Common;

namespace EntityDb.SqlDb.Documents.AgentSignature;

internal class AgentSignatureEntityIdsDocumentReader : AgentSignatureDocumentReaderBase, IDocumentReader<AgentSignatureDocument>
{
    private static readonly string[] PropertyNames =
    {
        nameof(AgentSignatureDocument.EntityIds),
    };

    public AgentSignatureEntityIdsDocumentReader() : base(PropertyNames)
    {
    }

    public async Task<AgentSignatureDocument> Read(DbDataReader dbDataReader, CancellationToken cancellationToken)
    {
        return new AgentSignatureDocument
        {
            EntityIds = (await dbDataReader.GetFieldValueAsync<Guid[]>(EntityIdsOrdinal, cancellationToken))
                .Select(guid => new Id(guid))
                .ToArray()
        };
    }
}

using System.Data.Common;

namespace EntityDb.SqlDb.Documents.AgentSignature;

internal class AgentSignatureDataDocumentReader : AgentSignatureDocumentReaderBase, IDocumentReader<AgentSignatureDocument>
{
    private static readonly string[] PropertyNames =
    {
        nameof(AgentSignatureDocument.Data),
    };

    public AgentSignatureDataDocumentReader() : base(PropertyNames)
    {
    }

    public async Task<AgentSignatureDocument> Read(DbDataReader dbDataReader, CancellationToken cancellationToken)
    {
        return new AgentSignatureDocument
        {
            Data = await dbDataReader.GetFieldValueAsync<string>(DataOrdinal, cancellationToken)
        };
    }
}

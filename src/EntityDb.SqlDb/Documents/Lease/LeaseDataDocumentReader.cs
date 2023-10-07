using System.Data.Common;

namespace EntityDb.SqlDb.Documents.Lease;

internal class LeaseDataDocumentReader : LeaseDocumentReaderBase, IDocumentReader<LeaseDocument>
{
    private static readonly string[] PropertyNames =
    {
        nameof(LeaseDocument.Data),
    };

    public LeaseDataDocumentReader() : base(PropertyNames)
    {
    }

    public async Task<LeaseDocument> Read(DbDataReader dbDataReader, CancellationToken cancellationToken)
    {
        return new LeaseDocument
        {
            Data = await dbDataReader.GetFieldValueAsync<string>(DataOrdinal, cancellationToken)
        };
    }
}

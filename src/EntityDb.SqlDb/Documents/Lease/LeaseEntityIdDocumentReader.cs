using EntityDb.Abstractions.ValueObjects;
using System.Data.Common;

namespace EntityDb.SqlDb.Documents.Lease;

internal class LeaseEntityIdDocumentReader : LeaseDocumentReaderBase, IDocumentReader<LeaseDocument>
{
    private static readonly string[] PropertyName =
    {
        nameof(LeaseDocument.EntityId),
    };

    public LeaseEntityIdDocumentReader() : base(PropertyName)
    {
    }

    public async Task<LeaseDocument> Read(DbDataReader dbDataReader, CancellationToken cancellationToken)
    {
        return new LeaseDocument
        {
            EntityId = new Id(await dbDataReader.GetFieldValueAsync<Guid>(EntityIdOrdinal, cancellationToken))
        };
    }
}

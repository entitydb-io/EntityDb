using EntityDb.Abstractions.ValueObjects;
using System.Data.Common;

namespace EntityDb.SqlDb.Documents.Lease;

internal class LeaseDocumentReader : LeaseDocumentReaderBase, IDocumentReader<LeaseDocument>
{
    private static readonly string[] PropertyNames =
    {
        nameof(LeaseDocument.TransactionId),
        nameof(LeaseDocument.TransactionTimeStamp),
        nameof(LeaseDocument.EntityId),
        nameof(LeaseDocument.EntityVersionNumber),
        nameof(LeaseDocument.DataType),
        nameof(LeaseDocument.Data),
        nameof(LeaseDocument.Scope),
        nameof(LeaseDocument.Label),
        nameof(LeaseDocument.Value),
    };

    public LeaseDocumentReader() : base(PropertyNames)
    {
    }

    public async Task<LeaseDocument> Read(DbDataReader dbDataReader, CancellationToken cancellationToken)
    {
        return new LeaseDocument
        {
            TransactionId = new Id(await dbDataReader.GetFieldValueAsync<Guid>(TransactionIdOrdinal, cancellationToken)),
            TransactionTimeStamp = new TimeStamp(await dbDataReader.GetFieldValueAsync<DateTime>(TransactionTimeStampOrdinal, cancellationToken)),
            EntityId = new Id(await dbDataReader.GetFieldValueAsync<Guid>(EntityIdOrdinal, cancellationToken)),
            EntityVersionNumber = new VersionNumber(Convert.ToUInt64(await dbDataReader.GetFieldValueAsync<long>(EntityVersionNumberOrdinal, cancellationToken))),
            DataType = await dbDataReader.GetFieldValueAsync<string>(DataTypeOrdinal, cancellationToken),
            Data = await dbDataReader.GetFieldValueAsync<string>(DataOrdinal, cancellationToken),
            Scope = await dbDataReader.GetFieldValueAsync<string>(ScopeOrdinal, cancellationToken),
            Label = await dbDataReader.GetFieldValueAsync<string>(LabelOrdinal, cancellationToken),
            Value = await dbDataReader.GetFieldValueAsync<string>(ValueOrdinal, cancellationToken),
        };
    }
}

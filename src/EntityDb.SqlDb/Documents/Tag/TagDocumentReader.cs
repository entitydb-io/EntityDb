using EntityDb.Abstractions.ValueObjects;
using System.Data.Common;

namespace EntityDb.SqlDb.Documents.Tag;

internal class TagDocumentReader : TagDocumentReaderBase, IDocumentReader<TagDocument>
{
    private static readonly string[] PropertyNames =
    {
        nameof(TagDocument.TransactionId),
        nameof(TagDocument.TransactionTimeStamp),
        nameof(TagDocument.EntityId),
        nameof(TagDocument.EntityVersionNumber),
        nameof(TagDocument.DataType),
        nameof(TagDocument.Data),
        nameof(TagDocument.Label),
        nameof(TagDocument.Value),
    };

    public TagDocumentReader() : base(PropertyNames)
    {
    }

    public async Task<TagDocument> Read(DbDataReader dbDataReader, CancellationToken cancellationToken)
    {
        return new TagDocument
        {
            TransactionId = new Id(await dbDataReader.GetFieldValueAsync<Guid>(TransactionIdOrdinal, cancellationToken)),
            TransactionTimeStamp = new TimeStamp(await dbDataReader.GetFieldValueAsync<DateTime>(TransactionTimeStampOrdinal, cancellationToken)),
            EntityId = new Id(await dbDataReader.GetFieldValueAsync<Guid>(EntityIdOrdinal, cancellationToken)),
            EntityVersionNumber = new VersionNumber(Convert.ToUInt64(await dbDataReader.GetFieldValueAsync<long>(EntityVersionNumberOrdinal, cancellationToken))),
            DataType = await dbDataReader.GetFieldValueAsync<string>(DataTypeOrdinal, cancellationToken),
            Data = await dbDataReader.GetFieldValueAsync<string>(DataOrdinal, cancellationToken),
            Label = await dbDataReader.GetFieldValueAsync<string>(LabelOrdinal, cancellationToken),
            Value = await dbDataReader.GetFieldValueAsync<string>(ValueOrdinal, cancellationToken),
        };
    }
}

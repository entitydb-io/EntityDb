using EntityDb.Abstractions.ValueObjects;
using System.Data.Common;

namespace EntityDb.SqlDb.Documents.Command;

internal class CommandDocumentReader : CommandDocumentReaderBase, IDocumentReader<CommandDocument>
{
    private static readonly string[] PropertyNames =
    {
        nameof(CommandDocument.TransactionId),
        nameof(CommandDocument.TransactionTimeStamp),
        nameof(CommandDocument.EntityId),
        nameof(CommandDocument.EntityVersionNumber),
        nameof(CommandDocument.DataType),
        nameof(CommandDocument.Data),
    };

    public CommandDocumentReader() : base(PropertyNames)
    {
    }

    public async Task<CommandDocument> Read(DbDataReader dbDataReader, CancellationToken cancellationToken)
    {
        return new CommandDocument
        {
            TransactionId = new Id(await dbDataReader.GetFieldValueAsync<Guid>(TransactionIdOrdinal, cancellationToken)),
            TransactionTimeStamp = new TimeStamp(await dbDataReader.GetFieldValueAsync<DateTime>(TransactionTimeStampOrdinal, cancellationToken)),
            EntityId = new Id(await dbDataReader.GetFieldValueAsync<Guid>(EntityIdOrdinal, cancellationToken)),
            EntityVersionNumber = new VersionNumber(Convert.ToUInt64(await dbDataReader.GetFieldValueAsync<long>(EntityVersionNumberOrdinal, cancellationToken))),
            DataType = await dbDataReader.GetFieldValueAsync<string>(DataTypeOrdinal, cancellationToken),
            Data = await dbDataReader.GetFieldValueAsync<string>(DataOrdinal, cancellationToken),
        };
    }
}

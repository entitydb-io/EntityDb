using System.Data.Common;

namespace EntityDb.SqlDb.Documents.Command;

internal class CommandDataDocumentReader : CommandDocumentReaderBase, IDocumentReader<CommandDocument>
{
    private static readonly string[] PropertyNames =
    {
        nameof(CommandDocument.Data),
    };

    public CommandDataDocumentReader() : base(PropertyNames)
    {
    }

    public async Task<CommandDocument> Read(DbDataReader dbDataReader, CancellationToken cancellationToken)
    {
        return new CommandDocument
        {
            Data = await dbDataReader.GetFieldValueAsync<string>(DataOrdinal, cancellationToken)
        };
    }
}

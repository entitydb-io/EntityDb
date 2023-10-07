using EntityDb.Abstractions.ValueObjects;
using System.Data.Common;

namespace EntityDb.SqlDb.Documents.Command;

internal class CommandEntityIdDocumentReader : CommandDocumentReaderBase, IDocumentReader<CommandDocument>
{
    private static readonly string[] PropertyNames =
    {
        nameof(CommandDocument.EntityId),
    };

    public CommandEntityIdDocumentReader() : base(PropertyNames)
    {
    }

    public async Task<CommandDocument> Read(DbDataReader dbDataReader, CancellationToken cancellationToken)
    {
        return new CommandDocument
        {
            EntityId = new Id(await dbDataReader.GetFieldValueAsync<Guid>(EntityIdOrdinal, cancellationToken))
        };
    }
}

using System.Data.Common;

namespace EntityDb.SqlDb.Documents.Tag;

internal class TagDataDocumentReader : TagDocumentReaderBase, IDocumentReader<TagDocument>
{
    private static readonly string[] PropertyNames =
    {
        nameof(TagDocument.Data),
    };

    public TagDataDocumentReader() : base(PropertyNames)
    {
    }

    public async Task<TagDocument> Read(DbDataReader dbDataReader, CancellationToken cancellationToken)
    {
        return new TagDocument
        {
            Data = await dbDataReader.GetFieldValueAsync<string>(DataOrdinal, cancellationToken)
        };
    }
}

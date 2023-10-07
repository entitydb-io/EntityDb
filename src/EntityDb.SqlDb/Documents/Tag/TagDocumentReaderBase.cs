namespace EntityDb.SqlDb.Documents.Tag;

internal abstract class TagDocumentReaderBase
{
    private readonly string[] _propertyNames;

    protected readonly int TransactionIdOrdinal;
    protected readonly int TransactionTimeStampOrdinal;
    protected readonly int EntityIdOrdinal;
    protected readonly int EntityVersionNumberOrdinal;
    protected readonly int DataTypeOrdinal;
    protected readonly int DataOrdinal;
    protected readonly int LabelOrdinal;
    protected readonly int ValueOrdinal;

    public string[] GetPropertyNames() => _propertyNames;

    protected TagDocumentReaderBase(string[] propertyNames)
    {
        _propertyNames = propertyNames;

        TransactionIdOrdinal = Array.IndexOf(_propertyNames, nameof(TagDocument.TransactionId));
        TransactionTimeStampOrdinal = Array.IndexOf(_propertyNames, nameof(TagDocument.TransactionTimeStamp));
        EntityIdOrdinal = Array.IndexOf(_propertyNames, nameof(TagDocument.EntityId));
        EntityVersionNumberOrdinal = Array.IndexOf(_propertyNames, nameof(TagDocument.EntityVersionNumber));
        DataTypeOrdinal = Array.IndexOf(_propertyNames, nameof(TagDocument.DataType));
        DataOrdinal = Array.IndexOf(_propertyNames, nameof(TagDocument.Data));
        LabelOrdinal = Array.IndexOf(_propertyNames, nameof(TagDocument.Label));
        ValueOrdinal = Array.IndexOf(_propertyNames, nameof(TagDocument.Value));
    }
}

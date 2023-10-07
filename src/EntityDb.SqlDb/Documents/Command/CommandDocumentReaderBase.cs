namespace EntityDb.SqlDb.Documents.Command;

internal abstract class CommandDocumentReaderBase
{
    private readonly string[] _propertyNames;

    protected readonly int TransactionIdOrdinal;
    protected readonly int TransactionTimeStampOrdinal;
    protected readonly int EntityIdOrdinal;
    protected readonly int EntityVersionNumberOrdinal;
    protected readonly int DataTypeOrdinal;
    protected readonly int DataOrdinal;

    public string[] GetPropertyNames() => _propertyNames;

    protected CommandDocumentReaderBase(string[] propertyNames)
    {
        _propertyNames = propertyNames;

        TransactionIdOrdinal = Array.IndexOf(_propertyNames, nameof(CommandDocument.TransactionId));
        TransactionTimeStampOrdinal = Array.IndexOf(_propertyNames, nameof(CommandDocument.TransactionTimeStamp));
        EntityIdOrdinal = Array.IndexOf(_propertyNames, nameof(CommandDocument.EntityId));
        EntityVersionNumberOrdinal = Array.IndexOf(_propertyNames, nameof(CommandDocument.EntityVersionNumber));
        DataTypeOrdinal = Array.IndexOf(_propertyNames, nameof(CommandDocument.DataType));
        DataOrdinal = Array.IndexOf(_propertyNames, nameof(CommandDocument.Data));
    }
}

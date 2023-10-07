namespace EntityDb.SqlDb.Documents.Lease;

internal abstract class LeaseDocumentReaderBase
{
    private readonly string[] _propertyNames;

    protected readonly int TransactionIdOrdinal;
    protected readonly int TransactionTimeStampOrdinal;
    protected readonly int EntityIdOrdinal;
    protected readonly int EntityVersionNumberOrdinal;
    protected readonly int DataTypeOrdinal;
    protected readonly int DataOrdinal;
    protected readonly int ScopeOrdinal;
    protected readonly int LabelOrdinal;
    protected readonly int ValueOrdinal;

    public string[] GetPropertyNames() => _propertyNames;

    protected LeaseDocumentReaderBase(string[] propertyNames)
    {
        _propertyNames = propertyNames;

        TransactionIdOrdinal = Array.IndexOf(_propertyNames, nameof(LeaseDocument.TransactionId));
        TransactionTimeStampOrdinal = Array.IndexOf(_propertyNames, nameof(LeaseDocument.TransactionTimeStamp));
        EntityIdOrdinal = Array.IndexOf(_propertyNames, nameof(LeaseDocument.EntityId));
        EntityVersionNumberOrdinal = Array.IndexOf(_propertyNames, nameof(LeaseDocument.EntityVersionNumber));
        DataTypeOrdinal = Array.IndexOf(_propertyNames, nameof(LeaseDocument.DataType));
        DataOrdinal = Array.IndexOf(_propertyNames, nameof(LeaseDocument.Data));
        ScopeOrdinal = Array.IndexOf(_propertyNames, nameof(LeaseDocument.Scope));
        LabelOrdinal = Array.IndexOf(_propertyNames, nameof(LeaseDocument.Label));
        ValueOrdinal = Array.IndexOf(_propertyNames, nameof(LeaseDocument.Value));
    }
}

namespace EntityDb.SqlDb.Documents.AgentSignature;

internal abstract class AgentSignatureDocumentReaderBase
{
    private readonly string[] _propertyNames;

    protected readonly int TransactionIdOrdinal;
    protected readonly int TransactionTimeStampOrdinal;
    protected readonly int EntityIdsOrdinal;
    protected readonly int DataTypeOrdinal;
    protected readonly int DataOrdinal;

    public string[] GetPropertyNames() => _propertyNames;

    protected AgentSignatureDocumentReaderBase(string[] propertyNames)
    {
        _propertyNames = propertyNames;

        TransactionIdOrdinal = Array.IndexOf(_propertyNames, nameof(AgentSignatureDocument.TransactionId));
        TransactionTimeStampOrdinal = Array.IndexOf(_propertyNames, nameof(AgentSignatureDocument.TransactionTimeStamp));
        EntityIdsOrdinal = Array.IndexOf(_propertyNames, nameof(AgentSignatureDocument.EntityIds));
        DataTypeOrdinal = Array.IndexOf(_propertyNames, nameof(AgentSignatureDocument.DataType));
        DataOrdinal = Array.IndexOf(_propertyNames, nameof(AgentSignatureDocument.Data));
    }
}

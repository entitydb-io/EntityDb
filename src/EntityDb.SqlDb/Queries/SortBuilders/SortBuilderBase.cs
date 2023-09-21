using EntityDb.Abstractions.Queries.SortBuilders;
using EntityDb.SqlDb.Documents;
using EntityDb.SqlDb.Queries.Definitions.Sort;

namespace EntityDb.SqlDb.Queries.SortBuilders;

internal abstract class SortBuilderBase : ISortBuilder<ISortDefinition>
{
    public ISortDefinition SourceTimeStamp(bool ascending)
    {
        return Sort(ascending, nameof(ITransactionDocument.TransactionTimeStamp));
    }

    public ISortDefinition SourceId(bool ascending)
    {
        return Sort(ascending, nameof(ITransactionDocument.TransactionId));
    }


    public ISortDefinition Combine(params ISortDefinition[] sorts)
    {
        return new CombineSortDefinition(sorts);
    }

    protected static ISortDefinition SortDataType(bool ascending)
    {
        return Sort(ascending, nameof(ITransactionDocument.DataType));
    }

    protected static ISortDefinition Sort(bool ascending, string fieldName)
    {
        return ascending
            ? new AscSortDefinition(fieldName)
            : new DescSortDefinition(fieldName);
    }
}

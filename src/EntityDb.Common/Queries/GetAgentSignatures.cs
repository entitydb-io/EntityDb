using EntityDb.Abstractions.Queries;
using EntityDb.Abstractions.Queries.FilterBuilders;
using EntityDb.Abstractions.Queries.SortBuilders;
using EntityDb.Abstractions.ValueObjects;

namespace EntityDb.Common.Queries;

internal sealed record GetAgentSignatures(Id[] TransactionIds) : IAgentSignatureQuery
{
    public TFilter GetFilter<TFilter>(IAgentSignatureFilterBuilder<TFilter> builder)
    {
        return builder.SourceIdIn(TransactionIds);
    }

    public TSort? GetSort<TSort>(IAgentSignatureSortBuilder<TSort> builder)
    {
        return builder.SourceTimeStamp(true);
    }

    public int? Skip => null;

    public int? Take => null;

    public object? Options => null;
}

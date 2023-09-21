using EntityDb.Abstractions.Queries;
using EntityDb.Abstractions.Queries.FilterBuilders;
using EntityDb.Abstractions.Queries.SortBuilders;
using EntityDb.Abstractions.ValueObjects;

namespace EntityDb.Common.Tests.Implementations.Queries;

public record TransactionIdQuery(Id TransactionId, object? Options = null) : IAgentSignatureQuery, ICommandQuery, ILeaseQuery,
    ITagQuery
{
    public TFilter GetFilter<TFilter>(IAgentSignatureFilterBuilder<TFilter> builder)
    {
        return builder.SourceIdIn(TransactionId);
    }

    public TSort GetSort<TSort>(IAgentSignatureSortBuilder<TSort> builder)
    {
        return builder.Combine
        (
            builder.SourceId(true)
        );
    }

    public int? Skip => null;

    public int? Take => null;

    public TFilter GetFilter<TFilter>(ICommandFilterBuilder<TFilter> builder)
    {
        return builder.SourceIdIn(TransactionId);
    }

    public TSort GetSort<TSort>(ICommandSortBuilder<TSort> builder)
    {
        return builder.Combine
        (
            builder.SourceId(true),
            builder.EntityId(true),
            builder.EntityVersionNumber(true)
        );
    }

    public TFilter GetFilter<TFilter>(ILeaseFilterBuilder<TFilter> builder)
    {
        return builder.SourceIdIn(TransactionId);
    }

    public TSort GetSort<TSort>(ILeaseSortBuilder<TSort> builder)
    {
        return builder.Combine
        (
            builder.SourceId(true),
            builder.EntityId(true),
            builder.EntityVersionNumber(true)
        );
    }

    public TFilter GetFilter<TFilter>(ITagFilterBuilder<TFilter> builder)
    {
        return builder.SourceIdIn(TransactionId);
    }

    public TSort GetSort<TSort>(ITagSortBuilder<TSort> builder)
    {
        return builder.Combine
        (
            builder.SourceId(true),
            builder.EntityId(true),
            builder.EntityVersionNumber(true)
        );
    }
}
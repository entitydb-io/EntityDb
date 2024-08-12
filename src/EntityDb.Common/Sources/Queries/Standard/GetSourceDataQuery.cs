﻿using EntityDb.Abstractions;
using EntityDb.Abstractions.Sources.Queries;
using EntityDb.Abstractions.Sources.Queries.FilterBuilders;
using EntityDb.Abstractions.Sources.Queries.SortBuilders;

namespace EntityDb.Common.Sources.Queries.Standard;

internal sealed record GetSourceDataQuery(Id SourceId, Id? StateId) : ISourceDataQuery, IMessageDataQuery
{
    public TFilter GetFilter<TFilter>(IMessageDataFilterBuilder<TFilter> builder)
    {
        if (StateId.HasValue)
        {
            return builder.And
            (
                builder.StateIdIn(StateId.Value),
                builder.SourceIdIn(SourceId)
            );
        }
        return builder.SourceIdIn(SourceId);
    }

    public TSort? GetSort<TSort>(IMessageDataSortBuilder<TSort> builder)
    {
        return default;
    }

    public TFilter GetFilter<TFilter>(ISourceDataFilterBuilder<TFilter> builder)
    {
        return builder.SourceIdIn(SourceId);
    }

    public TSort? GetSort<TSort>(ISourceDataSortBuilder<TSort> builder)
    {
        return default;
    }

    public int? Skip => default;

    public int? Take => default;

    public object? Options => default;
}

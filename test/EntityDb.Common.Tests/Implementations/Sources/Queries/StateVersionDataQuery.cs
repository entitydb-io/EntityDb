﻿using EntityDb.Abstractions.Sources.Queries;
using EntityDb.Abstractions.Sources.Queries.FilterBuilders;
using EntityDb.Abstractions.Sources.Queries.SortBuilders;
using EntityDb.Abstractions.States;

namespace EntityDb.Common.Tests.Implementations.Sources.Queries;

public sealed record StateVersionDataQuery(StateVersion Gte, StateVersion Lte, object? Options = null) :
    IMessageDataQuery,
    ILeaseDataQuery, ITagDataQuery
{
    public TFilter GetFilter<TFilter>(ILeaseDataFilterBuilder<TFilter> builder)
    {
        return builder.And
        (
            builder.StateVersionGte(Gte),
            builder.StateVersionLte(Lte)
        );
    }

    public TSort GetSort<TSort>(ILeaseDataSortBuilder<TSort> builder)
    {
        return builder.StateVersion(true);
    }

    public TFilter GetFilter<TFilter>(IMessageDataFilterBuilder<TFilter> builder)
    {
        return builder.And
        (
            builder.StateVersionGte(Gte),
            builder.StateVersionLte(Lte)
        );
    }

    public TSort GetSort<TSort>(IMessageDataSortBuilder<TSort> builder)
    {
        return builder.StateVersion(true);
    }

    public int? Skip => null;

    public int? Take => null;

    public TFilter GetFilter<TFilter>(ITagDataFilterBuilder<TFilter> builder)
    {
        return builder.And
        (
            builder.StateVersionGte(Gte),
            builder.StateVersionLte(Lte)
        );
    }

    public TSort GetSort<TSort>(ITagDataSortBuilder<TSort> builder)
    {
        return builder.StateVersion(true);
    }
}

﻿using EntityDb.Abstractions.Queries;
using EntityDb.Abstractions.Queries.FilterBuilders;
using EntityDb.Abstractions.Queries.SortBuilders;
using EntityDb.Common.Leases;
using EntityDb.Common.Tags;
using EntityDb.TestImplementations.Commands;
using EntityDb.TestImplementations.Leases;
using EntityDb.TestImplementations.AgentSignature;
using EntityDb.TestImplementations.Tags;

namespace EntityDb.TestImplementations.Queries
{
    public record CountQuery(int Gte, int Lte) : IAgentSignatureQuery, ICommandQuery, ILeaseQuery,
        ITagQuery
    {
        public TFilter GetFilter<TFilter>(ICommandFilterBuilder<TFilter> builder)
        {
            return builder.CommandMatches((Count count) => Gte <= count.Number && count.Number <= Lte);
        }

        public TSort GetSort<TSort>(ICommandSortBuilder<TSort> builder)
        {
            return builder.Combine
            (
                builder.EntityId(true),
                builder.EntityVersionNumber(true),
                builder.CommandType(true),
                builder.CommandProperty(true, (Count count) => count.Number)
            );
        }

        public TFilter GetFilter<TFilter>(ILeaseFilterBuilder<TFilter> builder)
        {
            return builder.LeaseMatches((CountLease countLease) =>
                Gte <= countLease.Number && countLease.Number <= Lte);
        }

        public TSort GetSort<TSort>(ILeaseSortBuilder<TSort> builder)
        {
            return builder.Combine
            (
                builder.EntityId(true),
                builder.EntityVersionNumber(true),
                builder.LeaseType(true),
                builder.LeaseProperty(true, (CountLease countLease) => countLease.Number),
                builder.LeaseProperty(true, (Lease lease) => lease.Scope)
            );
        }

        public TFilter GetFilter<TFilter>(IAgentSignatureFilterBuilder<TFilter> builder)
        {
            return builder.AgentSignatureMatches((CounterAgentSignature counter) => Gte <= counter.Number && counter.Number <= Lte);
        }

        public TSort GetSort<TSort>(IAgentSignatureSortBuilder<TSort> builder)
        {
            return builder.Combine
            (
                builder.EntityIds(true),
                builder.AgentSignatureType(true),
                builder.AgentSignatureProperty(true, (CounterAgentSignature counter) => counter.Number)
            );
        }

        public int? Skip => null;

        public int? Take => null;

        public TFilter GetFilter<TFilter>(ITagFilterBuilder<TFilter> builder)
        {
            return builder.TagMatches((CountTag countTag) => Gte <= countTag.Number && countTag.Number <= Lte);
        }

        public TSort GetSort<TSort>(ITagSortBuilder<TSort> builder)
        {
            return builder.Combine
            (
                builder.EntityId(true),
                builder.EntityVersionNumber(true),
                builder.TagType(true),
                builder.TagProperty(true, (CountTag countTag) => countTag.Number),
                builder.TagProperty(true, (Tag tag) => tag.Label)
            );
        }
    }
}

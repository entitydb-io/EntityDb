﻿namespace EntityDb.Abstractions.Sources.Queries.SortBuilders;

/// <summary>
///     Builds a <typeparamref name="TSort" /> for a agentSignature query.
/// </summary>
/// <typeparam name="TSort">The type of sort used by the repository.</typeparam>
public interface ISourceDataSortBuilder<TSort> : IDataSortBuilder<TSort>
{
    /// <summary>
    ///     Returns a <typeparamref name="TSort" /> that orders objects by state ids.
    /// </summary>
    /// <param name="ascending">Pass <c>true</c> for ascending order or <c>false</c> for descending order.</param>
    /// <returns>A <typeparamref name="TSort" /> that orders objects by state ids.</returns>
    TSort StateIds(bool ascending);
}

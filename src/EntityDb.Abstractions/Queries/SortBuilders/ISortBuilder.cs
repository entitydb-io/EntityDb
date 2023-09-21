namespace EntityDb.Abstractions.Queries.SortBuilders;

/// <summary>
///     Builds a sort for an object repository. Possible objects include: agentSignatures, commands, facts, and leases.
/// </summary>
/// <typeparam name="TSort">The type of sort used by the repository.</typeparam>
public interface ISortBuilder<TSort>
{
    /// <ignore />
    [Obsolete("Please use SourceTimeStamp instead. This will be removed in a future version.")]
    TSort TransactionTimeStamp(bool ascending) => SourceTimeStamp(ascending);

    /// <ignore />
    [Obsolete("Please use SourceId instead. This will be removed in a future version.")]
    TSort TransactionId(bool ascending) => SourceId(ascending);

    /// <summary>
    ///     Returns a <typeparamref name="TSort" /> that orders objects by source timestamp.
    /// </summary>
    /// <param name="ascending">Pass <c>true</c> for ascending order or <c>false</c> for descending order.</param>
    /// <returns>A <typeparamref name="TSort" /> that orders objects by source timestamp.</returns>
    TSort SourceTimeStamp(bool ascending);

    /// <summary>
    ///     Returns a <typeparamref name="TSort" /> that orders objects by source id.
    /// </summary>
    /// <param name="ascending">Pass <c>true</c> for ascending order or <c>false</c> for descending order.</param>
    /// <returns>A <typeparamref name="TSort" /> that orders objects by source id.</returns>
    TSort SourceId(bool ascending);

    /// <summary>
    ///     Returns a <typeparamref name="TSort" /> that orders objects ordered by a series of sorts.
    /// </summary>
    /// <param name="sorts">The series of sorts.</param>
    /// <returns>A <typeparamref name="TSort" /> that orders objects ordered by <paramref name="sorts" />.</returns>
    TSort Combine(params TSort[] sorts);
}

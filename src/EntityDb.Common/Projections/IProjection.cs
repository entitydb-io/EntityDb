using EntityDb.Abstractions.Queries;
using EntityDb.Abstractions.Sources;
using EntityDb.Abstractions.ValueObjects;
using EntityDb.Common.Snapshots;

namespace EntityDb.Common.Projections;

/// <summary>
///     Provides basic functionality for the common implementation of projections.
/// </summary>
/// <typeparam name="TProjection"></typeparam>
public interface IProjection<TProjection> : ISnapshot<TProjection>
{
    /// <summary>
    ///     Incorporates the source into the projection.
    /// </summary>
    /// <param name="source">The source of information</param>
    void Mutate(ISource source);

    /// <summary>
    ///     Returns a <see cref="IAgentSignatureQuery" /> that finds transactions that need to be passed to the reducer.
    /// </summary>
    /// <param name="sourceRepository">The source repository, which can be used to locate new information</param>
    /// <param name="projectionPointer">A pointer to the desired projection state</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A <see cref="IQuery" /> that is used to load the rest of the transaction commands for the given projection pointer.</returns>
    IAsyncEnumerable<ISource> EnumerateSources(ISourceRepository sourceRepository, Pointer projectionPointer, CancellationToken cancellationToken);

    /// <summary>
    ///     Maps a source to a projection id, or default if the entity does not map to this projection.
    /// </summary>
    /// <param name="source">The source that could trigger a projection</param>
    /// <returns>The projection id for the entity, or default if none.</returns>
    static abstract IEnumerable<Id> EnumerateProjectionIds(ISource source);
}

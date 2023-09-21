using EntityDb.Abstractions.Projections;
using EntityDb.Abstractions.Sources;
using EntityDb.Common.Extensions;
using EntityDb.Common.Projections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EntityDb.Common.Sources.Processors;

/// <summary>
///     A source processor that creates projection snapshots
/// </summary>
/// <typeparam name="TProjection"></typeparam>
public sealed class ProjectionSnapshotSourceProcessor<TProjection> : ISourceProcessor
    where TProjection : IProjection<TProjection>
{
    private readonly ILogger<ProjectionSnapshotSourceProcessor<TProjection>> _logger;
    private readonly IProjectionRepositoryFactory<TProjection> _projectionRepositoryFactory;
    private readonly string _snapshotSessionOptionsName;
    private readonly string _transactionSessionOptionsName;

    /// <ignore />
    public ProjectionSnapshotSourceProcessor
    (
        ILogger<ProjectionSnapshotSourceProcessor<TProjection>> logger,
        IProjectionRepositoryFactory<TProjection> projectionRepositoryFactory,
        string transactionSessionOptionsName,
        string snapshotSessionOptionsName
    )
    {
        _logger = logger;
        _projectionRepositoryFactory = projectionRepositoryFactory;
        _transactionSessionOptionsName = transactionSessionOptionsName;
        _snapshotSessionOptionsName = snapshotSessionOptionsName;
    }

    /// <inheritdoc />
    public async Task Process(ISource source, CancellationToken cancellationToken)
    {
        await using var projectionRepository = await _projectionRepositoryFactory
            .CreateRepository(_transactionSessionOptionsName, _snapshotSessionOptionsName, cancellationToken);

        if (projectionRepository.SnapshotRepository is null)
        {
            _logger.LogWarning("Snapshots not enabled, no point in processing source.");

            return;
        }

        foreach (var projectionId in TProjection.EnumerateProjectionIds(source))
        {
            var previousProjection = await projectionRepository.SnapshotRepository
                .GetSnapshotOrDefault(projectionId, cancellationToken);

            var nextProjection = previousProjection ?? TProjection.Construct(projectionId);

            nextProjection.Mutate(source);

            var nextProjectionPointer = nextProjection.GetPointer();

            if (nextProjection.ShouldRecordAsLatest(previousProjection))
            {
                await projectionRepository.SnapshotRepository.PutSnapshot(projectionId, nextProjection, cancellationToken);
            }

            if (nextProjection.ShouldRecord())
            {
                await projectionRepository.SnapshotRepository.PutSnapshot(nextProjectionPointer, nextProjection, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    internal static ProjectionSnapshotSourceProcessor<TProjection> Create(IServiceProvider serviceProvider,
        string transactionSessionOptionsName, string snapshotSessionOptionsName)
    {
        return ActivatorUtilities.CreateInstance<ProjectionSnapshotSourceProcessor<TProjection>>(serviceProvider,
            transactionSessionOptionsName,
            snapshotSessionOptionsName);
    }
}

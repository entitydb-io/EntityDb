using EntityDb.Abstractions.Projections;
using EntityDb.Abstractions.Snapshots;
using EntityDb.Abstractions.Sources;
using EntityDb.Abstractions.Transactions;
using EntityDb.Abstractions.ValueObjects;
using EntityDb.Common.Disposables;
using EntityDb.Common.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace EntityDb.Common.Projections;

internal sealed class ProjectionRepository<TProjection> : DisposableResourceBaseClass, IProjectionRepository<TProjection>, ISourceRepository
    where TProjection : IProjection<TProjection>
{
    public ProjectionRepository
    (
        ITransactionRepository transactionRepository,
        ISnapshotRepository<TProjection>? snapshotRepository = null
    )
    {
        TransactionRepository = transactionRepository;
        SnapshotRepository = snapshotRepository;
    }

    public ITransactionRepository TransactionRepository { get; }

    public ISnapshotRepository<TProjection>? SnapshotRepository { get; }

    public async Task<TProjection> GetSnapshot(Pointer projectionPointer, CancellationToken cancellationToken = default)
    {
        var projection = SnapshotRepository is not null
            ? await SnapshotRepository.GetSnapshotOrDefault(projectionPointer, cancellationToken) ??
              TProjection.Construct(projectionPointer.Id)
            : TProjection.Construct(projectionPointer.Id);

        var sources = projection.EnumerateSources(this, projectionPointer, cancellationToken);

        await foreach (var source in sources)
        {
            projection.Mutate(source);
        }

        if (!projectionPointer.IsSatisfiedBy(projection.GetVersionNumber()))
        {
            throw new SnapshotPointerDoesNotExistException();
        }

        return projection;
    }

    public static IProjectionRepository<TProjection> Create(IServiceProvider serviceProvider,
        ITransactionRepository transactionRepository,
        ISnapshotRepository<TProjection>? snapshotRepository = null)
    {
        if (snapshotRepository is null)
        {
            return ActivatorUtilities.CreateInstance<ProjectionRepository<TProjection>>(serviceProvider,
                transactionRepository);
        }

        return ActivatorUtilities.CreateInstance<ProjectionRepository<TProjection>>(serviceProvider,
            transactionRepository, snapshotRepository);
    }
}

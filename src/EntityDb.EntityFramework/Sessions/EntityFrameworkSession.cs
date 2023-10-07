using EntityDb.Common.Disposables;
using EntityDb.Common.Exceptions;
using EntityDb.EntityFramework.Predicates;
using EntityDb.EntityFramework.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Pointer = EntityDb.Abstractions.ValueObjects.Pointer;

namespace EntityDb.EntityFramework.Sessions;

internal class EntityFrameworkSession<TSnapshot, TDbContext> : DisposableResourceBaseClass, IEntityFrameworkSession<TSnapshot>
    where TSnapshot : class, IEntityFrameworkSnapshot<TSnapshot>
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly DbSet<SnapshotReference<TSnapshot>> _snapshotReferences;
    private readonly DbSet<TSnapshot> _snapshots;
    private readonly EntityFrameworkSnapshotSessionOptions _options;

    DbContext IEntityFrameworkSession<TSnapshot>.DbContext => _dbContext;
    private IDbContextTransaction? Transaction { get; set; }

    public EntityFrameworkSession(TDbContext dbContext, EntityFrameworkSnapshotSessionOptions options)
    {
        _dbContext = dbContext;
        _snapshotReferences = dbContext.Set<SnapshotReference<TSnapshot>>();
        _snapshots = dbContext.Set<TSnapshot>();
        _options = options;
    }

    private static Expression<Func<SnapshotReference<TSnapshot>, bool>> SnapshotPointerPredicate(Pointer snapshotPointer)
    {
        return snapshotReference =>
            snapshotReference.PointerId == snapshotPointer.Id &&
            snapshotReference.PointerVersionNumber == snapshotPointer.VersionNumber;
    }

    private async Task<bool> ShouldDeleteSnapshot(SnapshotReference<TSnapshot> snapshotReference, CancellationToken cancellationToken)
    {
        if (_options.KeepSnapshotsWithoutSnapshotReferences)
        {
            return false;
        }

        var otherSnapshotReferences = await _snapshotReferences
            .Where
            (
                relatedSnapshotReference =>
                    relatedSnapshotReference.Id != snapshotReference.Id &&
                    relatedSnapshotReference.SnapshotId == snapshotReference.SnapshotId &&
                    relatedSnapshotReference.SnapshotVersionNumber == snapshotReference.SnapshotVersionNumber
            )
            .AnyAsync(cancellationToken);

        return !otherSnapshotReferences;
    }

    public async Task Delete(IEnumerable<Pointer> snapshotPointers, CancellationToken cancellationToken)
    {
        AssertNotReadOnly();

        var snapshotReferences = await _snapshotReferences
            .Include(snapshotReference => snapshotReference.Snapshot)
            .Where(PredicateExpressionBuilder.Or(snapshotPointers, SnapshotPointerPredicate))
            .ToArrayAsync(cancellationToken);

        foreach (var snapshotReference in snapshotReferences)
        {
            if (await ShouldDeleteSnapshot(snapshotReference, cancellationToken))
            {
                _snapshots.Remove(snapshotReference.Snapshot);
            }

            _snapshotReferences.Remove(snapshotReference);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TSnapshot?> Get(Pointer snapshotPointer, CancellationToken cancellationToken)
    {
        var snapshotReference = await _snapshotReferences
            .Include(reference => reference.Snapshot)
            .AsNoTracking()
            .Where(SnapshotPointerPredicate(snapshotPointer))
            .SingleOrDefaultAsync(cancellationToken);

        return snapshotReference?.Snapshot;
    }

    public async Task Upsert(Pointer snapshotPointer, TSnapshot snapshot, CancellationToken cancellationToken)
    {
        AssertNotReadOnly();

        var snapshotExists = await _snapshots
            .Where(snapshot.GetKeyPredicate())
            .AnyAsync(cancellationToken);

        if (!snapshotExists)
        {
            _snapshots.Add(snapshot);
        }

        var previousSnapshotReference = await _snapshotReferences
            .Include(snapshotReference => snapshotReference.Snapshot)
            .Where(SnapshotPointerPredicate(snapshotPointer))
            .SingleOrDefaultAsync(cancellationToken);

        if (previousSnapshotReference != null)
        {
            if (await ShouldDeleteSnapshot(previousSnapshotReference, cancellationToken))
            {
                _snapshots.Remove(previousSnapshotReference.Snapshot);
            }

            previousSnapshotReference.SnapshotId = snapshot.GetId();
            previousSnapshotReference.SnapshotVersionNumber = snapshot.GetVersionNumber();
            previousSnapshotReference.Snapshot = snapshot;
        }
        else
        {
            _snapshotReferences.Add(new SnapshotReference<TSnapshot>
            {
                Id = Guid.NewGuid(),
                PointerId = snapshotPointer.Id,
                PointerVersionNumber = snapshotPointer.VersionNumber,
                SnapshotId = snapshot.GetId(),
                SnapshotVersionNumber = snapshot.GetVersionNumber(),
                Snapshot = snapshot,
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void AssertNotReadOnly()
    {
        if (_options.ReadOnly)
        {
            throw new CannotWriteInReadOnlyModeException();
        }
    }

    public static IEntityFrameworkSession<TSnapshot> Create
    (
        IServiceProvider serviceProvider,
        TDbContext dbContext,
        EntityFrameworkSnapshotSessionOptions options
    )
    {
        return ActivatorUtilities.CreateInstance<EntityFrameworkSession<TSnapshot, TDbContext>>(serviceProvider, dbContext, options);
    }

    public async Task StartTransaction(CancellationToken cancellationToken)
    {
        Transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    [ExcludeFromCodeCoverage(Justification =
        "Tests should run with the Debug configuration, and should not execute this method.")]
    public async Task CommitTransaction(CancellationToken cancellationToken)
    {
        if (Transaction != null)
        {
            await Transaction.CommitAsync(cancellationToken);
            await Transaction.DisposeAsync();

            Transaction = null;
        }
    }

    public async Task AbortTransaction(CancellationToken cancellationToken)
    {
        if (Transaction != null)
        {
            await Transaction.RollbackAsync(cancellationToken);
            await Transaction.DisposeAsync();

            Transaction = null;
        }
    }

    public IEntityFrameworkSession<TSnapshot> WithSnapshotSessionOptions(EntityFrameworkSnapshotSessionOptions snapshotSessionOptions)
    {
        return new EntityFrameworkSession<TSnapshot, TDbContext>(_dbContext, snapshotSessionOptions);
    }

    public override ValueTask DisposeAsync()
    {
        return _dbContext.DisposeAsync();
    }
}

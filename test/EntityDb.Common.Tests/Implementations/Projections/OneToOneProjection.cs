using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using EntityDb.Abstractions.States;
using EntityDb.Abstractions.Sources;
using EntityDb.Abstractions.Transactions;
using EntityDb.Abstractions.ValueObjects;
using EntityDb.Common.Extensions;
using EntityDb.Common.Projections;
using EntityDb.Common.Queries;
using EntityDb.Common.Tests.Implementations.Snapshots;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pointer = EntityDb.Abstractions.ValueObjects.Pointer;

namespace EntityDb.Common.Tests.Implementations.Projections;

public class OneToOneProjection : IProjection<OneToOneProjection>, ISnapshotWithTestLogic<OneToOneProjection>
{
    public required Id Id { get; set; }
    public TimeStamp LastTransactionAt { get; set; }
    public VersionNumber VersionNumber { get; set; }

    public static OneToOneProjection Construct(Id projectionId)
    {
        return new OneToOneProjection
        { 
            Id = projectionId,
        };
    }

    public static void Configure(EntityTypeBuilder<OneToOneProjection> oneToOneProjectionBuilder)
    {
        oneToOneProjectionBuilder
            .HasKey(oneToOneProjection => new
            {
                oneToOneProjection.Id,
                oneToOneProjection.VersionNumber,
            });
    }

    public Id GetId()
    {
        return Id;
    }

    public VersionNumber GetVersionNumber()
    {
        return VersionNumber;
    }

    public void Mutate(ISource source)
    {
        if (source is not ITransaction transaction)
        {
            throw new NotSupportedException();
        }

        LastTransactionAt = transaction.TimeStamp;

        foreach (var command in transaction.Commands)
        {
            if (command.Data is not IMutator<OneToOneProjection> mutator)
            {
                continue;
            }

            mutator.Mutate(this);

            VersionNumber = command.EntityVersionNumber;
        }
    }

    public bool ShouldRecord()
    {
        return ShouldRecordLogic.Value is not null && ShouldRecordLogic.Value.Invoke(this);
    }

    public bool ShouldRecordAsLatest(OneToOneProjection? previousSnapshot)
    {
        return ShouldRecordAsLatestLogic.Value is not null && ShouldRecordAsLatestLogic.Value.Invoke(this, previousSnapshot);
    }

    public async IAsyncEnumerable<ISource> EnumerateSources
    (
        ISourceRepository sourceRepository,
        Pointer projectionPointer,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var query = new GetEntityCommandsQuery(projectionPointer, VersionNumber);

        var transactionIds = await sourceRepository.TransactionRepository
            .EnumerateTransactionIds(query, cancellationToken)
            .ToArrayAsync(cancellationToken);

        foreach (var transactionId in transactionIds)
        {
            yield return await sourceRepository.TransactionRepository
                .GetTransaction(transactionId, cancellationToken);
        }
    }

    public static IEnumerable<Id> EnumerateProjectionIds(ISource source)
    {
        var projectionIds = new HashSet<Id>();

        if (source is ITransaction transaction)
        {
            foreach (var command in transaction.Commands)
            {
                if (command.Data is not IReducer<OneToOneProjection>)
                {
                    continue;
                }

                projectionIds.Add(command.EntityId);
            }
        }

        return projectionIds;
    }

    public static string RedisKeyNamespace => "one-to-one-projection";

    public OneToOneProjection WithVersionNumber(VersionNumber versionNumber)
    {
        VersionNumber = versionNumber;

        return this;
    }

    public static AsyncLocal<Func<OneToOneProjection, bool>?> ShouldRecordLogic { get; } = new();

    public static AsyncLocal<Func<OneToOneProjection, OneToOneProjection?, bool>?> ShouldRecordAsLatestLogic { get; } =
        new();

    public Expression<Func<OneToOneProjection, bool>> GetKeyPredicate()
    {
        return (oneToOneProjection) => oneToOneProjection.Id == Id && oneToOneProjection.VersionNumber == VersionNumber;
    }
}
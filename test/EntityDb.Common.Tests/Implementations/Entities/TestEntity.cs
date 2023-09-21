using System.Linq.Expressions;
using EntityDb.Abstractions.States;
using EntityDb.Abstractions.ValueObjects;
using EntityDb.Common.Entities;
using EntityDb.Common.Tests.Implementations.Snapshots;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntityDb.Common.Tests.Implementations.Entities;

public record TestEntity : IEntity<TestEntity>, ISnapshotWithTestLogic<TestEntity>
{
    public required Id Id { get; init; }
    public VersionNumber VersionNumber { get; init; }

    public static TestEntity Construct(Id entityId)
    {
        return new TestEntity
        {
            Id = entityId,
        };
    }

    public static void Configure(EntityTypeBuilder<TestEntity> testEntityBuilder)
    {
        testEntityBuilder
            .HasKey(testEntity => new
            {
                testEntity.Id,
                testEntity.VersionNumber,
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

    public static bool CanReduce(object command)
    {
        return command is IReducer<TestEntity>;
    }

    public TestEntity Reduce(object command)
    {
        if (command is IReducer<TestEntity> reducer)
        {
            return reducer.Reduce(this);
        }

        throw new NotSupportedException();
    }

    public bool ShouldRecord()
    {
        return ShouldRecordLogic.Value is not null && ShouldRecordLogic.Value.Invoke(this);
    }

    public bool ShouldRecordAsLatest(TestEntity? previousMostRecentSnapshot)
    {
        return ShouldRecordAsLatestLogic.Value is not null && ShouldRecordAsLatestLogic.Value.Invoke(this, previousMostRecentSnapshot);
    }

    public static string RedisKeyNamespace => "test-entity";

    public TestEntity WithVersionNumber(VersionNumber versionNumber)
    {
        return this with { VersionNumber = versionNumber };
    }

    public static AsyncLocal<Func<TestEntity, bool>?> ShouldRecordLogic { get; } = new();

    public static AsyncLocal<Func<TestEntity, TestEntity?, bool>?> ShouldRecordAsLatestLogic { get; } = new();

    public Expression<Func<TestEntity, bool>> GetKeyPredicate()
    {
        return (testEntity) => testEntity.Id == Id && testEntity.VersionNumber == VersionNumber;
    }
}
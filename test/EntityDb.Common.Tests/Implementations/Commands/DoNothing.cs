using EntityDb.Abstractions.Commands;
using EntityDb.Abstractions.Leases;
using EntityDb.Abstractions.States;
using EntityDb.Abstractions.Tags;
using EntityDb.Common.Tests.Implementations.Entities;
using EntityDb.Common.Tests.Implementations.Projections;

namespace EntityDb.Common.Tests.Implementations.Commands;

public record DoNothing : IReducer<TestEntity>, IMutator<OneToOneProjection>
{
    public void Mutate(OneToOneProjection projection)
    {
        projection.VersionNumber = projection.VersionNumber.Next();
    }

    public TestEntity Reduce(TestEntity entity)
    {
        return entity with { VersionNumber = entity.VersionNumber.Next() };
    }
}

public record AddLease(ILease Lease) : DoNothing, IAddLeasesCommand
{
    public IEnumerable<ILease> GetLeases()
    {
        yield return Lease;
    }
}

public record DeleteLease(ILease Lease) : DoNothing, IDeleteLeasesCommand
{
    public IEnumerable<ILease> GetLeases()
    {
        yield return Lease;
    }
}

public record AddTag(ITag Tag) : DoNothing, IAddTagsCommand
{
    public IEnumerable<ITag> GetTags()
    {
        yield return Tag;
    }
}

public record DeleteTag(ITag Tag) : DoNothing, IDeleteTagsCommand
{
    public IEnumerable<ITag> GetTags()
    {
        yield return Tag;
    }
}
using EntityDb.Abstractions.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace EntityDb.EntityFramework.Converters;

internal class IdConverter : ValueConverter<Id, Guid>
{
    private static readonly Expression<Func<Id, Guid>> IdToGuid = id => id.Value;
    private static readonly Expression<Func<Guid, Id>> GuidToId = guid => new Id(guid);

    public IdConverter() : base(IdToGuid, GuidToId)
    {
    }
}

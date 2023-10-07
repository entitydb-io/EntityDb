using EntityDb.Abstractions.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace EntityDb.EntityFramework.Converters;

internal class VersionNumberConverter : ValueConverter<VersionNumber, ulong>
{
    private static readonly Expression<Func<VersionNumber, ulong>> VersionNumberToUlong = versionNumber => versionNumber.Value;
    private static readonly Expression<Func<ulong, VersionNumber>> UlongToVersionNumber = @ulong => new VersionNumber(@ulong);

    public VersionNumberConverter() : base(VersionNumberToUlong, UlongToVersionNumber)
    {
    }
}

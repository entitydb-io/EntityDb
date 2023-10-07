using EntityDb.Abstractions.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace EntityDb.EntityFramework.Converters;

internal class TimeStampConverter : ValueConverter<TimeStamp, DateTime>
{
    private static readonly Expression<Func<TimeStamp, DateTime>> TimeStampToDateTime = timeStamp => timeStamp.Value;
    private static readonly Expression<Func<DateTime, TimeStamp>> DateTimeToTimeStamp = dateTime => new TimeStamp(dateTime);

    public TimeStampConverter() : base(TimeStampToDateTime, DateTimeToTimeStamp)
    {
    }
}

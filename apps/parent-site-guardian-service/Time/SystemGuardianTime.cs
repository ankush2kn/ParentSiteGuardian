namespace ParentSiteGuardian.Service.Time;

public sealed class SystemGuardianTime : IGuardianTime
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public TimeZoneInfo ReportingTimeZone => TimeZoneInfo.Local;

    public (DateTimeOffset startInclusive, DateTimeOffset endExclusive) GetLocalDayBounds(DateTimeOffset? instant = null)
    {
        var point = instant ?? UtcNow;
        var local = TimeZoneInfo.ConvertTime(point, ReportingTimeZone);
        var d = local.Date;
        var startUnspecified = DateTime.SpecifyKind(d, DateTimeKind.Unspecified);
        var offset = ReportingTimeZone.GetUtcOffset(startUnspecified);
        var start = new DateTimeOffset(d.Year, d.Month, d.Day, 0, 0, 0, offset);
        return (start, start.AddDays(1));
    }

    public (DateTimeOffset startInclusive, DateTimeOffset endExclusive) GetLocalDayBoundsForLocalDate(DateOnly localDate)
    {
        var d = new DateTime(localDate.Year, localDate.Month, localDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var offset = ReportingTimeZone.GetUtcOffset(d);
        var start = new DateTimeOffset(localDate.Year, localDate.Month, localDate.Day, 0, 0, 0, offset);
        return (start, start.AddDays(1));
    }
}

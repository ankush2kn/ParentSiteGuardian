namespace ParentSiteGuardian.Service.Time;

/// <summary>Deterministic clock for tests.</summary>
public sealed class FixedGuardianTime(DateTimeOffset utcNow, TimeZoneInfo reportingTimeZone) : IGuardianTime
{
    public DateTimeOffset UtcNow => utcNow;
    public TimeZoneInfo ReportingTimeZone => reportingTimeZone;

    public (DateTimeOffset startInclusive, DateTimeOffset endExclusive) GetLocalDayBounds(DateTimeOffset? instant = null)
    {
        var point = instant ?? utcNow;
        var local = TimeZoneInfo.ConvertTime(point, reportingTimeZone);
        var d = local.Date;
        var startUnspecified = DateTime.SpecifyKind(d, DateTimeKind.Unspecified);
        var offset = reportingTimeZone.GetUtcOffset(startUnspecified);
        var start = new DateTimeOffset(d.Year, d.Month, d.Day, 0, 0, 0, offset);
        return (start, start.AddDays(1));
    }

    public (DateTimeOffset startInclusive, DateTimeOffset endExclusive) GetLocalDayBoundsForLocalDate(DateOnly localDate)
    {
        var d = new DateTime(localDate.Year, localDate.Month, localDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var offset = reportingTimeZone.GetUtcOffset(d);
        var start = new DateTimeOffset(localDate.Year, localDate.Month, localDate.Day, 0, 0, 0, offset);
        return (start, start.AddDays(1));
    }
}

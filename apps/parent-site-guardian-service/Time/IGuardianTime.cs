namespace ParentSiteGuardian.Service.Time;

/// <summary>Reporting "today" boundaries for daily budgets (laptop local timezone in production).</summary>
public interface IGuardianTime
{
    DateTimeOffset UtcNow { get; }
    TimeZoneInfo ReportingTimeZone { get; }

    /// <summary>Start of local calendar day for <paramref name="instant"/> (inclusive) and start of next day (exclusive).</summary>
    (DateTimeOffset startInclusive, DateTimeOffset endExclusive) GetLocalDayBounds(DateTimeOffset? instant = null);

    /// <summary>Local midnight bounds for a calendar date in <see cref="ReportingTimeZone"/>.</summary>
    (DateTimeOffset startInclusive, DateTimeOffset endExclusive) GetLocalDayBoundsForLocalDate(DateOnly localDate);
}

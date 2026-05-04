namespace ParentSiteGuardian.Service.Api;

public sealed class UsageTickRequest
{
    public required string DeviceId { get; set; }
    public required string Browser { get; set; }
    public required string Url { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }
    public int ActiveDurationSeconds { get; set; }
}

public sealed class UsageTickResponse
{
    public bool Recorded { get; set; }
    public string? SiteGroupId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class DecisionRequest
{
    public required string DeviceId { get; set; }
    public required string Url { get; set; }
}

public sealed class DecisionResponse
{
    public bool Matched { get; set; }
    public string? SiteGroupId { get; set; }
    public string? SiteGroupName { get; set; }
    public bool Allowed { get; set; }
    public int UsedSeconds { get; set; }
    /// <summary>Effective daily ceiling: base limit plus non-expired override bonus seconds.</summary>
    public int LimitSeconds { get; set; }
    public int RemainingSeconds { get; set; }
    public string Reason { get; set; } = string.Empty;

    /// <summary>Daily cap from <c>daily_limits</c> (before active override bonus).</summary>
    public int? BaseLimitSeconds { get; set; }

    /// <summary>Seconds added by non-expired overrides for this device/group.</summary>
    public int? OverrideBonusSeconds { get; set; }

    /// <summary>When the daily budget resets (start of next local calendar day), ISO-8601.</summary>
    public string? NextResetAt { get; set; }
}

public sealed class TodayUsageResponse
{
    public required string LocalDate { get; set; }
    public required string DeviceId { get; set; }
    public required IReadOnlyList<TodaySiteGroupUsageDto> Groups { get; set; }
}

public sealed class TodaySiteGroupUsageDto
{
    public required string SiteGroupId { get; set; }
    public required string SiteGroupName { get; set; }
    public int UsedSeconds { get; set; }
    public int BaseLimitSeconds { get; set; }
    public int OverrideBonusSeconds { get; set; }
    public int EffectiveLimitSeconds { get; set; }
    public int RemainingSeconds { get; set; }
}

public sealed class UsageHistoryResponse
{
    public required string DeviceId { get; set; }
    public required IReadOnlyList<UsageHistoryDayDto> Days { get; set; }
}

public sealed class UsageHistoryDayDto
{
    public required string LocalDate { get; set; }
    public required IReadOnlyList<UsageHistoryGroupDto> Groups { get; set; }
}

public sealed class UsageHistoryGroupDto
{
    public required string SiteGroupId { get; set; }
    public required string SiteGroupName { get; set; }
    public int UsedSeconds { get; set; }
}

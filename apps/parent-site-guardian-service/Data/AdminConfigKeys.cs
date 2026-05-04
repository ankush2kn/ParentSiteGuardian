namespace ParentSiteGuardian.Service.Data;

public static class AdminConfigKeys
{
    /// <summary>Positive integer seconds between browser usage ticks (extension polling interval).</summary>
    public const string UsageTickIntervalSeconds = "usage_tick_interval_seconds";

    /// <summary>BCrypt hash of the parent PIN (set via POST /api/admin/pin/setup).</summary>
    public const string ParentPinHash = "parent_pin_hash";
}

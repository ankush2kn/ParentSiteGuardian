namespace ParentSiteGuardian.Service.Data;

public static class AdminConfigKeys
{
    /// <summary>Positive integer seconds between browser usage ticks (extension polling interval).</summary>
    public const string UsageTickIntervalSeconds = "usage_tick_interval_seconds";

    /// <summary>BCrypt hash of the parent PIN (set via POST /api/admin/pin/setup).</summary>
    public const string ParentPinHash = "parent_pin_hash";

    /// <summary>Consecutive wrong PIN attempts since the last correct one (integer).</summary>
    public const string PinFailedAttempts = "pin_failed_attempts";

    /// <summary>ISO-8601 UTC instant until which PIN attempts are rejected; empty when not locked.</summary>
    public const string PinLockedUntil = "pin_locked_until";
}

namespace ParentSiteGuardian.Service.Services;

public enum ParentPinSetupResult
{
    Ok,
    AlreadyConfigured,
    InvalidPin,
}

public enum ParentPinChangeResult
{
    Ok,
    NotConfigured,
    InvalidCurrentPin,
    InvalidNewPin,
    LockedOut,
}

public enum PinCheckStatus
{
    Ok,
    Invalid,
    LockedOut,
    NotConfigured,
}

/// <summary>
/// Outcome of a PIN attempt. <see cref="RetryAfter"/> is how long until the next attempt is accepted
/// (zero when no wait applies). It is set for <see cref="PinCheckStatus.LockedOut"/>, and also for
/// <see cref="PinCheckStatus.Invalid"/> when that wrong attempt started a new lockout.
/// </summary>
public sealed record PinCheckResult(PinCheckStatus Status, TimeSpan RetryAfter)
{
    public bool IsOk => Status == PinCheckStatus.Ok;
}

public interface IParentPinService
{
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);

    Task<ParentPinSetupResult> SetupAsync(string pin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks a PIN with escalating lockout after repeated failures. Every caller that accepts a parent PIN
    /// must go through this so all entry points share one attempt counter.
    /// </summary>
    Task<PinCheckResult> CheckAsync(string pin, CancellationToken cancellationToken = default);

    /// <summary>Remaining lockout time, or <see cref="TimeSpan.Zero"/> when attempts are currently accepted.</summary>
    Task<TimeSpan> GetLockoutRemainingAsync(CancellationToken cancellationToken = default);

    Task<ParentPinChangeResult> ChangeAsync(string currentPin, string newPin, CancellationToken cancellationToken = default);
}

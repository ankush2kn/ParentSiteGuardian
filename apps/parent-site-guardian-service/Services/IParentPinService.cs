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
}

public interface IParentPinService
{
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);

    Task<ParentPinSetupResult> SetupAsync(string pin, CancellationToken cancellationToken = default);

    Task<bool> VerifyAsync(string pin, CancellationToken cancellationToken = default);

    Task<ParentPinChangeResult> ChangeAsync(string currentPin, string newPin, CancellationToken cancellationToken = default);
}

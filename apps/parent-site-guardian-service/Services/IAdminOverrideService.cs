namespace ParentSiteGuardian.Service.Services;

public enum AdminOverrideCreateResult
{
    Ok,
    PinNotConfigured,
    InvalidPin,
    PinLockedOut,
    InvalidExtraMinutes,
    UnknownDevice,
    UnknownSiteGroup,
}

public interface IAdminOverrideService
{
    Task<AdminOverrideCreateResult> CreateAsync(
        string deviceId,
        string siteGroupId,
        string pin,
        int extraMinutes,
        CancellationToken cancellationToken = default);
}

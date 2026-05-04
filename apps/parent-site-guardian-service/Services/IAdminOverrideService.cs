namespace ParentSiteGuardian.Service.Services;

public enum AdminOverrideCreateResult
{
    Ok,
    PinNotConfigured,
    InvalidPin,
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

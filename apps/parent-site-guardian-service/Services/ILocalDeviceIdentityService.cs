using ParentSiteGuardian.Service.Api;

namespace ParentSiteGuardian.Service.Services;

public interface ILocalDeviceIdentityService
{
    /// <summary>Ensures a stable <see cref="LocalDeviceIdentityDto.AppDeviceId"/> exists, refreshes machine snapshot in <c>settings</c>, and returns the effective view.</summary>
    Task<LocalDeviceIdentityDto> EnsurePersistAndGetAsync(CancellationToken cancellationToken = default);
}

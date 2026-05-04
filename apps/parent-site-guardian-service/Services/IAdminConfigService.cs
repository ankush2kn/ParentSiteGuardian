using ParentSiteGuardian.Service.Api;

namespace ParentSiteGuardian.Service.Services;

public interface IAdminConfigService
{
    Task<ExtensionConfigResponse> GetExtensionConfigAsync(CancellationToken cancellationToken = default);
}

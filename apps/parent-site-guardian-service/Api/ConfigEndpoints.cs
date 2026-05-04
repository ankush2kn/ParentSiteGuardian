using ParentSiteGuardian.Service.Services;

namespace ParentSiteGuardian.Service.Api;

public static class ConfigEndpoints
{
    public static IEndpointRouteBuilder MapConfigEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/config/effective", async Task<IResult> (
            IAdminConfigService adminConfig,
            ILocalDeviceIdentityService localDevice,
            CancellationToken cancellationToken) =>
        {
            var extension = await adminConfig.GetExtensionConfigAsync(cancellationToken);
            var local = await localDevice.EnsurePersistAndGetAsync(cancellationToken);
            return Results.Ok(new EffectiveConfigResponse
            {
                Extension = extension,
                LocalDevice = local,
            });
        });

        return app;
    }
}

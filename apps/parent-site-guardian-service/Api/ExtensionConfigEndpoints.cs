using ParentSiteGuardian.Service.Services;

namespace ParentSiteGuardian.Service.Api;

public static class ExtensionConfigEndpoints
{
    public static IEndpointRouteBuilder MapExtensionConfigEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/extension-config", async Task<IResult> (
            IAdminConfigService adminConfig,
            CancellationToken cancellationToken) =>
        {
            var cfg = await adminConfig.GetExtensionConfigAsync(cancellationToken);
            return Results.Ok(cfg);
        });

        return app;
    }
}

using ParentSiteGuardian.Service.Services;

namespace ParentSiteGuardian.Service.Api;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/pin/status", async Task<IResult> (IParentPinService pins, CancellationToken ct) =>
        {
            var configured = await pins.IsConfiguredAsync(ct);
            return Results.Ok(new PinStatusResponse { Configured = configured });
        });

        app.MapPost("/api/admin/pin/setup", async Task<IResult> (PinSetupRequest body, IParentPinService pins, CancellationToken ct) =>
        {
            var result = await pins.SetupAsync(body.Pin, ct);
            return result switch
            {
                ParentPinSetupResult.Ok => Results.Ok(new { status = "ok" }),
                ParentPinSetupResult.AlreadyConfigured => Results.Conflict(new { error = "pin_already_configured" }),
                ParentPinSetupResult.InvalidPin => Results.BadRequest(new { error = "invalid_pin" }),
                _ => Results.BadRequest(),
            };
        });

        app.MapPost("/api/admin/pin/verify", async Task<IResult> (PinVerifyRequest body, IParentPinService pins, CancellationToken ct) =>
        {
            var check = await pins.CheckAsync(body.Pin, ct);
            return check.Status switch
            {
                PinCheckStatus.Ok => Results.Ok(new PinVerifyResponse { Valid = true }),
                PinCheckStatus.LockedOut => PinResults.LockedOut(check.RetryAfter),
                _ => Results.Ok(new PinVerifyResponse
                {
                    Valid = false,
                    RetryAfterSeconds = PinResults.ToSeconds(check.RetryAfter),
                }),
            };
        });

        app.MapPost("/api/admin/pin/change", async Task<IResult> (PinChangeRequest body, IParentPinService pins, CancellationToken ct) =>
        {
            var result = await pins.ChangeAsync(body.CurrentPin, body.NewPin, ct);
            return result switch
            {
                ParentPinChangeResult.Ok => Results.Ok(new { status = "ok" }),
                ParentPinChangeResult.NotConfigured => Results.Conflict(new { error = "pin_not_configured" }),
                ParentPinChangeResult.InvalidCurrentPin => PinResults.Invalid(await pins.GetLockoutRemainingAsync(ct)),
                ParentPinChangeResult.LockedOut => PinResults.LockedOut(await pins.GetLockoutRemainingAsync(ct)),
                ParentPinChangeResult.InvalidNewPin => Results.BadRequest(new { error = "invalid_new_pin" }),
                _ => Results.BadRequest(),
            };
        });

        app.MapPost("/api/admin/overrides", async Task<IResult> (
            AdminOverrideRequest body,
            IAdminOverrideService overrides,
            IParentPinService pins,
            CancellationToken ct) =>
        {
            var result = await overrides.CreateAsync(body.DeviceId, body.SiteGroupId, body.Pin, body.ExtraMinutes, ct);
            return result switch
            {
                AdminOverrideCreateResult.Ok => Results.Ok(new { status = "ok" }),
                AdminOverrideCreateResult.PinNotConfigured => Results.Conflict(new { error = "pin_not_configured" }),
                AdminOverrideCreateResult.InvalidPin => PinResults.Invalid(await pins.GetLockoutRemainingAsync(ct)),
                AdminOverrideCreateResult.PinLockedOut => PinResults.LockedOut(await pins.GetLockoutRemainingAsync(ct)),
                AdminOverrideCreateResult.InvalidExtraMinutes => Results.BadRequest(new { error = "invalid_extra_minutes" }),
                AdminOverrideCreateResult.UnknownDevice => Results.NotFound(new { error = "unknown_device" }),
                AdminOverrideCreateResult.UnknownSiteGroup => Results.NotFound(new { error = "unknown_site_group" }),
                _ => Results.BadRequest(),
            };
        });

        return app;
    }
}

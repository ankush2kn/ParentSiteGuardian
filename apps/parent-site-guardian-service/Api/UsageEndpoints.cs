using ParentSiteGuardian.Service.Services;

namespace ParentSiteGuardian.Service.Api;

public static class UsageEndpoints
{
    public static IEndpointRouteBuilder MapUsageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/usage/tick", async Task<IResult> (
            UsageTickRequest body,
            IUsageAccountingService usage,
            CancellationToken cancellationToken) =>
        {
            var result = await usage.RecordTickAsync(body, cancellationToken);
            if (result.Reason == "unknown_device")
                return Results.NotFound(result);
            return Results.Ok(result);
        });

        app.MapPost("/api/decision", async Task<IResult> (
            DecisionRequest body,
            IUsageAccountingService usage,
            CancellationToken cancellationToken) =>
        {
            var result = await usage.DecideAsync(body, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        app.MapGet("/api/usage/today", async Task<IResult> (
            string deviceId,
            IUsageAccountingService usage,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return Results.BadRequest(new { error = "deviceId is required" });
            var result = await usage.GetTodayAsync(deviceId.Trim(), cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        return app;
    }
}

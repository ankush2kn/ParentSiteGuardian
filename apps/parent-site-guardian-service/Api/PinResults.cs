using System.Globalization;

namespace ParentSiteGuardian.Service.Api;

/// <summary>
/// Shared HTTP shapes for PIN failures so the admin UI can show how long to wait.
/// Wrong PIN → 401 <c>{ error: "invalid_pin", retryAfterSeconds }</c>;
/// locked → 429 <c>{ error: "pin_locked", retryAfterSeconds }</c> plus a <c>Retry-After</c> header.
/// </summary>
internal static class PinResults
{
    public static int ToSeconds(TimeSpan wait) => wait <= TimeSpan.Zero ? 0 : (int)Math.Ceiling(wait.TotalSeconds);

    public static IResult Invalid(TimeSpan retryAfter) =>
        Results.Json(new { error = "invalid_pin", retryAfterSeconds = ToSeconds(retryAfter) }, statusCode: StatusCodes.Status401Unauthorized);

    public static IResult LockedOut(TimeSpan retryAfter) => new LockedOutResult(Math.Max(1, ToSeconds(retryAfter)));

    private sealed class LockedOutResult(int seconds) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
            return Results.Json(new { error = "pin_locked", retryAfterSeconds = seconds }, statusCode: StatusCodes.Status429TooManyRequests)
                .ExecuteAsync(httpContext);
        }
    }
}

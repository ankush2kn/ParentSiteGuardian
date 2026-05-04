using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Api;
using ParentSiteGuardian.Service.Data;

namespace ParentSiteGuardian.Service.Services;

public sealed class AdminConfigService(AppDbContext db) : IAdminConfigService
{
    private const int MinTickSeconds = 1;
    private const int MaxTickSeconds = 3600;
    private const int DefaultTickSeconds = 10;

    public async Task<ExtensionConfigResponse> GetExtensionConfigAsync(CancellationToken cancellationToken = default)
    {
        var row = await db.AdminConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == AdminConfigKeys.UsageTickIntervalSeconds, cancellationToken);
        var seconds = ParseTickSeconds(row?.Value);
        return new ExtensionConfigResponse(UsageTickIntervalSeconds: seconds);
    }

    private static int ParseTickSeconds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return DefaultTickSeconds;
        if (!int.TryParse(raw.Trim(), out var v))
            return DefaultTickSeconds;
        if (v < MinTickSeconds)
            return MinTickSeconds;
        if (v > MaxTickSeconds)
            return MaxTickSeconds;
        return v;
    }
}

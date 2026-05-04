using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Data.Entities;
using ParentSiteGuardian.Service.Time;

namespace ParentSiteGuardian.Service.Services;

public sealed class AdminOverrideService(
    AppDbContext db,
    IParentPinService parentPin,
    IGuardianTime time) : IAdminOverrideService
{
    private static readonly int[] AllowedMinutes = [5, 10, 15, 30];

    public async Task<AdminOverrideCreateResult> CreateAsync(
        string deviceId,
        string siteGroupId,
        string pin,
        int extraMinutes,
        CancellationToken cancellationToken = default)
    {
        if (!await parentPin.IsConfiguredAsync(cancellationToken))
            return AdminOverrideCreateResult.PinNotConfigured;

        if (!await parentPin.VerifyAsync(pin, cancellationToken))
            return AdminOverrideCreateResult.InvalidPin;

        if (!AllowedMinutes.Contains(extraMinutes))
            return AdminOverrideCreateResult.InvalidExtraMinutes;

        if (!await db.Devices.AsNoTracking().AnyAsync(d => d.Id == deviceId, cancellationToken))
            return AdminOverrideCreateResult.UnknownDevice;

        if (!await db.SiteGroups.AsNoTracking().AnyAsync(g => g.Id == siteGroupId, cancellationToken))
            return AdminOverrideCreateResult.UnknownSiteGroup;

        var now = time.UtcNow;
        var extraSeconds = extraMinutes * 60;
        db.Overrides.Add(new Override
        {
            Id = Guid.NewGuid().ToString(),
            DeviceId = deviceId,
            SiteGroupId = siteGroupId,
            ExtraSeconds = extraSeconds,
            ExpiresAt = now.AddMinutes(extraMinutes),
            CreatedAt = now,
        });
        await db.SaveChangesAsync(cancellationToken);
        return AdminOverrideCreateResult.Ok;
    }
}

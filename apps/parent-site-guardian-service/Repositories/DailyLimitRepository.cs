using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Repositories;

public sealed class DailyLimitRepository(AppDbContext db) : IDailyLimitRepository
{
    public Task<DailyLimit?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        db.DailyLimits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<DailyLimit?> GetByDeviceAndSiteGroupAsync(string deviceId, string siteGroupId, CancellationToken cancellationToken = default) =>
        db.DailyLimits.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DeviceId == deviceId && x.SiteGroupId == siteGroupId, cancellationToken);

    public async Task<IReadOnlyList<DailyLimit>> ListByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default) =>
        await db.DailyLimits.AsNoTracking()
            .Where(x => x.DeviceId == deviceId)
            .OrderBy(x => x.SiteGroupId)
            .ToListAsync(cancellationToken);

    public async Task<DailyLimit> AddAsync(DailyLimit limit, CancellationToken cancellationToken = default)
    {
        db.DailyLimits.Add(limit);
        await db.SaveChangesAsync(cancellationToken);
        return limit;
    }

    public async Task UpdateAsync(DailyLimit limit, CancellationToken cancellationToken = default)
    {
        var tracked = await db.DailyLimits.FirstOrDefaultAsync(x => x.Id == limit.Id, cancellationToken);
        if (tracked is null)
            return;
        tracked.DeviceId = limit.DeviceId;
        tracked.SiteGroupId = limit.SiteGroupId;
        tracked.LimitSeconds = limit.LimitSeconds;
        tracked.CreatedAt = limit.CreatedAt;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await db.DailyLimits.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            return;
        db.DailyLimits.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}

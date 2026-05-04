using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Repositories;

public sealed class OverrideRepository(AppDbContext db) : IOverrideRepository
{
    public Task<Override?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        db.Overrides.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Override>> ListByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var rows = await db.Overrides.AsNoTracking()
            .Where(x => x.DeviceId == deviceId)
            .ToListAsync(cancellationToken);
        return rows.OrderByDescending(x => x.CreatedAt).ToList();
    }

    public async Task<Override> AddAsync(Override overrideEntity, CancellationToken cancellationToken = default)
    {
        db.Overrides.Add(overrideEntity);
        await db.SaveChangesAsync(cancellationToken);
        return overrideEntity;
    }

    public async Task UpdateAsync(Override overrideEntity, CancellationToken cancellationToken = default)
    {
        var tracked = await db.Overrides.FirstOrDefaultAsync(x => x.Id == overrideEntity.Id, cancellationToken);
        if (tracked is null)
            return;
        tracked.ExtraSeconds = overrideEntity.ExtraSeconds;
        tracked.ExpiresAt = overrideEntity.ExpiresAt;
        tracked.CreatedAt = overrideEntity.CreatedAt;
        tracked.DeviceId = overrideEntity.DeviceId;
        tracked.SiteGroupId = overrideEntity.SiteGroupId;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Overrides.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            return;
        db.Overrides.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}

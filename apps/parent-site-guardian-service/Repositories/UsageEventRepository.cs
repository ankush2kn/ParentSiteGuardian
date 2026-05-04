using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Repositories;

public sealed class UsageEventRepository(AppDbContext db) : IUsageEventRepository
{
    public Task<UsageEvent?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        db.UsageEvents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UsageEvent>> ListByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        // SQLite: ORDER BY DateTimeOffset is not translated; sort after materialize.
        var rows = await db.UsageEvents.AsNoTracking()
            .Where(x => x.DeviceId == deviceId)
            .ToListAsync(cancellationToken);
        return rows.OrderByDescending(x => x.StartedAt).ToList();
    }

    public async Task<UsageEvent> AddAsync(UsageEvent usageEvent, CancellationToken cancellationToken = default)
    {
        db.UsageEvents.Add(usageEvent);
        await db.SaveChangesAsync(cancellationToken);
        return usageEvent;
    }

    public async Task UpdateAsync(UsageEvent usageEvent, CancellationToken cancellationToken = default)
    {
        var tracked = await db.UsageEvents.FirstOrDefaultAsync(x => x.Id == usageEvent.Id, cancellationToken);
        if (tracked is null)
            return;
        tracked.ChildId = usageEvent.ChildId;
        tracked.DeviceId = usageEvent.DeviceId;
        tracked.SiteGroupId = usageEvent.SiteGroupId;
        tracked.Browser = usageEvent.Browser;
        tracked.Url = usageEvent.Url;
        tracked.StartedAt = usageEvent.StartedAt;
        tracked.EndedAt = usageEvent.EndedAt;
        tracked.DurationSeconds = usageEvent.DurationSeconds;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await db.UsageEvents.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            return;
        db.UsageEvents.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}

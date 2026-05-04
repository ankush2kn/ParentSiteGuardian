using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Repositories;

public sealed class SiteGroupRepository(AppDbContext db) : ISiteGroupRepository
{
    public Task<SiteGroup?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        db.SiteGroups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<SiteGroup?> GetByIdWithRulesAsync(string id, CancellationToken cancellationToken = default) =>
        db.SiteGroups.AsNoTracking()
            .Include(x => x.SiteRules)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SiteGroup>> ListByChildIdAsync(string childId, CancellationToken cancellationToken = default) =>
        await db.SiteGroups.AsNoTracking()
            .Where(x => x.ChildId == childId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SiteGroup>> ListByChildIdWithRulesAsync(string childId, CancellationToken cancellationToken = default) =>
        await db.SiteGroups.AsNoTracking()
            .Include(x => x.SiteRules)
            .Where(x => x.ChildId == childId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task<SiteGroup> AddAsync(SiteGroup group, CancellationToken cancellationToken = default)
    {
        db.SiteGroups.Add(group);
        await db.SaveChangesAsync(cancellationToken);
        return group;
    }

    public async Task UpdateAsync(SiteGroup group, CancellationToken cancellationToken = default)
    {
        var tracked = await db.SiteGroups.FirstOrDefaultAsync(x => x.Id == group.Id, cancellationToken);
        if (tracked is null)
            return;
        tracked.Name = group.Name;
        tracked.Color = group.Color;
        tracked.CreatedAt = group.CreatedAt;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await db.SiteGroups.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            return;
        db.SiteGroups.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}

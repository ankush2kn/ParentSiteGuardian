using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Repositories;

public sealed class SiteRuleRepository(AppDbContext db) : ISiteRuleRepository
{
    public Task<SiteRule?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        db.SiteRules.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SiteRule>> ListBySiteGroupIdAsync(string siteGroupId, CancellationToken cancellationToken = default) =>
        await db.SiteRules.AsNoTracking()
            .Where(x => x.SiteGroupId == siteGroupId)
            .OrderBy(x => x.PatternType)
            .ThenBy(x => x.Pattern)
            .ToListAsync(cancellationToken);

    public async Task<SiteRule> AddAsync(SiteRule rule, CancellationToken cancellationToken = default)
    {
        db.SiteRules.Add(rule);
        await db.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task UpdateAsync(SiteRule rule, CancellationToken cancellationToken = default)
    {
        var tracked = await db.SiteRules.FirstOrDefaultAsync(x => x.Id == rule.Id, cancellationToken);
        if (tracked is null)
            return;
        tracked.Pattern = rule.Pattern;
        tracked.PatternType = rule.PatternType;
        tracked.CreatedAt = rule.CreatedAt;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await db.SiteRules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            return;
        db.SiteRules.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Matching;

namespace ParentSiteGuardian.Service.Services;

public sealed class SiteGroupUrlResolver(AppDbContext db) : ISiteGroupUrlResolver
{
    public async Task<SiteGroupMatch?> ResolveAsync(string deviceId, string url, CancellationToken cancellationToken = default)
    {
        var device = await db.Devices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);
        if (device is null)
            return null;

        var groups = await db.SiteGroups.AsNoTracking()
            .Where(g => g.ChildId == device.ChildId)
            .Include(g => g.SiteRules)
            .OrderBy(g => g.Id)
            .ToListAsync(cancellationToken);

        foreach (var group in groups)
        {
            foreach (var rule in group.SiteRules.OrderBy(r => r.Id))
            {
                if (UrlMatcher.IsMatch(url, rule.Pattern, rule.PatternType))
                    return new SiteGroupMatch(group.Id, group.Name);
            }
        }

        return null;
    }
}

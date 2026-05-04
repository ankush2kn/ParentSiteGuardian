using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Repositories;

public interface ISiteRuleRepository
{
    Task<SiteRule?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SiteRule>> ListBySiteGroupIdAsync(string siteGroupId, CancellationToken cancellationToken = default);
    Task<SiteRule> AddAsync(SiteRule rule, CancellationToken cancellationToken = default);
    Task UpdateAsync(SiteRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

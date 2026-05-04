using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Repositories;

public interface ISiteGroupRepository
{
    Task<SiteGroup?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<SiteGroup?> GetByIdWithRulesAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SiteGroup>> ListByChildIdAsync(string childId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SiteGroup>> ListByChildIdWithRulesAsync(string childId, CancellationToken cancellationToken = default);
    Task<SiteGroup> AddAsync(SiteGroup group, CancellationToken cancellationToken = default);
    Task UpdateAsync(SiteGroup group, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

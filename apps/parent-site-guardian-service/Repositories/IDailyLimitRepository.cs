using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Repositories;

public interface IDailyLimitRepository
{
    Task<DailyLimit?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<DailyLimit?> GetByDeviceAndSiteGroupAsync(string deviceId, string siteGroupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailyLimit>> ListByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<DailyLimit> AddAsync(DailyLimit limit, CancellationToken cancellationToken = default);
    Task UpdateAsync(DailyLimit limit, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

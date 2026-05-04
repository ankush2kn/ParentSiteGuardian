using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Repositories;

public interface IUsageEventRepository
{
    Task<UsageEvent?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsageEvent>> ListByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<UsageEvent> AddAsync(UsageEvent usageEvent, CancellationToken cancellationToken = default);
    Task UpdateAsync(UsageEvent usageEvent, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

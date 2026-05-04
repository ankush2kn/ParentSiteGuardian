using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Repositories;

public interface IOverrideRepository
{
    Task<Override?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Override>> ListByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<Override> AddAsync(Override overrideEntity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Override overrideEntity, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

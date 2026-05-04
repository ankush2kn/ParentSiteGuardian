using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Repositories;

public interface IDeviceRepository
{
    Task<IReadOnlyList<Device>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<Device?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Device>> ListByChildIdAsync(string childId, CancellationToken cancellationToken = default);
    Task<Device> AddAsync(Device device, CancellationToken cancellationToken = default);
    Task UpdateAsync(Device device, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

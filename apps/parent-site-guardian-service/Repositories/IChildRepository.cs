using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Repositories;

public interface IChildRepository
{
    Task<Child?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Child>> ListAsync(CancellationToken cancellationToken = default);
    Task<Child> AddAsync(Child child, CancellationToken cancellationToken = default);
    Task UpdateAsync(Child child, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

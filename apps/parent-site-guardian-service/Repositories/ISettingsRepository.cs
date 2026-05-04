namespace ParentSiteGuardian.Service.Repositories;

public interface ISettingsRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}

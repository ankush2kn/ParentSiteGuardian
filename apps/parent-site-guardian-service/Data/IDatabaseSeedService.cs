namespace ParentSiteGuardian.Service.Data;

public interface IDatabaseSeedService
{
    /// <summary>Ensures <c>admin_config</c> rows exist (e.g. usage tick interval). Safe to call on every startup.</summary>
    Task EnsureAdminConfigDefaultsAsync(CancellationToken cancellationToken = default);

    /// <summary>Inserts default child, device, site groups, rules, and limits when the database has no children.</summary>
    Task SeedIfNeededAsync(CancellationToken cancellationToken = default);
}

namespace ParentSiteGuardian.Service.Services;

public interface ISiteGroupUrlResolver
{
    /// <summary>First matching site group for the device's child, or null if device missing or no rule matches.</summary>
    Task<SiteGroupMatch?> ResolveAsync(string deviceId, string url, CancellationToken cancellationToken = default);
}

namespace ParentSiteGuardian.Service.Data.Entities;

public class Override
{
    public string Id { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string SiteGroupId { get; set; } = string.Empty;
    public int ExtraSeconds { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Device Device { get; set; } = null!;
    public SiteGroup SiteGroup { get; set; } = null!;
}

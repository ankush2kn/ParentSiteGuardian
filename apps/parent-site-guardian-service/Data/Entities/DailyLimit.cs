namespace ParentSiteGuardian.Service.Data.Entities;

public class DailyLimit
{
    public string Id { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string SiteGroupId { get; set; } = string.Empty;
    public int LimitSeconds { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Device Device { get; set; } = null!;
    public SiteGroup SiteGroup { get; set; } = null!;
}

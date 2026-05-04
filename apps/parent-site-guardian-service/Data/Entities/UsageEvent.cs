namespace ParentSiteGuardian.Service.Data.Entities;

public class UsageEvent
{
    public string Id { get; set; } = string.Empty;
    public string ChildId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string SiteGroupId { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }
    public int DurationSeconds { get; set; }

    public Child Child { get; set; } = null!;
    public Device Device { get; set; } = null!;
    public SiteGroup SiteGroup { get; set; } = null!;
}

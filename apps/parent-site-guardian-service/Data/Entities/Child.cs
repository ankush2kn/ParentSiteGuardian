namespace ParentSiteGuardian.Service.Data.Entities;

public class Child
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<SiteGroup> SiteGroups { get; set; } = new List<SiteGroup>();
    public ICollection<UsageEvent> UsageEvents { get; set; } = new List<UsageEvent>();
}

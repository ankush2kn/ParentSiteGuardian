namespace ParentSiteGuardian.Service.Data.Entities;

public class SiteGroup
{
    public string Id { get; set; } = string.Empty;
    public string ChildId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Child Child { get; set; } = null!;
    public ICollection<SiteRule> SiteRules { get; set; } = new List<SiteRule>();
    public ICollection<DailyLimit> DailyLimits { get; set; } = new List<DailyLimit>();
    public ICollection<UsageEvent> UsageEvents { get; set; } = new List<UsageEvent>();
    public ICollection<Override> Overrides { get; set; } = new List<Override>();
}

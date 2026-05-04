namespace ParentSiteGuardian.Service.Data.Entities;

public class SiteRule
{
    public string Id { get; set; } = string.Empty;
    public string SiteGroupId { get; set; } = string.Empty;
    /// <summary>Host or host/path fragment (e.g. youtube.com or bing.com/videos).</summary>
    public string Pattern { get; set; } = string.Empty;
    /// <summary>domain | pathPrefix</summary>
    public string PatternType { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public SiteGroup SiteGroup { get; set; } = null!;
}

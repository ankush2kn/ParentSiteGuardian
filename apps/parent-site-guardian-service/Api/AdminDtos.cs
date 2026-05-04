namespace ParentSiteGuardian.Service.Api;

public sealed class PinSetupRequest
{
    public required string Pin { get; set; }
}

public sealed class PinVerifyRequest
{
    public required string Pin { get; set; }
}

public sealed class PinVerifyResponse
{
    public bool Valid { get; set; }
}

public sealed class PinStatusResponse
{
    public bool Configured { get; set; }
}

public sealed class AdminOverrideRequest
{
    public required string DeviceId { get; set; }
    public required string SiteGroupId { get; set; }
    public required string Pin { get; set; }
    /// <summary>5, 10, 15, or 30.</summary>
    public int ExtraMinutes { get; set; }
}

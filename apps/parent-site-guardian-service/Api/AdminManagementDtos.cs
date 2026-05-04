namespace ParentSiteGuardian.Service.Api;

public sealed class AdminBootstrapDeviceDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string ChildId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? MachineFingerprint { get; set; }
    public string? WindowsUserSid { get; set; }
}

public sealed class AdminBootstrapResponse
{
    public bool PinConfigured { get; set; }
    public required IReadOnlyList<AdminBootstrapDeviceDto> Devices { get; set; }
}

public sealed class AdminSiteRuleDto
{
    public required string Id { get; set; }
    public required string Pattern { get; set; }
    public required string PatternType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AdminSiteGroupDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Color { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public required IReadOnlyList<AdminSiteRuleDto> Rules { get; set; }
}

public sealed class AdminSiteGroupsResponse
{
    public required IReadOnlyList<AdminSiteGroupDto> Groups { get; set; }
}

public class PinProtectedDeviceRequest
{
    public required string DeviceId { get; set; }
    public required string Pin { get; set; }
}

public sealed class CreateSiteGroupRequest : PinProtectedDeviceRequest
{
    public required string Name { get; set; }
}

public sealed class UpdateSiteGroupRequest : PinProtectedDeviceRequest
{
    public required string Name { get; set; }
}

public sealed class AddSiteRuleRequest : PinProtectedDeviceRequest
{
    public required string Pattern { get; set; }
    public required string PatternType { get; set; }
}

public sealed class DeleteSiteRuleRequest : PinProtectedDeviceRequest
{
    public required string RuleId { get; set; }
}

public sealed class UpsertDailyLimitRequest : PinProtectedDeviceRequest
{
    public required string SiteGroupId { get; set; }
    public int LimitSeconds { get; set; }
}

public sealed class PinChangeRequest
{
    public required string CurrentPin { get; set; }
    public required string NewPin { get; set; }
}

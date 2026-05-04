namespace ParentSiteGuardian.Service.Api;

/// <summary>Optional NIC labels only — never used as the primary device identifier.</summary>
public sealed class OptionalNetworkLabelDto
{
    public required string InterfaceName { get; set; }
    public required string Description { get; set; }
    /// <summary>Physical (MAC-style) address for display only.</summary>
    public required string PhysicalAddress { get; set; }
}

public sealed class MachineFingerprintDto
{
    public required string MachineName { get; set; }
    public required string OsDescription { get; set; }
    public required string OsArchitecture { get; set; }
    public required string ProcessArchitecture { get; set; }
    public int ProcessorCount { get; set; }
    public required string FrameworkDescription { get; set; }
    public required string SystemDirectory { get; set; }
    public bool UserInteractive { get; set; }
    public required string LocalTimeZoneId { get; set; }
    public required string CapturedAtUtc { get; set; }
}

/// <summary>Stable identity for this guardian installation on this machine (stored in SQLite <c>settings</c>).</summary>
public sealed class LocalDeviceIdentityDto
{
    /// <summary>Stable GUID generated on first run; not derived from MAC or hardware serials.</summary>
    public required string AppDeviceId { get; set; }

    public required MachineFingerprintDto MachineFingerprint { get; set; }

    /// <summary>Windows: <c>DOMAIN\User</c> from <see cref="System.Security.Principal.WindowsIdentity"/> when available.</summary>
    public string? WindowsUsername { get; set; }

    public string? WindowsUserSid { get; set; }

    /// <summary>MAC / physical addresses for human-readable context only.</summary>
    public required IReadOnlyList<OptionalNetworkLabelDto> OptionalNetworkLabels { get; set; }
}

public sealed class EffectiveConfigResponse
{
    public required ExtensionConfigResponse Extension { get; set; }
    public required LocalDeviceIdentityDto LocalDevice { get; set; }
}

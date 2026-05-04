using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json.Serialization;

namespace ParentSiteGuardian.Service.Identity;

/// <summary>Best-effort machine context without elevation. Not used as the primary device identifier.</summary>
public sealed class MachineFingerprintPayload
{
    public string MachineName { get; set; } = string.Empty;
    public string OsDescription { get; set; } = string.Empty;
    public string OsArchitecture { get; set; } = string.Empty;
    public string ProcessArchitecture { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public string FrameworkDescription { get; set; } = string.Empty;
    public string SystemDirectory { get; set; } = string.Empty;
    public bool UserInteractive { get; set; }
    public string LocalTimeZoneId { get; set; } = string.Empty;
    public string CapturedAtUtc { get; set; } = string.Empty;
}

/// <summary>Network MAC / physical address for display only — never the primary app device id.</summary>
public sealed class OptionalMacLabelPayload
{
    public string InterfaceName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PhysicalAddress { get; set; } = string.Empty;
}

public static class LocalMachineInfoCollector
{
    public static (MachineFingerprintPayload Fingerprint, string? WindowsUsername, string? WindowsUserSid, IReadOnlyList<OptionalMacLabelPayload> MacLabels) Collect()
    {
        var fingerprint = new MachineFingerprintPayload
        {
            CapturedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
        };

        try
        {
            fingerprint.MachineName = Environment.MachineName;
        }
        catch
        {
            fingerprint.MachineName = string.Empty;
        }

        try
        {
            fingerprint.OsDescription = RuntimeInformation.OSDescription;
            fingerprint.OsArchitecture = RuntimeInformation.OSArchitecture.ToString();
            fingerprint.ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString();
            fingerprint.FrameworkDescription = RuntimeInformation.FrameworkDescription;
        }
        catch
        {
            // leave defaults
        }

        try
        {
            fingerprint.ProcessorCount = Environment.ProcessorCount;
            fingerprint.UserInteractive = Environment.UserInteractive;
            fingerprint.SystemDirectory = Environment.SystemDirectory;
        }
        catch
        {
            // leave defaults
        }

        try
        {
            fingerprint.LocalTimeZoneId = TimeZoneInfo.Local.Id;
        }
        catch
        {
            fingerprint.LocalTimeZoneId = string.Empty;
        }

        string? windowsUsername = null;
        string? windowsUserSid = null;
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                windowsUserSid = identity.User?.Value;
                windowsUsername = identity.Name;
            }
            catch
            {
                try
                {
                    windowsUsername = $"{Environment.UserDomainName}\\{Environment.UserName}";
                }
                catch
                {
                    windowsUsername = Environment.UserName;
                }
            }
        }
        else
        {
            try
            {
                windowsUsername = Environment.UserName;
            }
            catch
            {
                windowsUsername = null;
            }
        }

        var macLabels = CollectOptionalMacLabels();
        return (fingerprint, windowsUsername, windowsUserSid, macLabels);
    }

    private static IReadOnlyList<OptionalMacLabelPayload> CollectOptionalMacLabels()
    {
        var list = new List<OptionalMacLabelPayload>();
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;
                var addr = ni.GetPhysicalAddress();
                var bytes = addr.GetAddressBytes();
                if (bytes.Length == 0 || bytes.All(b => b == 0))
                    continue;
                var hex = BitConverter.ToString(bytes).Replace("-", ":", StringComparison.Ordinal);
                list.Add(new OptionalMacLabelPayload
                {
                    InterfaceName = ni.Name,
                    Description = ni.Description,
                    PhysicalAddress = hex,
                });
            }
        }
        catch
        {
            // ignore
        }

        return list;
    }
}

using System.Text.Json;
using ParentSiteGuardian.Service.Api;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Identity;
using ParentSiteGuardian.Service.Repositories;

namespace ParentSiteGuardian.Service.Services;

public sealed class LocalDeviceIdentityService(ISettingsRepository settings) : ILocalDeviceIdentityService
{
    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public async Task<LocalDeviceIdentityDto> EnsurePersistAndGetAsync(CancellationToken cancellationToken = default)
    {
        var appId = await settings.GetValueAsync(LocalDeviceIdentitySettingKeys.LocalAppDeviceId, cancellationToken);
        if (string.IsNullOrWhiteSpace(appId))
        {
            appId = Guid.NewGuid().ToString("D");
            await settings.SetAsync(LocalDeviceIdentitySettingKeys.LocalAppDeviceId, appId, cancellationToken);
        }

        var (fp, winUser, winSid, macLabels) = LocalMachineInfoCollector.Collect();

        await settings.SetAsync(
            LocalDeviceIdentitySettingKeys.LocalMachineFingerprintJson,
            JsonSerializer.Serialize(fp, JsonWriteOptions),
            cancellationToken);

        await settings.SetAsync(
            LocalDeviceIdentitySettingKeys.LocalWindowsUsername,
            winUser ?? string.Empty,
            cancellationToken);

        await settings.SetAsync(
            LocalDeviceIdentitySettingKeys.LocalWindowsUserSid,
            winSid ?? string.Empty,
            cancellationToken);

        await settings.SetAsync(
            LocalDeviceIdentitySettingKeys.LocalOptionalMacLabelsJson,
            JsonSerializer.Serialize(macLabels, JsonWriteOptions),
            cancellationToken);

        return new LocalDeviceIdentityDto
        {
            AppDeviceId = appId,
            MachineFingerprint = new MachineFingerprintDto
            {
                MachineName = fp.MachineName,
                OsDescription = fp.OsDescription,
                OsArchitecture = fp.OsArchitecture,
                ProcessArchitecture = fp.ProcessArchitecture,
                ProcessorCount = fp.ProcessorCount,
                FrameworkDescription = fp.FrameworkDescription,
                SystemDirectory = fp.SystemDirectory,
                UserInteractive = fp.UserInteractive,
                LocalTimeZoneId = fp.LocalTimeZoneId,
                CapturedAtUtc = fp.CapturedAtUtc,
            },
            WindowsUsername = string.IsNullOrWhiteSpace(winUser) ? null : winUser,
            WindowsUserSid = string.IsNullOrWhiteSpace(winSid) ? null : winSid,
            OptionalNetworkLabels = macLabels
                .Select(m => new OptionalNetworkLabelDto
                {
                    InterfaceName = m.InterfaceName,
                    Description = m.Description,
                    PhysicalAddress = m.PhysicalAddress,
                })
                .ToList(),
        };
    }
}

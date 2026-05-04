namespace ParentSiteGuardian.Service.Data;

/// <summary>Keys in the <c>settings</c> table for this installation&apos;s local machine identity.</summary>
public static class LocalDeviceIdentitySettingKeys
{
    public const string LocalAppDeviceId = "local_app_device_id";
    public const string LocalMachineFingerprintJson = "local_machine_fingerprint_json";
    public const string LocalWindowsUsername = "local_windows_username";
    public const string LocalWindowsUserSid = "local_windows_user_sid";
    public const string LocalOptionalMacLabelsJson = "local_optional_mac_labels_json";
}

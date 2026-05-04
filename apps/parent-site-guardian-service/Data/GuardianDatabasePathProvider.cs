namespace ParentSiteGuardian.Service.Data;

/// <summary>
/// Resolves the SQLite file path. Default: ProgramData\ParentSiteGuardian\parent-site-guardian.db (see implementation plan).
/// Override with configuration key GuardianDatabase:Path (full path to .db file).
/// </summary>
public sealed class GuardianDatabasePathProvider(IConfiguration configuration) : IGuardianDatabasePathProvider
{
    public string DatabasePath => ResolvePath(configuration);

    private static string ResolvePath(IConfiguration configuration)
    {
        var configured = configuration["GuardianDatabase:Path"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim();

        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ParentSiteGuardian");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "parent-site-guardian.db");
    }
}

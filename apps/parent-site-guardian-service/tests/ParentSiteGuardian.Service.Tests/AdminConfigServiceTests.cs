using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Data.Entities;
using ParentSiteGuardian.Service.Services;
using ParentSiteGuardian.Service.Tests.Support;

namespace ParentSiteGuardian.Service.Tests;

public class AdminConfigServiceTests
{
    [Fact]
    public async Task Extension_config_returns_default_when_row_missing()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            var sut = new AdminConfigService(db);
            var cfg = await sut.GetExtensionConfigAsync();
            Assert.Equal(10, cfg.UsageTickIntervalSeconds);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task Extension_config_reads_stored_interval()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            db.AdminConfigs.Add(new AdminConfig
            {
                Key = AdminConfigKeys.UsageTickIntervalSeconds,
                Value = "30",
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
            var sut = new AdminConfigService(db);
            var cfg = await sut.GetExtensionConfigAsync();
            Assert.Equal(30, cfg.UsageTickIntervalSeconds);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }
}

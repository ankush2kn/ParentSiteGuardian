using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Tests.Support;

namespace ParentSiteGuardian.Service.Tests;

public class FreshDatabaseBootstrapTests
{
    [Fact]
    public async Task Empty_database_has_no_domain_rows_until_seed_runs()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            Assert.Equal(0, await db.Children.CountAsync());
            Assert.Equal(0, await db.Devices.CountAsync());
            Assert.Equal(0, await db.SiteGroups.CountAsync());

            await new DatabaseSeedService(db).SeedIfNeededAsync();

            Assert.Equal(1, await db.Children.CountAsync());
            Assert.Equal(1, await db.Devices.CountAsync());
            Assert.Equal(2, await db.SiteGroups.CountAsync());
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }
}

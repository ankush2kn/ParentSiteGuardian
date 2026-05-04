using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Tests.Support;

namespace ParentSiteGuardian.Service.Tests;

public class DatabaseSeedServiceTests
{
    [Fact]
    public async Task SeedIfNeededAsync_inserts_default_child_device_groups_rules_and_limits()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            var sut = new DatabaseSeedService(db);
            await sut.SeedIfNeededAsync();

            Assert.Equal(1, await db.Children.CountAsync());
            Assert.Equal(1, await db.Devices.CountAsync());
            Assert.Equal(2, await db.SiteGroups.CountAsync());
            Assert.Equal(4, await db.SiteRules.CountAsync());
            Assert.Equal(2, await db.DailyLimits.CountAsync());

            var child = await db.Children.AsNoTracking().SingleAsync();
            Assert.Equal(WellKnownSeedIds.DefaultChildId, child.Id);
            Assert.Equal("Default Child", child.Name);

            var device = await db.Devices.AsNoTracking().SingleAsync();
            Assert.Equal("This Windows laptop", device.Name);
            Assert.Equal(child.Id, device.ChildId);

            var youtube = await db.SiteGroups.AsNoTracking().SingleAsync(g => g.Name == "YouTube");
            var rules = await db.SiteRules.AsNoTracking().Where(r => r.SiteGroupId == youtube.Id).ToListAsync();
            Assert.Equal(3, rules.Count);
            Assert.Contains(rules, r => r.PatternType == "domain" && r.Pattern == "youtube.com");
            Assert.Contains(rules, r => r.PatternType == "domain" && r.Pattern == "youtu.be");
            Assert.Contains(rules, r => r.PatternType == "domain" && r.Pattern == "youtube-nocookie.com");

            var videoSearch = await db.SiteGroups.AsNoTracking().SingleAsync(g => g.Name == "Video Search");
            var vsRules = await db.SiteRules.AsNoTracking().Where(r => r.SiteGroupId == videoSearch.Id).ToListAsync();
            Assert.Single(vsRules);
            Assert.Equal("pathPrefix", vsRules[0].PatternType);
            Assert.Equal("bing.com/videos", vsRules[0].Pattern);

            var limits = await db.DailyLimits.AsNoTracking().Where(l => l.DeviceId == device.Id).ToListAsync();
            Assert.Equal(2700, limits.Single(l => l.SiteGroupId == youtube.Id).LimitSeconds);
            Assert.Equal(900, limits.Single(l => l.SiteGroupId == videoSearch.Id).LimitSeconds);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task SeedIfNeededAsync_is_idempotent()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            var sut = new DatabaseSeedService(db);
            await sut.SeedIfNeededAsync();
            await sut.SeedIfNeededAsync();
            await sut.SeedIfNeededAsync();

            Assert.Equal(1, await db.Children.CountAsync());
            Assert.Equal(4, await db.SiteRules.CountAsync());
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }
}

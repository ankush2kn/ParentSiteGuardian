using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Data.Entities;
using ParentSiteGuardian.Service.Repositories;
using ParentSiteGuardian.Service.Tests.Support;

namespace ParentSiteGuardian.Service.Tests;

public class RepositoryCrudTests
{
    [Fact]
    public async Task ChildRepository_add_get_update_delete_list()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            var sut = new ChildRepository(db);
            var now = DateTimeOffset.UtcNow;

            var added = await sut.AddAsync(new Child
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Alex",
                CreatedAt = now,
            });

            var fetched = await sut.GetByIdAsync(added.Id);
            Assert.NotNull(fetched);
            Assert.Equal("Alex", fetched!.Name);

            fetched.Name = "Alexandra";
            await sut.UpdateAsync(fetched);

            var afterUpdate = await db.Children.AsNoTracking().SingleAsync(c => c.Id == added.Id);
            Assert.Equal("Alexandra", afterUpdate.Name);

            var list = await sut.ListAsync();
            Assert.Single(list);

            await sut.DeleteAsync(added.Id);
            Assert.Empty(await sut.ListAsync());
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task SiteGroupRepository_and_SiteRuleRepository_roundtrip_with_seed()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await new DatabaseSeedService(db).SeedIfNeededAsync();

            var groups = new SiteGroupRepository(db);
            var youtube = await groups.GetByIdWithRulesAsync(WellKnownSeedIds.YouTubeSiteGroupId);
            Assert.NotNull(youtube);
            Assert.Equal(3, youtube!.SiteRules.Count);

            var rules = new SiteRuleRepository(db);
            var list = await rules.ListBySiteGroupIdAsync(WellKnownSeedIds.YouTubeSiteGroupId);
            Assert.Equal(3, list.Count);

            var newRule = await rules.AddAsync(new SiteRule
            {
                Id = Guid.NewGuid().ToString(),
                SiteGroupId = WellKnownSeedIds.YouTubeSiteGroupId,
                Pattern = "googlevideo.com",
                PatternType = "domain",
                CreatedAt = DateTimeOffset.UtcNow,
            });
            Assert.Equal(4, (await rules.ListBySiteGroupIdAsync(WellKnownSeedIds.YouTubeSiteGroupId)).Count);

            newRule.Pattern = "googlevideo.net";
            await rules.UpdateAsync(newRule);

            await rules.DeleteAsync(newRule.Id);
            Assert.Equal(3, (await rules.ListBySiteGroupIdAsync(WellKnownSeedIds.YouTubeSiteGroupId)).Count);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task DailyLimitRepository_list_update_after_seed()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await new DatabaseSeedService(db).SeedIfNeededAsync();
            var sut = new DailyLimitRepository(db);
            var list = await sut.ListByDeviceIdAsync(WellKnownSeedIds.DefaultDeviceId);
            Assert.Equal(2, list.Count);

            var youtubeLimit = list.Single(l => l.SiteGroupId == WellKnownSeedIds.YouTubeSiteGroupId);
            youtubeLimit.LimitSeconds = 60;
            await sut.UpdateAsync(youtubeLimit);

            var fetched = await sut.GetByIdAsync(youtubeLimit.Id);
            Assert.Equal(60, fetched!.LimitSeconds);

            await sut.DeleteAsync(WellKnownSeedIds.VideoSearchDailyLimitId);
            Assert.Single(await sut.ListByDeviceIdAsync(WellKnownSeedIds.DefaultDeviceId));
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task SettingsRepository_set_get_delete()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            var sut = new SettingsRepository(db);
            await sut.SetAsync("pin_hash", "abc");
            Assert.Equal("abc", await sut.GetValueAsync("pin_hash"));
            await sut.SetAsync("pin_hash", "def");
            Assert.Equal("def", await sut.GetValueAsync("pin_hash"));
            await sut.DeleteAsync("pin_hash");
            Assert.Null(await sut.GetValueAsync("pin_hash"));
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task UsageEventRepository_and_OverrideRepository_add_list_delete()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await new DatabaseSeedService(db).SeedIfNeededAsync();
            var now = DateTimeOffset.UtcNow;

            var usage = new UsageEventRepository(db);
            var evt = await usage.AddAsync(new UsageEvent
            {
                Id = Guid.NewGuid().ToString(),
                ChildId = WellKnownSeedIds.DefaultChildId,
                DeviceId = WellKnownSeedIds.DefaultDeviceId,
                SiteGroupId = WellKnownSeedIds.YouTubeSiteGroupId,
                Browser = "chrome",
                Url = "https://www.youtube.com/watch?v=test",
                StartedAt = now,
                EndedAt = now.AddSeconds(5),
                DurationSeconds = 5,
            });

            var listed = await usage.ListByDeviceIdAsync(WellKnownSeedIds.DefaultDeviceId);
            Assert.Single(listed);
            await usage.DeleteAsync(evt.Id);
            Assert.Empty(await usage.ListByDeviceIdAsync(WellKnownSeedIds.DefaultDeviceId));

            var ovRepo = new OverrideRepository(db);
            var ov = await ovRepo.AddAsync(new Override
            {
                Id = Guid.NewGuid().ToString(),
                DeviceId = WellKnownSeedIds.DefaultDeviceId,
                SiteGroupId = WellKnownSeedIds.YouTubeSiteGroupId,
                ExtraSeconds = 300,
                ExpiresAt = now.AddHours(1),
                CreatedAt = now,
            });
            Assert.Single(await ovRepo.ListByDeviceIdAsync(WellKnownSeedIds.DefaultDeviceId));
            await ovRepo.DeleteAsync(ov.Id);
            Assert.Empty(await ovRepo.ListByDeviceIdAsync(WellKnownSeedIds.DefaultDeviceId));
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task DeviceRepository_list_by_child_after_seed()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await new DatabaseSeedService(db).SeedIfNeededAsync();
            var sut = new DeviceRepository(db);
            var devices = await sut.ListByChildIdAsync(WellKnownSeedIds.DefaultChildId);
            Assert.Single(devices);
            Assert.Equal("This Windows laptop", devices[0].Name);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }
}

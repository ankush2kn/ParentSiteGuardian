using ParentSiteGuardian.Service.Api;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Services;
using ParentSiteGuardian.Service.Tests.Support;
using ParentSiteGuardian.Service.Time;

namespace ParentSiteGuardian.Service.Tests;

public class UsageAccountingVideoSearchTests
{
    private static UsageAccountingService CreateSut(AppDbContext db, IGuardianTime time) =>
        new(db, new SiteGroupUrlResolver(db), time);

    [Fact]
    public async Task Decision_matches_bing_videos_as_video_search()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await new DatabaseSeedService(db).SeedIfNeededAsync();
            var noon = new DateTimeOffset(2026, 12, 1, 12, 0, 0, TimeSpan.Zero);
            var sut = CreateSut(db, new FixedGuardianTime(noon, TimeZoneInfo.Utc));

            var d = await sut.DecideAsync(new DecisionRequest
            {
                DeviceId = WellKnownSeedIds.DefaultDeviceId,
                Url = "https://www.bing.com/videos/search?q=cats",
            });

            Assert.NotNull(d);
            Assert.True(d!.Matched);
            Assert.Equal(WellKnownSeedIds.VideoSearchSiteGroupId, d.SiteGroupId);
            Assert.Equal("Video Search", d.SiteGroupName);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task Tick_records_video_search_usage()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await new DatabaseSeedService(db).SeedIfNeededAsync();
            var noon = new DateTimeOffset(2026, 12, 2, 12, 0, 0, TimeSpan.Zero);
            var sut = CreateSut(db, new FixedGuardianTime(noon, TimeZoneInfo.Utc));

            var tick = await sut.RecordTickAsync(new UsageTickRequest
            {
                DeviceId = WellKnownSeedIds.DefaultDeviceId,
                Browser = "edge",
                Url = "https://www.bing.com/videos/browse",
                StartedAt = noon,
                EndedAt = noon.AddSeconds(12),
                ActiveDurationSeconds = 12,
            });

            Assert.True(tick.Recorded);
            Assert.Equal(WellKnownSeedIds.VideoSearchSiteGroupId, tick.SiteGroupId);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }
}

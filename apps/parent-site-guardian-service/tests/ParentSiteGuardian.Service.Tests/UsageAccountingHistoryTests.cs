using ParentSiteGuardian.Service.Api;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Services;
using ParentSiteGuardian.Service.Tests.Support;
using ParentSiteGuardian.Service.Time;

namespace ParentSiteGuardian.Service.Tests;

public class UsageAccountingHistoryTests
{
    private static UsageAccountingService CreateSut(AppDbContext db, IGuardianTime time) =>
        new(db, new SiteGroupUrlResolver(db), time);

    private static UsageTickRequest Tick(
        string url,
        int seconds,
        DateTimeOffset started,
        string deviceId = WellKnownSeedIds.DefaultDeviceId) =>
        new()
        {
            DeviceId = deviceId,
            Browser = "chrome",
            Url = url,
            StartedAt = started,
            EndedAt = started.AddSeconds(seconds),
            ActiveDurationSeconds = seconds,
        };

    [Fact]
    public async Task GetHistory_aggregates_usage_per_local_day_and_site_group()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await new DatabaseSeedService(db).SeedIfNeededAsync();

            var day1 = new DateTimeOffset(2026, 11, 5, 18, 0, 0, TimeSpan.Zero);
            var day2 = new DateTimeOffset(2026, 11, 6, 18, 0, 0, TimeSpan.Zero);

            var sutDay1 = CreateSut(db, new FixedGuardianTime(day1, TimeZoneInfo.Utc));
            await sutDay1.RecordTickAsync(Tick("https://www.youtube.com/a", 30, day1));
            await sutDay1.RecordTickAsync(Tick("https://www.bing.com/videos/foo", 10, day1));

            var sutDay2 = CreateSut(db, new FixedGuardianTime(day2, TimeZoneInfo.Utc));
            await sutDay2.RecordTickAsync(Tick("https://www.youtube.com/b", 5, day2));

            var histSut = CreateSut(db, new FixedGuardianTime(day2, TimeZoneInfo.Utc));
            var hist = await histSut.GetHistoryAsync(WellKnownSeedIds.DefaultDeviceId, 30);

            Assert.NotNull(hist);
            Assert.Equal(WellKnownSeedIds.DefaultDeviceId, hist!.DeviceId);
            Assert.Contains(hist.Days, d => d.LocalDate == "2026-11-05");
            Assert.Contains(hist.Days, d => d.LocalDate == "2026-11-06");

            var day1Row = hist.Days.First(d => d.LocalDate == "2026-11-05");
            var yt1 = day1Row.Groups.Single(g => g.SiteGroupId == WellKnownSeedIds.YouTubeSiteGroupId);
            var vs1 = day1Row.Groups.Single(g => g.SiteGroupId == WellKnownSeedIds.VideoSearchSiteGroupId);
            Assert.Equal(30, yt1.UsedSeconds);
            Assert.Equal(10, vs1.UsedSeconds);

            var day2Row = hist.Days.Single(d => d.LocalDate == "2026-11-06");
            Assert.Equal(5, day2Row.Groups.Single(g => g.SiteGroupId == WellKnownSeedIds.YouTubeSiteGroupId).UsedSeconds);
            Assert.Equal(0, day2Row.Groups.Single(g => g.SiteGroupId == WellKnownSeedIds.VideoSearchSiteGroupId).UsedSeconds);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }
}

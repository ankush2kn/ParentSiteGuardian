using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Api;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Data.Entities;
using ParentSiteGuardian.Service.Services;
using ParentSiteGuardian.Service.Tests.Support;
using ParentSiteGuardian.Service.Time;

namespace ParentSiteGuardian.Service.Tests;

public class UsageAccountingServiceTests
{
    private static UsageAccountingService CreateSut(AppDbContext db, IGuardianTime time) =>
        new(db, new SiteGroupUrlResolver(db), time);

    private static UsageTickRequest Tick(string url, int seconds, DateTimeOffset started, string? deviceId = null) =>
        new()
        {
            DeviceId = deviceId ?? WellKnownSeedIds.DefaultDeviceId,
            Browser = "chrome",
            Url = url,
            StartedAt = started,
            EndedAt = started.AddSeconds(seconds),
            ActiveDurationSeconds = seconds,
        };

    [Fact]
    public async Task Tick_and_decision_ignore_unmatched_urls()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await new DatabaseSeedService(db).SeedIfNeededAsync();
            var noonUtc = new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero);
            var time = new FixedGuardianTime(noonUtc, TimeZoneInfo.Utc);
            var sut = CreateSut(db, time);

            var tick = await sut.RecordTickAsync(Tick("https://www.bing.com/search?q=cats", 5, noonUtc));
            Assert.False(tick.Recorded);
            Assert.Equal("unmatched", tick.Reason);

            var decision = await sut.DecideAsync(new DecisionRequest
            {
                DeviceId = WellKnownSeedIds.DefaultDeviceId,
                Url = "https://www.bing.com/search?q=cats",
            });
            Assert.NotNull(decision);
            Assert.False(decision!.Matched);
            Assert.True(decision.Allowed);
            Assert.Equal("unmatched", decision.Reason);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task Decision_blocks_when_daily_limit_exhausted()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await new DatabaseSeedService(db).SeedIfNeededAsync();
            var limit = await db.DailyLimits.FirstAsync(l => l.SiteGroupId == WellKnownSeedIds.YouTubeSiteGroupId);
            limit.LimitSeconds = 100;
            await db.SaveChangesAsync();

            var noonUtc = new DateTimeOffset(2026, 6, 1, 15, 0, 0, TimeSpan.Zero);
            var time = new FixedGuardianTime(noonUtc, TimeZoneInfo.Utc);
            var sut = CreateSut(db, time);

            var tickResult = await sut.RecordTickAsync(Tick("https://www.youtube.com/watch?v=1", 100, noonUtc));
            Assert.True(tickResult.Recorded);

            var decision = await sut.DecideAsync(new DecisionRequest
            {
                DeviceId = WellKnownSeedIds.DefaultDeviceId,
                Url = "https://www.youtube.com/watch?v=2",
            });
            Assert.NotNull(decision);
            Assert.True(decision!.Matched);
            Assert.False(decision.Allowed);
            Assert.Equal(100, decision.UsedSeconds);
            Assert.Equal(100, decision.LimitSeconds);
            Assert.Equal(0, decision.RemainingSeconds);
            Assert.Equal("daily_limit_exhausted", decision.Reason);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task Usage_resets_on_next_local_calendar_day()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await new DatabaseSeedService(db).SeedIfNeededAsync();
            var limit = await db.DailyLimits.FirstAsync(l => l.SiteGroupId == WellKnownSeedIds.YouTubeSiteGroupId);
            limit.LimitSeconds = 200;
            await db.SaveChangesAsync();

            var day1 = new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);
            var day2 = new DateTimeOffset(2026, 7, 11, 12, 0, 0, TimeSpan.Zero);

            var sutDay1 = CreateSut(db, new FixedGuardianTime(day1, TimeZoneInfo.Utc));
            await sutDay1.RecordTickAsync(Tick("https://www.youtube.com/a", 150, day1));

            var sutDay2 = CreateSut(db, new FixedGuardianTime(day2, TimeZoneInfo.Utc));
            var decision = await sutDay2.DecideAsync(new DecisionRequest
            {
                DeviceId = WellKnownSeedIds.DefaultDeviceId,
                Url = "https://www.youtube.com/b",
            });

            Assert.NotNull(decision);
            Assert.True(decision!.Matched);
            Assert.True(decision.Allowed);
            Assert.Equal(0, decision.UsedSeconds);
            Assert.Equal(200, decision.LimitSeconds);
            Assert.Equal(200, decision.RemainingSeconds);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task Non_expired_override_increases_effective_limit_and_allows_browsing()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await new DatabaseSeedService(db).SeedIfNeededAsync();
            var limit = await db.DailyLimits.FirstAsync(l => l.SiteGroupId == WellKnownSeedIds.YouTubeSiteGroupId);
            limit.LimitSeconds = 100;
            await db.SaveChangesAsync();

            var noonUtc = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
            var time = new FixedGuardianTime(noonUtc, TimeZoneInfo.Utc);
            var sut = CreateSut(db, time);

            await sut.RecordTickAsync(Tick("https://www.youtube.com/x", 100, noonUtc));

            db.Overrides.Add(new Override
            {
                Id = Guid.NewGuid().ToString(),
                DeviceId = WellKnownSeedIds.DefaultDeviceId,
                SiteGroupId = WellKnownSeedIds.YouTubeSiteGroupId,
                ExtraSeconds = 80,
                ExpiresAt = noonUtc.AddHours(2),
                CreatedAt = noonUtc,
            });
            await db.SaveChangesAsync();

            var decision = await sut.DecideAsync(new DecisionRequest
            {
                DeviceId = WellKnownSeedIds.DefaultDeviceId,
                Url = "https://www.youtube.com/y",
            });

            Assert.NotNull(decision);
            Assert.True(decision!.Matched);
            Assert.True(decision.Allowed);
            Assert.Equal(100, decision.UsedSeconds);
            Assert.Equal(180, decision.LimitSeconds);
            Assert.Equal(80, decision.RemainingSeconds);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task GetToday_includes_per_group_usage_and_override_bonus()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await new DatabaseSeedService(db).SeedIfNeededAsync();
            var youtubeLimit = await db.DailyLimits.FirstAsync(l => l.SiteGroupId == WellKnownSeedIds.YouTubeSiteGroupId);
            youtubeLimit.LimitSeconds = 200;
            await db.SaveChangesAsync();

            var noonUtc = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
            var time = new FixedGuardianTime(noonUtc, TimeZoneInfo.Utc);
            var sut = CreateSut(db, time);

            await sut.RecordTickAsync(Tick("https://www.youtube.com/z", 50, noonUtc));
            db.Overrides.Add(new Override
            {
                Id = Guid.NewGuid().ToString(),
                DeviceId = WellKnownSeedIds.DefaultDeviceId,
                SiteGroupId = WellKnownSeedIds.YouTubeSiteGroupId,
                ExtraSeconds = 25,
                ExpiresAt = noonUtc.AddHours(1),
                CreatedAt = noonUtc,
            });
            await db.SaveChangesAsync();

            var today = await sut.GetTodayAsync(WellKnownSeedIds.DefaultDeviceId);
            Assert.NotNull(today);
            Assert.Equal("2026-09-01", today!.LocalDate);
            var youtube = today.Groups.Single(g => g.SiteGroupId == WellKnownSeedIds.YouTubeSiteGroupId);
            Assert.Equal(50, youtube.UsedSeconds);
            Assert.Equal(200, youtube.BaseLimitSeconds);
            Assert.Equal(25, youtube.OverrideBonusSeconds);
            Assert.Equal(225, youtube.EffectiveLimitSeconds);
            Assert.Equal(175, youtube.RemainingSeconds);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task Tick_unknown_device_returns_not_recorded()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await new DatabaseSeedService(db).SeedIfNeededAsync();
            var sut = CreateSut(db, new FixedGuardianTime(DateTimeOffset.UtcNow, TimeZoneInfo.Utc));
            var r = await sut.RecordTickAsync(Tick("https://www.youtube.com/", 1, DateTimeOffset.UtcNow, "missing-device"));
            Assert.False(r.Recorded);
            Assert.Equal("unknown_device", r.Reason);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task Decide_unknown_device_returns_null()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await new DatabaseSeedService(db).SeedIfNeededAsync();
            var sut = CreateSut(db, new FixedGuardianTime(DateTimeOffset.UtcNow, TimeZoneInfo.Utc));
            var d = await sut.DecideAsync(new DecisionRequest { DeviceId = "nope", Url = "https://www.youtube.com/" });
            Assert.Null(d);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }
}

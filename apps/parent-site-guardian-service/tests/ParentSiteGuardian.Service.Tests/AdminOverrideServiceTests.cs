using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Services;
using ParentSiteGuardian.Service.Tests.Support;
using ParentSiteGuardian.Service.Time;

namespace ParentSiteGuardian.Service.Tests;

public class AdminOverrideServiceTests
{
    [Fact]
    public async Task Create_override_requires_pin_and_valid_minutes()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await new DatabaseSeedService(db).SeedIfNeededAsync();
            var pins = new ParentPinService(db, new FixedGuardianTime(DateTimeOffset.UtcNow, TimeZoneInfo.Utc));
            await pins.SetupAsync("88888888");

            var time = new FixedGuardianTime(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero), TimeZoneInfo.Utc);
            var sut = new AdminOverrideService(db, pins, time);

            Assert.Equal(
                AdminOverrideCreateResult.InvalidPin,
                await sut.CreateAsync(WellKnownSeedIds.DefaultDeviceId, WellKnownSeedIds.YouTubeSiteGroupId, "nope", 10));

            Assert.Equal(
                AdminOverrideCreateResult.InvalidExtraMinutes,
                await sut.CreateAsync(WellKnownSeedIds.DefaultDeviceId, WellKnownSeedIds.YouTubeSiteGroupId, "88888888", 7));

            Assert.Equal(
                AdminOverrideCreateResult.Ok,
                await sut.CreateAsync(WellKnownSeedIds.DefaultDeviceId, WellKnownSeedIds.YouTubeSiteGroupId, "88888888", 10));

            var overrides = await db.Overrides.ToListAsync();
            Assert.Single(overrides);
            Assert.Equal(600, overrides[0].ExtraSeconds);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task Create_override_fails_when_pin_not_configured()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await new DatabaseSeedService(db).SeedIfNeededAsync();
            var pins = new ParentPinService(db, new FixedGuardianTime(DateTimeOffset.UtcNow, TimeZoneInfo.Utc));
            var time = new FixedGuardianTime(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);
            var sut = new AdminOverrideService(db, pins, time);

            Assert.Equal(
                AdminOverrideCreateResult.PinNotConfigured,
                await sut.CreateAsync(WellKnownSeedIds.DefaultDeviceId, WellKnownSeedIds.YouTubeSiteGroupId, "any", 5));
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }
}

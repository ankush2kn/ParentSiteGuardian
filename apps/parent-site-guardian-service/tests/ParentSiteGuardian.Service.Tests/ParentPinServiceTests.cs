using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Services;
using ParentSiteGuardian.Service.Tests.Support;
using ParentSiteGuardian.Service.Time;

namespace ParentSiteGuardian.Service.Tests;

public class ParentPinServiceTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    /// <summary>A fresh service per call simulates a later request (state lives only in the database).</summary>
    private static ParentPinService At(AppDbContext db, DateTimeOffset now) =>
        new(db, new FixedGuardianTime(now, TimeZoneInfo.Utc));

    [Fact]
    public async Task Setup_then_check_roundtrip()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            var sut = At(db, T0);
            Assert.False(await sut.IsConfiguredAsync());
            Assert.Equal(PinCheckStatus.NotConfigured, (await sut.CheckAsync("424242")).Status);
            Assert.Equal(ParentPinSetupResult.Ok, await sut.SetupAsync("424242"));
            Assert.True(await sut.IsConfiguredAsync());
            Assert.True((await sut.CheckAsync("424242")).IsOk);
            Assert.Equal(PinCheckStatus.Invalid, (await sut.CheckAsync("wrong")).Status);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task Setup_twice_returns_already_configured()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            var sut = At(db, T0);
            Assert.Equal(ParentPinSetupResult.Ok, await sut.SetupAsync("123456"));
            Assert.Equal(ParentPinSetupResult.AlreadyConfigured, await sut.SetupAsync("999999"));
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task Setup_short_pin_rejected()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            Assert.Equal(ParentPinSetupResult.InvalidPin, await At(db, T0).SetupAsync("123"));
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task Change_pin_requires_current_and_updates_check()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            var sut = At(db, T0);
            Assert.Equal(ParentPinSetupResult.Ok, await sut.SetupAsync("111111"));
            Assert.Equal(ParentPinChangeResult.InvalidCurrentPin, await sut.ChangeAsync("wrong", "222222"));
            Assert.Equal(ParentPinChangeResult.InvalidNewPin, await sut.ChangeAsync("111111", "22"));
            Assert.Equal(ParentPinChangeResult.Ok, await sut.ChangeAsync("111111", "222222"));
            Assert.Equal(PinCheckStatus.Invalid, (await sut.CheckAsync("111111")).Status);
            Assert.True((await sut.CheckAsync("222222")).IsOk);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 1)]
    [InlineData(4, 3)]
    [InlineData(5, 9)]
    [InlineData(6, 27)]
    [InlineData(7, 81)]
    [InlineData(9, 729)]
    [InlineData(10, 24 * 60)]
    [InlineData(500, 24 * 60)]
    public void Lockout_schedule_triples_after_three_free_attempts_and_caps_at_24h(int failures, int expectedMinutes)
    {
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), ParentPinService.LockoutAfterFailures(failures));
    }

    [Fact]
    public async Task Wrong_pins_escalate_lockout_and_correct_pin_resets()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await At(db, T0).SetupAsync("246810");

            // Attempts 1–2: wrong, no wait.
            Assert.Equal(new PinCheckResult(PinCheckStatus.Invalid, TimeSpan.Zero), await At(db, T0).CheckAsync("0000"));
            Assert.Equal(new PinCheckResult(PinCheckStatus.Invalid, TimeSpan.Zero), await At(db, T0).CheckAsync("0000"));
            // Attempt 3: wrong, and the 4th try must wait 1 minute.
            Assert.Equal(new PinCheckResult(PinCheckStatus.Invalid, TimeSpan.FromMinutes(1)), await At(db, T0).CheckAsync("0000"));

            // During the wait even the correct PIN is refused, and the attempt is not counted.
            var locked = await At(db, T0.AddSeconds(30)).CheckAsync("246810");
            Assert.Equal(PinCheckStatus.LockedOut, locked.Status);
            Assert.Equal(TimeSpan.FromSeconds(30), locked.RetryAfter);
            Assert.Equal(TimeSpan.FromSeconds(30), await At(db, T0.AddSeconds(30)).GetLockoutRemainingAsync());

            // Attempt 4 after 1 minute: wrong → 3 minute wait.
            var t4 = T0.AddMinutes(1);
            Assert.Equal(new PinCheckResult(PinCheckStatus.Invalid, TimeSpan.FromMinutes(3)), await At(db, t4).CheckAsync("0000"));

            // Attempt 5 after 3 more minutes: wrong → 9 minute wait.
            var t5 = t4.AddMinutes(3);
            Assert.Equal(PinCheckStatus.LockedOut, (await At(db, t5.AddSeconds(-1)).CheckAsync("0000")).Status);
            Assert.Equal(new PinCheckResult(PinCheckStatus.Invalid, TimeSpan.FromMinutes(9)), await At(db, t5).CheckAsync("0000"));

            // Correct PIN after the wait succeeds and clears the counter.
            var t6 = t5.AddMinutes(9);
            Assert.True((await At(db, t6).CheckAsync("246810")).IsOk);
            Assert.Equal(TimeSpan.Zero, await At(db, t6).GetLockoutRemainingAsync());
            Assert.Equal(new PinCheckResult(PinCheckStatus.Invalid, TimeSpan.Zero), await At(db, t6).CheckAsync("0000"));
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task Change_pin_is_blocked_while_locked_out()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await At(db, T0).SetupAsync("135790");
            for (var i = 0; i < ParentPinService.FreeAttempts; i++)
                await At(db, T0).ChangeAsync("bad!", "99999999");

            Assert.Equal(ParentPinChangeResult.LockedOut, await At(db, T0).ChangeAsync("135790", "99999999"));
            Assert.Equal(ParentPinChangeResult.Ok, await At(db, T0.AddMinutes(1)).ChangeAsync("135790", "99999999"));
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task Parallel_guesses_cannot_skip_the_lockout()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        var extra = new List<AppDbContext>();
        try
        {
            await At(db, T0).SetupAsync("112233");

            const int parallel = 8;
            for (var i = 0; i < parallel; i++)
            {
                extra.Add(new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite($"Data Source={path}")
                    .Options));
            }

            var results = await Task.WhenAll(extra.Select(ctx => At(ctx, T0).CheckAsync("wrong-guess")));

            Assert.Equal(ParentPinService.FreeAttempts, results.Count(r => r.Status == PinCheckStatus.Invalid));
            Assert.Equal(parallel - ParentPinService.FreeAttempts, results.Count(r => r.Status == PinCheckStatus.LockedOut));
        }
        finally
        {
            foreach (var ctx in extra)
                ctx.Dispose();
            TestAppDbContextFactory.Destroy(db, path);
        }
    }
}

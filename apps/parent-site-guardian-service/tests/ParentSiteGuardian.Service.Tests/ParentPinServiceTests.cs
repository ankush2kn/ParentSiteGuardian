using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Services;
using ParentSiteGuardian.Service.Tests.Support;

namespace ParentSiteGuardian.Service.Tests;

public class ParentPinServiceTests
{
    [Fact]
    public async Task Setup_then_verify_roundtrip()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            var sut = new ParentPinService(db);
            Assert.False(await sut.IsConfiguredAsync());
            Assert.Equal(ParentPinSetupResult.Ok, await sut.SetupAsync("424242"));
            Assert.True(await sut.IsConfiguredAsync());
            Assert.True(await sut.VerifyAsync("424242"));
            Assert.False(await sut.VerifyAsync("wrong"));
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
            var sut = new ParentPinService(db);
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
            var sut = new ParentPinService(db);
            Assert.Equal(ParentPinSetupResult.InvalidPin, await sut.SetupAsync("123"));
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }

    [Fact]
    public async Task Change_pin_requires_current_and_updates_verify()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            var sut = new ParentPinService(db);
            Assert.Equal(ParentPinSetupResult.Ok, await sut.SetupAsync("111111"));
            Assert.Equal(ParentPinChangeResult.InvalidCurrentPin, await sut.ChangeAsync("wrong", "222222"));
            Assert.Equal(ParentPinChangeResult.InvalidNewPin, await sut.ChangeAsync("111111", "22"));
            Assert.Equal(ParentPinChangeResult.Ok, await sut.ChangeAsync("111111", "222222"));
            Assert.False(await sut.VerifyAsync("111111"));
            Assert.True(await sut.VerifyAsync("222222"));
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }
}

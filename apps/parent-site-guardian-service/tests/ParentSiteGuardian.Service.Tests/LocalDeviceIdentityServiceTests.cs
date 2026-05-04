using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Repositories;
using ParentSiteGuardian.Service.Services;
using ParentSiteGuardian.Service.Tests.Support;

namespace ParentSiteGuardian.Service.Tests;

public class LocalDeviceIdentityServiceTests
{
    [Fact]
    public async Task EnsurePersistAndGetAsync_creates_stable_app_id_and_settings()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            var settings = new SettingsRepository(db);
            var sut = new LocalDeviceIdentityService(settings);

            var first = await sut.EnsurePersistAndGetAsync();
            Assert.False(string.IsNullOrWhiteSpace(first.AppDeviceId));
            Assert.NotNull(first.MachineFingerprint);
            Assert.NotNull(first.OptionalNetworkLabels);

            var storedId = await settings.GetValueAsync(LocalDeviceIdentitySettingKeys.LocalAppDeviceId);
            Assert.Equal(first.AppDeviceId, storedId);

            var second = await sut.EnsurePersistAndGetAsync();
            Assert.Equal(first.AppDeviceId, second.AppDeviceId);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }
}

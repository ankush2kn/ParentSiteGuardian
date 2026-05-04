using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Repositories;
using ParentSiteGuardian.Service.Services;
using ParentSiteGuardian.Service.Time;

namespace ParentSiteGuardian.Service.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddGuardianApplication(this IServiceCollection services)
    {
        services.AddSingleton<IGuardianTime, SystemGuardianTime>();
        services.AddScoped<ISiteGroupUrlResolver, SiteGroupUrlResolver>();
        services.AddScoped<IUsageAccountingService, UsageAccountingService>();
        services.AddScoped<IAdminConfigService, AdminConfigService>();
        services.AddScoped<IParentPinService, ParentPinService>();
        services.AddScoped<IAdminOverrideService, AdminOverrideService>();
        services.AddScoped<ILocalDeviceIdentityService, LocalDeviceIdentityService>();
        return services;
    }

    public static IServiceCollection AddGuardianPersistence(this IServiceCollection services)
    {
        services.AddSingleton<IGuardianDatabasePathProvider, GuardianDatabasePathProvider>();
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var path = sp.GetRequiredService<IGuardianDatabasePathProvider>().DatabasePath;
            options.UseSqlite($"Data Source={path}");
        });
        services.AddScoped<IDatabaseSeedService, DatabaseSeedService>();
        services.AddScoped<IChildRepository, ChildRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<ISiteGroupRepository, SiteGroupRepository>();
        services.AddScoped<ISiteRuleRepository, SiteRuleRepository>();
        services.AddScoped<IDailyLimitRepository, DailyLimitRepository>();
        services.AddScoped<IUsageEventRepository, UsageEventRepository>();
        services.AddScoped<IOverrideRepository, OverrideRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        return services;
    }
}

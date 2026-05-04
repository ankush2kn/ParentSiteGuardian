using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Data;

public sealed class DatabaseSeedService(AppDbContext db) : IDatabaseSeedService
{
    public async Task EnsureAdminConfigDefaultsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureAdminKeyAsync(AdminConfigKeys.UsageTickIntervalSeconds, "10", cancellationToken);
    }

    public async Task SeedIfNeededAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Children.AnyAsync(cancellationToken))
            return;

        var now = DateTimeOffset.UtcNow;

        var child = new Child
        {
            Id = WellKnownSeedIds.DefaultChildId,
            Name = "Default Child",
            CreatedAt = now,
        };

        var device = new Device
        {
            Id = WellKnownSeedIds.DefaultDeviceId,
            ChildId = child.Id,
            Name = "This Windows laptop",
            MachineFingerprint = null,
            WindowsUserSid = null,
            CreatedAt = now,
        };

        var youtube = new SiteGroup
        {
            Id = WellKnownSeedIds.YouTubeSiteGroupId,
            ChildId = child.Id,
            Name = "YouTube",
            Color = null,
            CreatedAt = now,
        };

        var videoSearch = new SiteGroup
        {
            Id = WellKnownSeedIds.VideoSearchSiteGroupId,
            ChildId = child.Id,
            Name = "Video Search",
            Color = null,
            CreatedAt = now,
        };

        var youtubeRules = new[]
        {
            NewRule(youtube.Id, "youtube.com", "domain", now),
            NewRule(youtube.Id, "youtu.be", "domain", now),
            NewRule(youtube.Id, "youtube-nocookie.com", "domain", now),
        };

        var videoSearchRules = new[]
        {
            NewRule(videoSearch.Id, "bing.com/videos", "pathPrefix", now),
        };

        var limits = new[]
        {
            new DailyLimit
            {
                Id = WellKnownSeedIds.YouTubeDailyLimitId,
                DeviceId = device.Id,
                SiteGroupId = youtube.Id,
                LimitSeconds = 45 * 60,
                CreatedAt = now,
            },
            new DailyLimit
            {
                Id = WellKnownSeedIds.VideoSearchDailyLimitId,
                DeviceId = device.Id,
                SiteGroupId = videoSearch.Id,
                LimitSeconds = 15 * 60,
                CreatedAt = now,
            },
        };

        db.Children.Add(child);
        db.Devices.Add(device);
        db.SiteGroups.AddRange(youtube, videoSearch);
        db.SiteRules.AddRange(youtubeRules);
        db.SiteRules.AddRange(videoSearchRules);
        db.DailyLimits.AddRange(limits);

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAdminKeyAsync(string key, string value, CancellationToken cancellationToken)
    {
        var exists = await db.AdminConfigs.AsNoTracking().AnyAsync(c => c.Key == key, cancellationToken);
        if (exists)
            return;

        db.AdminConfigs.Add(new AdminConfig
        {
            Key = key,
            Value = value,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static SiteRule NewRule(string siteGroupId, string pattern, string patternType, DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            SiteGroupId = siteGroupId,
            Pattern = pattern,
            PatternType = patternType,
            CreatedAt = createdAt,
        };
}

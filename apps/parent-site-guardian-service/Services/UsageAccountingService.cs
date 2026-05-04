using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Api;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Data.Entities;
using ParentSiteGuardian.Service.Time;

namespace ParentSiteGuardian.Service.Services;

public sealed class UsageAccountingService(
    AppDbContext db,
    ISiteGroupUrlResolver urlResolver,
    IGuardianTime time) : IUsageAccountingService
{
    public async Task<UsageTickResponse> RecordTickAsync(UsageTickRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ActiveDurationSeconds < 0)
            return new UsageTickResponse { Recorded = false, Reason = "invalid_duration" };

        var device = await db.Devices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId, cancellationToken);
        if (device is null)
            return new UsageTickResponse { Recorded = false, Reason = "unknown_device" };

        var match = await urlResolver.ResolveAsync(request.DeviceId, request.Url, cancellationToken);
        if (match is null)
            return new UsageTickResponse { Recorded = false, Reason = "unmatched" };

        if (request.ActiveDurationSeconds == 0)
            return new UsageTickResponse { Recorded = false, SiteGroupId = match.SiteGroupId, Reason = "zero_duration" };

        db.UsageEvents.Add(new UsageEvent
        {
            Id = Guid.NewGuid().ToString(),
            ChildId = device.ChildId,
            DeviceId = request.DeviceId,
            SiteGroupId = match.SiteGroupId,
            Browser = request.Browser,
            Url = request.Url,
            StartedAt = request.StartedAt,
            EndedAt = request.EndedAt,
            DurationSeconds = request.ActiveDurationSeconds,
        });
        await db.SaveChangesAsync(cancellationToken);

        return new UsageTickResponse
        {
            Recorded = true,
            SiteGroupId = match.SiteGroupId,
            Reason = "ok",
        };
    }

    public async Task<DecisionResponse?> DecideAsync(DecisionRequest request, CancellationToken cancellationToken = default)
    {
        var device = await db.Devices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId, cancellationToken);
        if (device is null)
            return null;

        var match = await urlResolver.ResolveAsync(request.DeviceId, request.Url, cancellationToken);
        if (match is null)
        {
            return new DecisionResponse
            {
                Matched = false,
                Allowed = true,
                UsedSeconds = 0,
                LimitSeconds = 0,
                RemainingSeconds = 0,
                Reason = "unmatched",
            };
        }

        var (used, baseLimit, overrideBonus, effectiveLimit, remaining, allowed) =
            await GetBudgetStateAsync(request.DeviceId, match.SiteGroupId, cancellationToken);

        var (_, endExclusive) = time.GetLocalDayBounds();

        return new DecisionResponse
        {
            Matched = true,
            SiteGroupId = match.SiteGroupId,
            SiteGroupName = match.SiteGroupName,
            Allowed = allowed,
            UsedSeconds = used,
            LimitSeconds = effectiveLimit,
            RemainingSeconds = remaining,
            Reason = allowed ? "ok" : "daily_limit_exhausted",
            BaseLimitSeconds = baseLimit,
            OverrideBonusSeconds = overrideBonus,
            NextResetAt = endExclusive.ToString("O"),
        };
    }

    public async Task<TodayUsageResponse?> GetTodayAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var device = await db.Devices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);
        if (device is null)
            return null;

        var (start, end) = time.GetLocalDayBounds();
        var localDate = start.ToString("yyyy-MM-dd");

        var limits = await db.DailyLimits.AsNoTracking()
            .Where(l => l.DeviceId == deviceId)
            .Join(
                db.SiteGroups.AsNoTracking(),
                l => l.SiteGroupId,
                g => g.Id,
                (l, g) => new { Limit = l, GroupName = g.Name })
            .OrderBy(x => x.GroupName)
            .ToListAsync(cancellationToken);

        // SQLite: DateTimeOffset range filters are not translated; evaluate the window in memory.
        var usageRows = await db.UsageEvents.AsNoTracking()
            .Where(e => e.DeviceId == deviceId)
            .Select(e => new { e.SiteGroupId, e.StartedAt, e.DurationSeconds })
            .ToListAsync(cancellationToken);
        var usageByGroup = usageRows
            .Where(e => e.StartedAt >= start && e.StartedAt < end)
            .GroupBy(e => e.SiteGroupId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.DurationSeconds));

        var now = time.UtcNow;
        var overrideRows = await db.Overrides.AsNoTracking()
            .Where(o => o.DeviceId == deviceId)
            .Select(o => new { o.SiteGroupId, o.ExpiresAt, o.ExtraSeconds })
            .ToListAsync(cancellationToken);
        var overrideByGroup = overrideRows
            .Where(o => o.ExpiresAt > now)
            .GroupBy(o => o.SiteGroupId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.ExtraSeconds));

        var groups = new List<TodaySiteGroupUsageDto>();
        foreach (var row in limits)
        {
            var gid = row.Limit.SiteGroupId;
            usageByGroup.TryGetValue(gid, out var used);
            overrideByGroup.TryGetValue(gid, out var bonus);
            var baseLimit = row.Limit.LimitSeconds;
            var effective = baseLimit + bonus;
            var remaining = Math.Max(0, effective - used);
            groups.Add(new TodaySiteGroupUsageDto
            {
                SiteGroupId = gid,
                SiteGroupName = row.GroupName,
                UsedSeconds = used,
                BaseLimitSeconds = baseLimit,
                OverrideBonusSeconds = bonus,
                EffectiveLimitSeconds = effective,
                RemainingSeconds = remaining,
            });
        }

        return new TodayUsageResponse
        {
            LocalDate = localDate,
            DeviceId = deviceId,
            Groups = groups,
        };
    }

    public async Task<UsageHistoryResponse?> GetHistoryAsync(string deviceId, int days, CancellationToken cancellationToken = default)
    {
        var device = await db.Devices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);
        if (device is null)
            return null;

        days = Math.Clamp(days, 1, 90);

        var localToday = TimeZoneInfo.ConvertTime(time.UtcNow, time.ReportingTimeZone).Date;
        var todayOnly = DateOnly.FromDateTime(localToday);
        var firstDay = todayOnly.AddDays(-(days - 1));

        var groups = await db.SiteGroups.AsNoTracking()
            .Where(g => g.ChildId == device.ChildId)
            .OrderBy(g => g.Name)
            .Select(g => new { g.Id, g.Name })
            .ToListAsync(cancellationToken);

        var (rangeStart, _) = time.GetLocalDayBoundsForLocalDate(firstDay);
        var (_, rangeEnd) = time.GetLocalDayBoundsForLocalDate(todayOnly);

        var usageRows = await db.UsageEvents.AsNoTracking()
            .Where(e => e.DeviceId == deviceId)
            .Select(e => new { e.SiteGroupId, e.StartedAt, e.DurationSeconds })
            .ToListAsync(cancellationToken);

        var inRange = usageRows
            .Where(e => e.StartedAt >= rangeStart && e.StartedAt < rangeEnd)
            .ToList();

        var dayDtos = new List<UsageHistoryDayDto>();
        for (var d = firstDay; d <= todayOnly; d = d.AddDays(1))
        {
            var (start, end) = time.GetLocalDayBoundsForLocalDate(d);
            var dateStr = d.ToString("yyyy-MM-dd");
            var groupDtos = new List<UsageHistoryGroupDto>();
            foreach (var g in groups)
            {
                var used = inRange
                    .Where(e => e.SiteGroupId == g.Id && e.StartedAt >= start && e.StartedAt < end)
                    .Sum(e => e.DurationSeconds);
                groupDtos.Add(new UsageHistoryGroupDto
                {
                    SiteGroupId = g.Id,
                    SiteGroupName = g.Name,
                    UsedSeconds = used,
                });
            }

            dayDtos.Add(new UsageHistoryDayDto
            {
                LocalDate = dateStr,
                Groups = groupDtos,
            });
        }

        return new UsageHistoryResponse
        {
            DeviceId = deviceId,
            Days = dayDtos,
        };
    }

    private async Task<(int used, int baseLimit, int overrideBonus, int effectiveLimit, int remaining, bool allowed)>
        GetBudgetStateAsync(string deviceId, string siteGroupId, CancellationToken cancellationToken)
    {
        var (start, end) = time.GetLocalDayBounds();

        // SQLite: DateTimeOffset range filters are not translated; evaluate the window in memory.
        var usageRows = await db.UsageEvents.AsNoTracking()
            .Where(e => e.DeviceId == deviceId && e.SiteGroupId == siteGroupId)
            .Select(e => new { e.StartedAt, e.DurationSeconds })
            .ToListAsync(cancellationToken);
        var used = usageRows
            .Where(e => e.StartedAt >= start && e.StartedAt < end)
            .Sum(e => e.DurationSeconds);

        var limitRow = await db.DailyLimits.AsNoTracking()
            .FirstOrDefaultAsync(l => l.DeviceId == deviceId && l.SiteGroupId == siteGroupId, cancellationToken);
        var baseLimit = limitRow?.LimitSeconds ?? 0;

        var now = time.UtcNow;
        var overrideRows = await db.Overrides.AsNoTracking()
            .Where(o => o.DeviceId == deviceId && o.SiteGroupId == siteGroupId)
            .Select(o => new { o.ExpiresAt, o.ExtraSeconds })
            .ToListAsync(cancellationToken);
        var overrideBonus = overrideRows.Where(o => o.ExpiresAt > now).Sum(o => o.ExtraSeconds);

        var effectiveLimit = baseLimit + overrideBonus;
        var remaining = Math.Max(0, effectiveLimit - used);
        var allowed = remaining > 0;
        return (used, baseLimit, overrideBonus, effectiveLimit, remaining, allowed);
    }
}

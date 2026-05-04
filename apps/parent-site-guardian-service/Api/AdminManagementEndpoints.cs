using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Data.Entities;
using ParentSiteGuardian.Service.Matching;
using ParentSiteGuardian.Service.Repositories;
using ParentSiteGuardian.Service.Services;

namespace ParentSiteGuardian.Service.Api;

public static class AdminManagementEndpoints
{
    public static IEndpointRouteBuilder MapAdminManagementEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/bootstrap", async Task<IResult> (
            IDeviceRepository devices,
            IParentPinService pins,
            CancellationToken ct) =>
        {
            var list = await devices.ListAllAsync(ct);
            var pinConfigured = await pins.IsConfiguredAsync(ct);
            var dtos = list.Select(d => new AdminBootstrapDeviceDto
            {
                Id = d.Id,
                Name = d.Name,
                ChildId = d.ChildId,
                CreatedAt = d.CreatedAt,
                MachineFingerprint = d.MachineFingerprint,
                WindowsUserSid = d.WindowsUserSid,
            }).ToList();
            return Results.Ok(new AdminBootstrapResponse { PinConfigured = pinConfigured, Devices = dtos });
        });

        app.MapGet("/api/admin/site-groups", async Task<IResult> (
            string deviceId,
            IDeviceRepository devices,
            ISiteGroupRepository siteGroups,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return Results.BadRequest(new { error = "deviceId is required" });
            var device = await devices.GetByIdAsync(deviceId.Trim(), ct);
            if (device is null)
                return Results.NotFound();
            var groups = await siteGroups.ListByChildIdWithRulesAsync(device.ChildId, ct);
            var response = new AdminSiteGroupsResponse
            {
                Groups = groups.Select(g => new AdminSiteGroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Color = g.Color,
                    CreatedAt = g.CreatedAt,
                    Rules = g.SiteRules
                        .OrderBy(r => r.PatternType)
                        .ThenBy(r => r.Pattern)
                        .Select(r => new AdminSiteRuleDto
                        {
                            Id = r.Id,
                            Pattern = r.Pattern,
                            PatternType = r.PatternType,
                            CreatedAt = r.CreatedAt,
                        })
                        .ToList(),
                }).ToList(),
            };
            return Results.Ok(response);
        });

        app.MapGet("/api/admin/usage/history", async Task<IResult> (
            string deviceId,
            int? days,
            IUsageAccountingService usage,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return Results.BadRequest(new { error = "deviceId is required" });
            var d = days ?? 30;
            var result = await usage.GetHistoryAsync(deviceId.Trim(), d, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        app.MapGet("/api/admin/daily-limits", async Task<IResult> (
            string deviceId,
            IDeviceRepository devices,
            IDailyLimitRepository limits,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return Results.BadRequest(new { error = "deviceId is required" });
            var device = await devices.GetByIdAsync(deviceId.Trim(), ct);
            if (device is null)
                return Results.NotFound();
            var rows = await limits.ListByDeviceIdAsync(device.Id, ct);
            var names = await db.SiteGroups.AsNoTracking()
                .Where(g => g.ChildId == device.ChildId)
                .ToDictionaryAsync(g => g.Id, g => g.Name, ct);
            var payload = rows.Select(r => new
            {
                r.Id,
                r.DeviceId,
                r.SiteGroupId,
                siteGroupName = names.GetValueOrDefault(r.SiteGroupId) ?? r.SiteGroupId,
                r.LimitSeconds,
                r.CreatedAt,
            }).ToList();
            return Results.Ok(payload);
        });

        app.MapPost("/api/admin/site-groups", async Task<IResult> (
            CreateSiteGroupRequest body,
            IParentPinService pins,
            IDeviceRepository devices,
            ISiteGroupRepository siteGroups,
            CancellationToken ct) =>
        {
            var guard = await GuardPinAsync(pins, body.Pin, ct);
            if (guard is not null)
                return guard;
            var device = await devices.GetByIdAsync(body.DeviceId.Trim(), ct);
            if (device is null)
                return Results.NotFound(new { error = "unknown_device" });
            var name = body.Name.Trim();
            if (string.IsNullOrEmpty(name))
                return Results.BadRequest(new { error = "invalid_name" });
            var now = DateTimeOffset.UtcNow;
            var group = new SiteGroup
            {
                Id = Guid.NewGuid().ToString(),
                ChildId = device.ChildId,
                Name = name,
                Color = null,
                CreatedAt = now,
            };
            await siteGroups.AddAsync(group, ct);
            return Results.Ok(new { id = group.Id });
        });

        app.MapPut("/api/admin/site-groups/{id}", async Task<IResult> (
            string id,
            UpdateSiteGroupRequest body,
            IParentPinService pins,
            IDeviceRepository devices,
            ISiteGroupRepository siteGroups,
            CancellationToken ct) =>
        {
            var guard = await GuardPinAsync(pins, body.Pin, ct);
            if (guard is not null)
                return guard;
            var device = await devices.GetByIdAsync(body.DeviceId.Trim(), ct);
            if (device is null)
                return Results.NotFound(new { error = "unknown_device" });
            var group = await siteGroups.GetByIdAsync(id, ct);
            if (group is null || group.ChildId != device.ChildId)
                return Results.NotFound(new { error = "unknown_site_group" });
            var name = body.Name.Trim();
            if (string.IsNullOrEmpty(name))
                return Results.BadRequest(new { error = "invalid_name" });
            group.Name = name;
            await siteGroups.UpdateAsync(group, ct);
            return Results.Ok(new { status = "ok" });
        });

        app.MapDelete("/api/admin/site-groups/{id}", async Task<IResult> (
            string id,
            [FromBody] PinProtectedDeviceRequest body,
            IParentPinService pins,
            IDeviceRepository devices,
            ISiteGroupRepository siteGroups,
            CancellationToken ct) =>
        {
            var guard = await GuardPinAsync(pins, body.Pin, ct);
            if (guard is not null)
                return guard;
            var device = await devices.GetByIdAsync(body.DeviceId.Trim(), ct);
            if (device is null)
                return Results.NotFound(new { error = "unknown_device" });
            var group = await siteGroups.GetByIdAsync(id, ct);
            if (group is null || group.ChildId != device.ChildId)
                return Results.NotFound(new { error = "unknown_site_group" });
            await siteGroups.DeleteAsync(id, ct);
            return Results.Ok(new { status = "ok" });
        });

        app.MapPost("/api/admin/site-groups/{groupId}/rules", async Task<IResult> (
            string groupId,
            AddSiteRuleRequest body,
            IParentPinService pins,
            IDeviceRepository devices,
            ISiteGroupRepository siteGroups,
            ISiteRuleRepository rules,
            CancellationToken ct) =>
        {
            var guard = await GuardPinAsync(pins, body.Pin, ct);
            if (guard is not null)
                return guard;
            var device = await devices.GetByIdAsync(body.DeviceId.Trim(), ct);
            if (device is null)
                return Results.NotFound(new { error = "unknown_device" });
            var group = await siteGroups.GetByIdAsync(groupId, ct);
            if (group is null || group.ChildId != device.ChildId)
                return Results.NotFound(new { error = "unknown_site_group" });
            if (!IsPlausibleRule(body.Pattern, body.PatternType))
                return Results.BadRequest(new { error = "invalid_rule" });
            var now = DateTimeOffset.UtcNow;
            var rule = new SiteRule
            {
                Id = Guid.NewGuid().ToString(),
                SiteGroupId = groupId,
                Pattern = body.Pattern.Trim(),
                PatternType = body.PatternType.Trim(),
                CreatedAt = now,
            };
            await rules.AddAsync(rule, ct);
            return Results.Ok(new { id = rule.Id });
        });

        app.MapPost("/api/admin/site-rules/delete", async Task<IResult> (
            DeleteSiteRuleRequest body,
            IParentPinService pins,
            IDeviceRepository devices,
            ISiteGroupRepository siteGroups,
            ISiteRuleRepository rules,
            CancellationToken ct) =>
        {
            var guard = await GuardPinAsync(pins, body.Pin, ct);
            if (guard is not null)
                return guard;
            var device = await devices.GetByIdAsync(body.DeviceId.Trim(), ct);
            if (device is null)
                return Results.NotFound(new { error = "unknown_device" });
            var rule = await rules.GetByIdAsync(body.RuleId, ct);
            if (rule is null)
                return Results.NotFound(new { error = "unknown_rule" });
            var group = await siteGroups.GetByIdAsync(rule.SiteGroupId, ct);
            if (group is null || group.ChildId != device.ChildId)
                return Results.NotFound(new { error = "unknown_rule" });
            await rules.DeleteAsync(body.RuleId, ct);
            return Results.Ok(new { status = "ok" });
        });

        app.MapPut("/api/admin/daily-limits", async Task<IResult> (
            UpsertDailyLimitRequest body,
            IParentPinService pins,
            IDeviceRepository devices,
            ISiteGroupRepository siteGroups,
            IDailyLimitRepository limits,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var guard = await GuardPinAsync(pins, body.Pin, ct);
            if (guard is not null)
                return guard;
            if (body.LimitSeconds < 0)
                return Results.BadRequest(new { error = "invalid_limit" });
            var device = await devices.GetByIdAsync(body.DeviceId.Trim(), ct);
            if (device is null)
                return Results.NotFound(new { error = "unknown_device" });
            var group = await siteGroups.GetByIdAsync(body.SiteGroupId, ct);
            if (group is null || group.ChildId != device.ChildId)
                return Results.NotFound(new { error = "unknown_site_group" });
            var existing = await limits.GetByDeviceAndSiteGroupAsync(device.Id, body.SiteGroupId, ct);
            var now = DateTimeOffset.UtcNow;
            if (existing is null)
            {
                await limits.AddAsync(new DailyLimit
                {
                    Id = Guid.NewGuid().ToString(),
                    DeviceId = device.Id,
                    SiteGroupId = body.SiteGroupId,
                    LimitSeconds = body.LimitSeconds,
                    CreatedAt = now,
                }, ct);
            }
            else
            {
                var tracked = await db.DailyLimits.FirstOrDefaultAsync(l => l.Id == existing.Id, ct);
                if (tracked is null)
                    return Results.NotFound();
                tracked.LimitSeconds = body.LimitSeconds;
                await db.SaveChangesAsync(ct);
            }

            return Results.Ok(new { status = "ok" });
        });

        return app;
    }

    private static async Task<IResult?> GuardPinAsync(IParentPinService pins, string? pin, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pin))
            return Results.BadRequest(new { error = "pin_required" });
        if (!await pins.IsConfiguredAsync(ct))
            return Results.Conflict(new { error = "pin_not_configured" });
        if (!await pins.VerifyAsync(pin, ct))
            return Results.Unauthorized();
        return null;
    }

    private static bool IsPlausibleRule(string pattern, string patternType)
    {
        var p = pattern.Trim();
        var t = patternType.Trim();
        if (string.IsNullOrEmpty(p))
            return false;
        if (string.Equals(t, UrlMatcher.PatternTypeDomain, StringComparison.OrdinalIgnoreCase))
            return UrlMatcher.IsMatch("https://a." + p + "/", p, UrlMatcher.PatternTypeDomain);
        if (string.Equals(t, UrlMatcher.PatternTypePathPrefix, StringComparison.OrdinalIgnoreCase))
            return UrlMatcher.IsMatch("https://www." + p + "/extra", p, UrlMatcher.PatternTypePathPrefix);
        return false;
    }
}

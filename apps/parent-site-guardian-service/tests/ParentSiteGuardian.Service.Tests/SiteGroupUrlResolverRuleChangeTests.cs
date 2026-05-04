using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Data.Entities;
using ParentSiteGuardian.Service.Services;
using ParentSiteGuardian.Service.Tests.Support;

namespace ParentSiteGuardian.Service.Tests;

/// <summary>Covers the same data path as an admin “add rule” after seed (no HTTP layer).</summary>
public class SiteGroupUrlResolverRuleChangeTests
{
    [Fact]
    public async Task Newly_inserted_domain_rule_is_visible_to_resolver()
    {
        var (db, path) = await TestAppDbContextFactory.CreateEmptyAsync();
        try
        {
            await new DatabaseSeedService(db).SeedIfNeededAsync();

            var testUrl = "https://z.parentguardian-e2e-test.invalid/path";
            var resolverBefore = new SiteGroupUrlResolver(db);
            Assert.Null(await resolverBefore.ResolveAsync(WellKnownSeedIds.DefaultDeviceId, testUrl));

            db.SiteRules.Add(new SiteRule
            {
                Id = Guid.NewGuid().ToString(),
                SiteGroupId = WellKnownSeedIds.YouTubeSiteGroupId,
                Pattern = "parentguardian-e2e-test.invalid",
                PatternType = "domain",
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();

            var resolverAfter = new SiteGroupUrlResolver(db);
            var match = await resolverAfter.ResolveAsync(WellKnownSeedIds.DefaultDeviceId, testUrl);
            Assert.NotNull(match);
            Assert.Equal(WellKnownSeedIds.YouTubeSiteGroupId, match!.SiteGroupId);
            Assert.Equal("YouTube", match.SiteGroupName);
        }
        finally
        {
            TestAppDbContextFactory.Destroy(db, path);
        }
    }
}

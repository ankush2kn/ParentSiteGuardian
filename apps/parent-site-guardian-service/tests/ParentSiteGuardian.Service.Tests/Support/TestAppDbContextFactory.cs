using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;

namespace ParentSiteGuardian.Service.Tests.Support;

internal static class TestAppDbContextFactory
{
    public static async Task<(AppDbContext Context, string DbPath)> CreateEmptyAsync()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"psg-test-{Guid.NewGuid():n}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        var ctx = new AppDbContext(options);
        await ctx.Database.MigrateAsync();
        return (ctx, dbPath);
    }

    public static void Destroy(AppDbContext ctx, string dbPath)
    {
        ctx.Dispose();
        try
        {
            File.Delete(dbPath);
        }
        catch (IOException)
        {
            // Windows may still hold a lock briefly; tests use unique paths.
        }
    }
}

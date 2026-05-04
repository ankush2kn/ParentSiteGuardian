using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ParentSiteGuardian.Service.Data;

/// <summary>Used by <c>dotnet ef</c> when the web host is not running.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=parent-site-guardian-design.db")
            .Options;
        return new AppDbContext(options);
    }
}

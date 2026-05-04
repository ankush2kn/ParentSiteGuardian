using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Child> Children => Set<Child>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<SiteGroup> SiteGroups => Set<SiteGroup>();
    public DbSet<SiteRule> SiteRules => Set<SiteRule>();
    public DbSet<DailyLimit> DailyLimits => Set<DailyLimit>();
    public DbSet<UsageEvent> UsageEvents => Set<UsageEvent>();
    public DbSet<Override> Overrides => Set<Override>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<AdminConfig> AdminConfigs => Set<AdminConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Child>(e =>
        {
            e.ToTable("children");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<Device>(e =>
        {
            e.ToTable("devices");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();
            e.HasOne(x => x.Child)
                .WithMany(x => x.Devices)
                .HasForeignKey(x => x.ChildId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SiteGroup>(e =>
        {
            e.ToTable("site_groups");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();
            e.HasOne(x => x.Child)
                .WithMany(x => x.SiteGroups)
                .HasForeignKey(x => x.ChildId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SiteRule>(e =>
        {
            e.ToTable("site_rules");
            e.HasKey(x => x.Id);
            e.Property(x => x.Pattern).IsRequired();
            e.Property(x => x.PatternType).IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();
            e.HasOne(x => x.SiteGroup)
                .WithMany(x => x.SiteRules)
                .HasForeignKey(x => x.SiteGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DailyLimit>(e =>
        {
            e.ToTable("daily_limits");
            e.HasKey(x => x.Id);
            e.Property(x => x.LimitSeconds).IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();
            e.HasOne(x => x.Device)
                .WithMany(x => x.DailyLimits)
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.SiteGroup)
                .WithMany(x => x.DailyLimits)
                .HasForeignKey(x => x.SiteGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UsageEvent>(e =>
        {
            e.ToTable("usage_events");
            e.HasKey(x => x.Id);
            e.Property(x => x.Browser).IsRequired();
            e.Property(x => x.Url).IsRequired();
            e.Property(x => x.StartedAt).IsRequired();
            e.Property(x => x.EndedAt).IsRequired();
            e.Property(x => x.DurationSeconds).IsRequired();
            e.HasOne(x => x.Child)
                .WithMany(x => x.UsageEvents)
                .HasForeignKey(x => x.ChildId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Device)
                .WithMany(x => x.UsageEvents)
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.SiteGroup)
                .WithMany(x => x.UsageEvents)
                .HasForeignKey(x => x.SiteGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Override>(e =>
        {
            e.ToTable("overrides");
            e.HasKey(x => x.Id);
            e.Property(x => x.ExtraSeconds).IsRequired();
            e.Property(x => x.ExpiresAt).IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();
            e.HasOne(x => x.Device)
                .WithMany(x => x.Overrides)
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.SiteGroup)
                .WithMany(x => x.Overrides)
                .HasForeignKey(x => x.SiteGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Setting>(e =>
        {
            e.ToTable("settings");
            e.HasKey(x => x.Key);
            e.Property(x => x.Value).IsRequired();
        });

        modelBuilder.Entity<AdminConfig>(e =>
        {
            e.ToTable("admin_config");
            e.HasKey(x => x.Key);
            e.Property(x => x.Value).IsRequired();
            e.Property(x => x.UpdatedAt).IsRequired();
        });
    }
}

using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Api;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGuardianPersistence();
builder.Services.AddGuardianApplication();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var seed = scope.ServiceProvider.GetRequiredService<IDatabaseSeedService>();
    await LegacySqliteMigrationAdoption.BeforeMigrateAsync(db);
    await db.Database.MigrateAsync();
    await seed.EnsureAdminConfigDefaultsAsync();
    await seed.SeedIfNeededAsync();
}

app.MapGet("/health", () =>
    Results.Json(new
    {
        status = "ok",
        service = "parent-site-guardian",
        timestamp = DateTimeOffset.UtcNow,
    }));

app.MapUsageEndpoints();
app.MapAdminEndpoints();
app.MapAdminManagementEndpoints();
app.MapExtensionConfigEndpoints();

app.Run();

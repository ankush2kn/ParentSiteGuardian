using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting.WindowsServices;
using ParentSiteGuardian.Service.Api;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Extensions;
using ParentSiteGuardian.Service.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService();
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

string? adminUiRoot = null;
var adminCandidate = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "admin-ui"));
if (Directory.Exists(adminCandidate))
{
    adminUiRoot = adminCandidate;
    var adminFiles = new PhysicalFileProvider(adminUiRoot);
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = adminFiles,
        RequestPath = "/admin",
        DefaultFileNames = { "index.html" },
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = adminFiles,
        RequestPath = "/admin",
    });
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var seed = scope.ServiceProvider.GetRequiredService<IDatabaseSeedService>();
    await LegacySqliteMigrationAdoption.BeforeMigrateAsync(db);
    await db.Database.MigrateAsync();
    await seed.EnsureAdminConfigDefaultsAsync();
    await seed.SeedIfNeededAsync();
    var localDevice = scope.ServiceProvider.GetRequiredService<ILocalDeviceIdentityService>();
    await localDevice.EnsurePersistAndGetAsync(CancellationToken.None);
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
app.MapConfigEndpoints();

if (adminUiRoot is not null)
{
    app.MapFallback("/admin/{**path:nonfile}", async context =>
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(Path.Combine(adminUiRoot, "index.html"));
    });
    app.MapGet("/", () => Results.Redirect("/admin/"));
}

app.Run();

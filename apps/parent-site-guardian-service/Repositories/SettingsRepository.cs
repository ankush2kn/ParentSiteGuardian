using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Repositories;

public sealed class SettingsRepository(AppDbContext db) : ISettingsRepository
{
    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var row = await db.Settings.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        return row?.Value;
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var existing = await db.Settings.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (existing is null)
            db.Settings.Add(new Setting { Key = key, Value = value });
        else
            existing.Value = value;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var entity = await db.Settings.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (entity is null)
            return;
        db.Settings.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}

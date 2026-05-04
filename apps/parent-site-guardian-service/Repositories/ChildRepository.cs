using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Repositories;

public sealed class ChildRepository(AppDbContext db) : IChildRepository
{
    public Task<Child?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        db.Children.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Child>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Children.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public async Task<Child> AddAsync(Child child, CancellationToken cancellationToken = default)
    {
        db.Children.Add(child);
        await db.SaveChangesAsync(cancellationToken);
        return child;
    }

    public async Task UpdateAsync(Child child, CancellationToken cancellationToken = default)
    {
        var tracked = await db.Children.FirstOrDefaultAsync(x => x.Id == child.Id, cancellationToken);
        if (tracked is null)
            return;
        tracked.Name = child.Name;
        tracked.CreatedAt = child.CreatedAt;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Children.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            return;
        db.Children.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}

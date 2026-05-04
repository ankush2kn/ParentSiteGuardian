using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Repositories;

public sealed class DeviceRepository(AppDbContext db) : IDeviceRepository
{
    public async Task<IReadOnlyList<Device>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await db.Devices.AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<Device?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        db.Devices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Device>> ListByChildIdAsync(string childId, CancellationToken cancellationToken = default) =>
        await db.Devices.AsNoTracking()
            .Where(x => x.ChildId == childId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task<Device> AddAsync(Device device, CancellationToken cancellationToken = default)
    {
        db.Devices.Add(device);
        await db.SaveChangesAsync(cancellationToken);
        return device;
    }

    public async Task UpdateAsync(Device device, CancellationToken cancellationToken = default)
    {
        var tracked = await db.Devices.FirstOrDefaultAsync(x => x.Id == device.Id, cancellationToken);
        if (tracked is null)
            return;
        tracked.Name = device.Name;
        tracked.MachineFingerprint = device.MachineFingerprint;
        tracked.WindowsUserSid = device.WindowsUserSid;
        tracked.CreatedAt = device.CreatedAt;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Devices.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            return;
        db.Devices.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}

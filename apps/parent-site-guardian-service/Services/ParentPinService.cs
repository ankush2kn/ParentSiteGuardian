using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Data.Entities;

namespace ParentSiteGuardian.Service.Services;

public sealed class ParentPinService(AppDbContext db) : IParentPinService
{
    private const int MinPinLength = 4;
    private const int MaxPinLength = 72;

    public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default) =>
        db.AdminConfigs.AsNoTracking().AnyAsync(c => c.Key == AdminConfigKeys.ParentPinHash, cancellationToken);

    public async Task<ParentPinSetupResult> SetupAsync(string pin, CancellationToken cancellationToken = default)
    {
        if (await IsConfiguredAsync(cancellationToken))
            return ParentPinSetupResult.AlreadyConfigured;

        if (!IsValidPin(pin))
            return ParentPinSetupResult.InvalidPin;

        var hash = BCrypt.Net.BCrypt.HashPassword(pin, workFactor: 11);
        db.AdminConfigs.Add(new AdminConfig
        {
            Key = AdminConfigKeys.ParentPinHash,
            Value = hash,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
        return ParentPinSetupResult.Ok;
    }

    public async Task<bool> VerifyAsync(string pin, CancellationToken cancellationToken = default)
    {
        var row = await db.AdminConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == AdminConfigKeys.ParentPinHash, cancellationToken);
        if (row is null)
            return false;
        return BCrypt.Net.BCrypt.Verify(pin, row.Value);
    }

    public async Task<ParentPinChangeResult> ChangeAsync(string currentPin, string newPin, CancellationToken cancellationToken = default)
    {
        if (!await IsConfiguredAsync(cancellationToken))
            return ParentPinChangeResult.NotConfigured;

        if (!IsValidPin(newPin))
            return ParentPinChangeResult.InvalidNewPin;

        if (!await VerifyAsync(currentPin, cancellationToken))
            return ParentPinChangeResult.InvalidCurrentPin;

        var hash = BCrypt.Net.BCrypt.HashPassword(newPin, workFactor: 11);
        var tracked = await db.AdminConfigs.FirstOrDefaultAsync(c => c.Key == AdminConfigKeys.ParentPinHash, cancellationToken);
        if (tracked is null)
            return ParentPinChangeResult.NotConfigured;

        tracked.Value = hash;
        tracked.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ParentPinChangeResult.Ok;
    }

    private static bool IsValidPin(string pin) =>
        pin.Length is >= MinPinLength and <= MaxPinLength;
}

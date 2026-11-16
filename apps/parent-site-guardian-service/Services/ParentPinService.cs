using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ParentSiteGuardian.Service.Data;
using ParentSiteGuardian.Service.Data.Entities;
using ParentSiteGuardian.Service.Time;

namespace ParentSiteGuardian.Service.Services;

public sealed class ParentPinService(AppDbContext db, IGuardianTime time) : IParentPinService
{
    private const int MinPinLength = 4;
    private const int MaxPinLength = 72;

    /// <summary>Wrong attempts allowed before any waiting is required.</summary>
    public const int FreeAttempts = 3;

    /// <summary>Upper bound so a child cannot lock the parent out of the dashboard for days.</summary>
    public static readonly TimeSpan MaxLockout = TimeSpan.FromHours(24);

    /// <summary>
    /// Serializes PIN checks across all requests (single service process) so parallel guesses cannot
    /// all read "not locked" before any failure is recorded.
    /// </summary>
    private static readonly SemaphoreSlim AttemptGate = new(1, 1);

    /// <summary>
    /// Wait required after the <paramref name="failedAttempts"/>-th consecutive wrong PIN:
    /// none for the first <see cref="FreeAttempts"/>, then 1, 3, 9, 27… minutes (×3 each time), capped at <see cref="MaxLockout"/>.
    /// </summary>
    public static TimeSpan LockoutAfterFailures(int failedAttempts)
    {
        if (failedAttempts < FreeAttempts)
            return TimeSpan.Zero;

        var minutes = 1.0;
        for (var i = FreeAttempts; i < failedAttempts && minutes < MaxLockout.TotalMinutes; i++)
            minutes *= 3;

        var wait = TimeSpan.FromMinutes(minutes);
        return wait > MaxLockout ? MaxLockout : wait;
    }

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
            UpdatedAt = time.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
        return ParentPinSetupResult.Ok;
    }

    public async Task<PinCheckResult> CheckAsync(string pin, CancellationToken cancellationToken = default)
    {
        await AttemptGate.WaitAsync(cancellationToken);
        try
        {
            var hashRow = await db.AdminConfigs.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Key == AdminConfigKeys.ParentPinHash, cancellationToken);
            if (hashRow is null)
                return new PinCheckResult(PinCheckStatus.NotConfigured, TimeSpan.Zero);

            var now = time.UtcNow;
            var (failures, lockedUntil) = await ReadAttemptStateAsync(cancellationToken);

            // While locked, reject without evaluating the PIN (even a correct one) and without counting the attempt.
            if (lockedUntil is { } until && until > now)
                return new PinCheckResult(PinCheckStatus.LockedOut, until - now);

            if (!string.IsNullOrEmpty(pin) && BCrypt.Net.BCrypt.Verify(pin, hashRow.Value))
            {
                if (failures != 0 || lockedUntil is not null)
                    await WriteAttemptStateAsync(0, null, now, cancellationToken);
                return new PinCheckResult(PinCheckStatus.Ok, TimeSpan.Zero);
            }

            failures++;
            var wait = LockoutAfterFailures(failures);
            await WriteAttemptStateAsync(failures, wait > TimeSpan.Zero ? now + wait : null, now, cancellationToken);
            return new PinCheckResult(PinCheckStatus.Invalid, wait);
        }
        finally
        {
            AttemptGate.Release();
        }
    }

    public async Task<TimeSpan> GetLockoutRemainingAsync(CancellationToken cancellationToken = default)
    {
        var (_, lockedUntil) = await ReadAttemptStateAsync(cancellationToken);
        var now = time.UtcNow;
        return lockedUntil is { } until && until > now ? until - now : TimeSpan.Zero;
    }

    public async Task<ParentPinChangeResult> ChangeAsync(string currentPin, string newPin, CancellationToken cancellationToken = default)
    {
        if (!await IsConfiguredAsync(cancellationToken))
            return ParentPinChangeResult.NotConfigured;

        if (!IsValidPin(newPin))
            return ParentPinChangeResult.InvalidNewPin;

        var check = await CheckAsync(currentPin, cancellationToken);
        switch (check.Status)
        {
            case PinCheckStatus.NotConfigured:
                return ParentPinChangeResult.NotConfigured;
            case PinCheckStatus.LockedOut:
                return ParentPinChangeResult.LockedOut;
            case PinCheckStatus.Invalid:
                return ParentPinChangeResult.InvalidCurrentPin;
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(newPin, workFactor: 11);
        var tracked = await db.AdminConfigs.FirstOrDefaultAsync(c => c.Key == AdminConfigKeys.ParentPinHash, cancellationToken);
        if (tracked is null)
            return ParentPinChangeResult.NotConfigured;

        tracked.Value = hash;
        tracked.UpdatedAt = time.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ParentPinChangeResult.Ok;
    }

    private async Task<(int failures, DateTimeOffset? lockedUntil)> ReadAttemptStateAsync(CancellationToken ct)
    {
        var rows = await db.AdminConfigs.AsNoTracking()
            .Where(c => c.Key == AdminConfigKeys.PinFailedAttempts || c.Key == AdminConfigKeys.PinLockedUntil)
            .ToListAsync(ct);

        var failures = 0;
        DateTimeOffset? lockedUntil = null;
        foreach (var row in rows)
        {
            if (row.Key == AdminConfigKeys.PinFailedAttempts
                && int.TryParse(row.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                failures = Math.Max(0, n);
            else if (row.Key == AdminConfigKeys.PinLockedUntil
                     && DateTimeOffset.TryParse(row.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var t))
                lockedUntil = t;
        }

        return (failures, lockedUntil);
    }

    private async Task WriteAttemptStateAsync(int failures, DateTimeOffset? lockedUntil, DateTimeOffset now, CancellationToken ct)
    {
        await UpsertAsync(AdminConfigKeys.PinFailedAttempts, failures.ToString(CultureInfo.InvariantCulture), now, ct);
        await UpsertAsync(AdminConfigKeys.PinLockedUntil, lockedUntil?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty, now, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task UpsertAsync(string key, string value, DateTimeOffset now, CancellationToken ct)
    {
        var row = await db.AdminConfigs.FirstOrDefaultAsync(c => c.Key == key, ct);
        if (row is null)
        {
            db.AdminConfigs.Add(new AdminConfig { Key = key, Value = value, UpdatedAt = now });
        }
        else
        {
            row.Value = value;
            row.UpdatedAt = now;
        }
    }

    private static bool IsValidPin(string pin) =>
        pin.Length is >= MinPinLength and <= MaxPinLength;
}

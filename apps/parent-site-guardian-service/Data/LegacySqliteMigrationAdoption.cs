using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace ParentSiteGuardian.Service.Data;

/// <summary>
/// Adopts SQLite files that were created with <c>EnsureCreated</c> before EF migrations were enabled:
/// tables exist but <c>__EFMigrationsHistory</c> is missing, so <see cref="RelationalDatabaseFacadeExtensions.MigrateAsync"/>
/// would try to run <c>InitialCreate</c> again and fail on "table already exists".
/// </summary>
public static class LegacySqliteMigrationAdoption
{
    /// <summary>Must match the first migration class name in <c>Data/Migrations</c> (timestamp prefix).</summary>
    public const string InitialMigrationId = "20260504194027_InitialCreate";

    public const string EfProductVersion = "10.0.1";

    public static async Task BeforeMigrateAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync(cancellationToken);
        try
        {
            if (!await TableExistsAsync(conn, "children", cancellationToken))
                return;

            if (await TableExistsAsync(conn, "__EFMigrationsHistory", cancellationToken)
                && await HistoryContainsMigrationAsync(conn, InitialMigrationId, cancellationToken))
                return;

            await EnsureAdminConfigTableAsync(conn, cancellationToken);
            await EnsureHistoryTableAsync(conn, cancellationToken);
            await InsertBaselineIfNeededAsync(conn, cancellationToken);
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    private static async Task EnsureAdminConfigTableAsync(DbConnection conn, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS "admin_config" (
                "Key" TEXT NOT NULL CONSTRAINT "PK_admin_config" PRIMARY KEY,
                "Value" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL
            );
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureHistoryTableAsync(DbConnection conn, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertBaselineIfNeededAsync(DbConnection conn, CancellationToken cancellationToken)
    {
        if (await HistoryContainsMigrationAsync(conn, InitialMigrationId, cancellationToken))
            return;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES (@id, @ver);
            """;
        var id = cmd.CreateParameter();
        id.ParameterName = "@id";
        id.Value = InitialMigrationId;
        cmd.Parameters.Add(id);
        var ver = cmd.CreateParameter();
        ver.ParameterName = "@ver";
        ver.Value = EfProductVersion;
        cmd.Parameters.Add(ver);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(DbConnection connection, string tableName, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM sqlite_master
            WHERE type = 'table' AND name = @name;
            """;
        var p = cmd.CreateParameter();
        p.ParameterName = "@name";
        p.Value = tableName;
        cmd.Parameters.Add(p);
        var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(scalar) > 0;
    }

    private static async Task<bool> HistoryContainsMigrationAsync(
        DbConnection connection,
        string migrationId,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "__EFMigrationsHistory", cancellationToken))
            return false;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM "__EFMigrationsHistory" WHERE "MigrationId" = @id;
            """;
        var p = cmd.CreateParameter();
        p.ParameterName = "@id";
        p.Value = migrationId;
        cmd.Parameters.Add(p);
        var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(scalar) > 0;
    }
}

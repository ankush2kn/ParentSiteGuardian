# Parent Site Guardian

Local Windows MVP for per–site-group time limits across Chrome and Edge. This repository is a **monorepo** with a .NET local API, a React admin UI, and a Manifest V3 browser extension.

**Progress:** SQLite + migrations, URL matching, usage accounting, **parent PIN** (BCrypt in `admin_config`), **admin override APIs**, polished **extension block page** with PIN + 5/10/15/30 minute grants. Admin UI remains a placeholder.

## Repository layout

| Path | Description |
|------|-------------|
| `apps/parent-site-guardian-service` | .NET 10 minimal API (`GET /health`) |
| `apps/admin-ui` | React + Vite + TypeScript (placeholder dashboard) |
| `apps/browser-extension` | Chrome / Edge MV3 extension (TypeScript, esbuild) |
| `docs` | Links to PRD and phase plan (see [docs/README.md](docs/README.md)) |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (for the local service)
- [Node.js](https://nodejs.org/) 20+ and npm (for admin UI and extension)

Verify:

```powershell
dotnet --version
node --version
npm --version
```

If `dotnet` is **not recognized**, the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) is missing or not on your `PATH`. Install the **SDK**, then **close and reopen** your terminal (and VS Code / Cursor if the terminal was opened from the IDE). Run `dotnet --version` again; you should see a `10.0.xxx` SDK.

`npm run dev:service` uses a small check first: if `dotnet` still cannot be found, npm prints install hints instead of the raw Windows error.

## First-time setup

From the repository root:

```powershell
npm install
```

This installs dependencies for **admin-ui** and **browser-extension** via npm workspaces.

## Local development

Run each part in its own terminal.

**1. Local API (default URL `http://127.0.0.1:5050`)**

```powershell
npm run dev:service
```

Smoke test:

```powershell
curl http://127.0.0.1:5050/health
```

You should see JSON with `"status":"ok"`.

### Database migrations

The service applies EF Core migrations on startup (`Database.MigrateAsync`). Migration C# files live in `apps/parent-site-guardian-service/Data/Migrations/`.

To add the `dotnet-ef` tool and apply migrations manually from the repo root:

```powershell
dotnet tool restore
dotnet ef database update --project apps/parent-site-guardian-service/ParentSiteGuardian.Service.csproj --configuration Release
```

**Upgrading from older builds** that created SQLite with `EnsureCreated` (no `__EFMigrationsHistory` table): on startup the service **baselines** that database (adds `admin_config` if needed and records the initial migration) so `MigrateAsync` does not try to recreate existing tables. If you still see migration errors, delete the database file after backing it up or point `GuardianDatabase:Path` to a new file.

Configurable extension settings are stored in the **`admin_config`** table (key/value). The usage tick interval uses key `usage_tick_interval_seconds` (integer seconds, default `10`). The extension reads it from `GET /api/extension-config`.

Usage APIs (after seed, use the default device id from the database or `20000000-0000-4000-8000-000000000001` from first-run seed):

- `POST /api/usage/tick` — JSON body: `deviceId`, `browser`, `url`, `startedAt`, `endedAt`, `activeDurationSeconds`. Records a row only when the URL matches a site group.
- `POST /api/decision` — JSON body: `deviceId`, `url`. Returns `matched`, `allowed`, `usedSeconds`, `limitSeconds` (effective limit including active overrides), `remainingSeconds`, `reason`, plus `baseLimitSeconds`, `overrideBonusSeconds`, and `nextResetAt` (ISO) when matched.
- `GET /api/usage/today?deviceId=...` — Today’s totals per site group (local calendar day, service `TimeZoneInfo.Local`).
- `GET /api/extension-config` — JSON: `usageTickIntervalSeconds` (from `admin_config`; extension polls this).

**Parent PIN (once per database, before overrides):**

- `GET /api/admin/pin/status` — `{ "configured": true|false }`
- `POST /api/admin/pin/setup` — body `{ "pin": "...." }` (min 4 characters). **409** if a PIN already exists.
- `POST /api/admin/pin/verify` — body `{ "pin": "...." }` — `{ "valid": true|false }`
- `POST /api/admin/overrides` — body `{ "deviceId", "siteGroupId", "pin", "extraMinutes": 5|10|15|30 }`. **401** if PIN is wrong. Creates a row in **`overrides`**.

Example (PowerShell) to set the PIN once:

```powershell
Invoke-RestMethod -Uri "http://127.0.0.1:5050/api/admin/pin/setup" -Method Post -ContentType "application/json" -Body '{"pin":"424242"}'
```

CORS is enabled so the extension’s `blocked.html` page can call these APIs from `chrome-extension://` origins.

**2. Admin UI**

```powershell
npm run dev:admin
```

Open the URL Vite prints (typically `http://localhost:5173`). You should see the placeholder **Admin dashboard**.

**3. Browser extension**

```powershell
npm run dev:extension
```

This runs a **watch** build into `apps/browser-extension/dist`. In Chrome or Edge:

1. Open `chrome://extensions` or `edge://extensions`
2. Enable **Developer mode**
3. **Load unpacked** → select `apps/browser-extension/dist`

The popup shows a short scaffold message.

## Service unit tests (SQLite)

From `apps/parent-site-guardian-service`:

```powershell
dotnet test tests\ParentSiteGuardian.Service.Tests\ParentSiteGuardian.Service.Tests.csproj
```

Stop the running API first if `dotnet test` fails with “file is being used by another process” on `ParentSiteGuardian.Service.dll`.

Tests use a temporary SQLite file each run. The running app stores data under `%ProgramData%\ParentSiteGuardian\parent-site-guardian.db` by default (override with `GuardianDatabase:Path` in `appsettings.json` or environment). To re-run **first-run seed** after experimenting, stop the service and delete that database file.

C:\ProgramData\ParentSiteGuardian\parent-site-guardian.db

ProgramData is hidden by default in Explorer. Open it with:

Win + R → shell:Common AppData → Enter → open the ParentSiteGuardian folder, or
Paste in Explorer: C:\ProgramData\ParentSiteGuardian\
The file only appears after the API has started at least once (so it can create the folder and DB).

## Build (production-style artifacts)

```powershell
npm run build:service
npm run build:admin
npm run build:extension
```

Or run all three:

```powershell
npm run build
```

- Service binaries: `apps/parent-site-guardian-service/bin/Release/net10.0/`
- Admin UI static files: `apps/admin-ui/dist/`
- Extension: `apps/browser-extension/dist/`

### Windows laptop install (Phase 1)

For publishing the API to `dist/install/`, scheduled task or Windows Service registration, Chrome/Edge unpacked load steps, and SQLite backup/reset — all **without cloud accounts** — see **[docs/windows-phase1-install.md](docs/windows-phase1-install.md)** and the PowerShell scripts in **`scripts/windows/`**.

### Build the Windows installer (.exe)

Use this on your **development machine** (not on end-user PCs). You already need **.NET 10 SDK**, **Node.js 20+**, and **`npm install`** from the repo root.

1. Install **[Inno Setup 6](https://jrsoftware.org/isdl.php)** (free). The build script looks for `ISCC.exe` under `Program Files` or `Program Files (x86)`.
2. From the **repository root**, run:

```powershell
npm run build:installer
```

This builds the self-contained service (`win-x64`), production admin UI + extension, stages everything under **`installer/staging/`**, and compiles **`dist/installer/ParentSiteGuardian-Setup-0.1.0.exe`** (version matches **`installer/ParentSiteGuardian.iss`**).

**Alternatives:**

- **`.\scripts\windows\Build-Installer.ps1`** — same as the npm script.
- **Staging only** (no Inno on PATH): **`npm run build:installer -- -SkipInno`** — populate **`installer/staging`** without producing the `.exe`.
- **Skip tests** while iterating: append **`-SkipTests`** to the script.

Full options and what the installer deploys: **[installer/README.md](installer/README.md)**.

## Troubleshooting

### `'dotnet' is not recognized` (Windows)

1. Install [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (the **SDK** row for Windows x64, not “Runtime” only).
2. Restart the terminal. If it still fails, sign out of Windows or reboot once so `PATH` updates everywhere.
3. In PowerShell, run `Get-Command dotnet` — it should resolve to a path under `Program Files\dotnet\`.

## Next steps

Follow [docs/Parent-Site-Guardian-Phase-1-Cursor-Implementation-Plan.md](docs/Parent-Site-Guardian-Phase-1-Cursor-Implementation-Plan.md) starting at **Sub-Phase 1** (SQLite, seed data, and APIs).

## License

Private / unspecified — add a `LICENSE` file when you decide terms.

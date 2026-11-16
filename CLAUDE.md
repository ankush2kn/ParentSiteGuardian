# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Parent Site Guardian: a local-only Windows parental-control MVP. It enforces daily time limits for groups of websites in Chrome and Edge. It has no cloud backend. All rules and usage are kept in a local SQLite file. It is an npm-workspaces monorepo with three apps:

- `apps/parent-site-guardian-service`: ASP.NET Core minimal API (.NET 10, EF Core + SQLite). It listens on `http://127.0.0.1:5050` and also runs as a Windows Service (`UseWindowsService`).
- `apps/admin-ui`: React 19 + Vite + TypeScript admin dashboard.
- `apps/browser-extension`: Manifest V3 extension (TypeScript, bundled with esbuild into `dist/`).

Product and design references: `docs/Parent-Site-Guardian-PRD.md` and `docs/Parent-Site-Guardian-Phase-1-Cursor-Implementation-Plan.md`.

## Commands

Run these from the repo root unless noted. Requires the .NET 10 SDK and Node 20+.

| Goal | Command |
|---|---|
| Install JS deps | `npm install` |
| Run API (dev) | `npm run dev:service` (wraps `dotnet run` via `scripts/dotnet-cli.cjs`, which checks that the SDK exists first) |
| Run admin UI (dev) | `npm run dev:admin` (Vite proxies `/api` and `/health` to :5050) |
| Extension watch build | `npm run dev:extension`, then "Load unpacked" `apps/browser-extension/dist` |
| Build everything | `npm run build` |
| Lint admin UI | `npm run lint -w admin-ui` |
| Typecheck extension | `npm run check -w parent-site-guardian-extension` |
| Apply EF migrations | `npm run db:update` (the service also auto-migrates on startup) |
| Windows installer | `npm run build:installer` (needs Inno Setup 6; see `installer/README.md`) |

**.NET tests** (xUnit v3; the test project is an Exe), run from `apps/parent-site-guardian-service`:

```powershell
dotnet test tests\ParentSiteGuardian.Service.Tests\ParentSiteGuardian.Service.Tests.csproj
# single test / class:
dotnet test tests\ParentSiteGuardian.Service.Tests\ParentSiteGuardian.Service.Tests.csproj --filter "FullyQualifiedName~UrlMatcherTests"
```

Stop a running API before testing or building; Windows file locks on the output DLLs cause failures. If Windows Application Control blocks the freshly built `ParentSiteGuardian.Service.exe`, run `dotnet bin/Debug/net10.0/ParentSiteGuardian.Service.dll` instead.

**Extension e2e** (Playwright, headed Chromium with the built extension loaded): `npm run test:e2e:extension`. The first time, run `npm run test:e2e:install-browsers -w parent-site-guardian-extension`. `youtube-usage.spec.ts` needs the API running on :5050 with the seeded DB, and skips itself otherwise. Single spec: `npx playwright test e2e/popup.spec.ts` from `apps/browser-extension` (build first).

**EF migrations**: `dotnet-ef` is a local tool (`.config/dotnet-tools.json`, run `dotnet tool restore`). Example: `dotnet ef migrations add <Name> --project apps/parent-site-guardian-service/ParentSiteGuardian.Service.csproj`. Migrations live in `Data/Migrations`.

Windows packaging and ops scripts are in `scripts/windows/`: Build-*, Install/Uninstall service or scheduled task, Backup/Reset database. See `docs/windows-phase1-install.md`.

## Architecture

**Data flow.** The extension background service worker (`src/background.ts`) calls the local API:

- `POST /api/decision` asks whether a URL is allowed. Results are cached for 4s (`DECISION_CACHE_MS`). A blocked tab is redirected to the extension's `blocked.html` with the reason, usage, and reset info in query params.
- `POST /api/usage/tick` reports active seconds, only for a focused window and a visible tab. The interval comes from `GET /api/extension-config`.
- The extension identifies itself with a device ID in `chrome.storage`. It defaults to the seeded device `20000000-0000-4000-8000-000000000001` (`src/config.ts`, `Data/WellKnownSeedIds.cs`).

**Service layout** (`apps/parent-site-guardian-service`):

- `Program.cs` sets up CORS (open, localhost only), runs startup migration and seeding (`LegacySqliteMigrationAdoption` → `MigrateAsync` → seed defaults → ensure the local device identity), then maps endpoint groups from `Api/*Endpoints.cs`.
- `Program.cs` also serves the built admin UI from `../admin-ui` relative to the content root at `/admin`, with an SPA fallback. This only happens when that folder exists, as in the published/installer layout.
- Layers: `Api/` (minimal-API endpoints + DTOs) → `Services/` (business logic) → `Repositories/` → `Data/AppDbContext`. DI registration lives in `Extensions/DependencyInjection.cs`. Add new services and repos there.
- URL → site-group matching: `Matching/UrlMatcher.cs` handles the pure rule types `domain` and `pathPrefix`. `Services/SiteGroupUrlResolver.cs` applies a device's rules on top of it.
- Usage and limits: `UsageAccountingService` sums `UsageEvent` durations per local day and compares them against `DailyLimit` plus active `Override` bonus seconds.
- Day boundaries and time zone come from `IGuardianTime`. Use `FixedGuardianTime` in tests; never call `DateTime.Now` directly in logic.
- Admin auth is a parent PIN (`ParentPinService`). Mutating `/api/admin/*` endpoints take `pin` in the request body. Every PIN check must go through `IParentPinService.CheckAsync`, which enforces a lockout that is saved in the database: 3 free wrong attempts, then waits of 1, 3, 9… minutes, capped at 24h, with checks serialized by a static semaphore. Failures map to 401 `invalid_pin` or 429 `pin_locked` with `retryAfterSeconds` (`Api/PinResults.cs`). Read endpoints are unauthenticated, since the service binds to loopback only.
- DB path: `%ProgramData%\ParentSiteGuardian\parent-site-guardian.db` by default. Override it with config `GuardianDatabase:Path`.
- `LegacySqliteMigrationAdoption` adopts old `EnsureCreated` databases into migration history. If you squash or rename `InitialCreate`, update its `InitialMigrationId`.

**Admin UI:**

- `src/lib/api.ts` is the API client. `src/lib/apiBase.ts` uses relative `/api` (proxied in dev, same-origin in prod), or `VITE_API_ORIGIN` when set.
- The production build uses `base: '/admin/'`.
- Global state, including the selected device and the unlocked PIN, lives in `providers/AppStateProvider.tsx`. Pages are in `src/pages/`.
- The `@repoDocs` alias lets the UI render markdown from the repo's `docs/`; `HardeningPage` uses it.

**Tests:**

- .NET tests use real SQLite temp files, not in-memory, via `tests/.../Support/TestAppDbContextFactory.cs`, which runs real migrations.
- Manual end-to-end steps are in `docs/integration-test-checklist.md`.

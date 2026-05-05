# Phase 1 — Windows install (local only)

Everything runs on the PC: **no cloud accounts** for this app.

- **API / admin (when installed):** `http://127.0.0.1:5050` and **`http://127.0.0.1:5050/admin/`** (admin UI is bundled next to the service and served by the API).
- **SQLite (default):** `%ProgramData%\ParentSiteGuardian\parent-site-guardian.db`
- **After `npm run build` (from source):** service binaries under `apps\parent-site-guardian-service\bin\Release\net10.0\`, admin UI in `apps\admin-ui\dist\`, extension in `apps\browser-extension\dist\`

### End users: Windows `.exe` installer

If you publish **`ParentSiteGuardian-Setup-*.exe`** (e.g. on GitHub Releases), testers only run that setup **as Administrator**. It installs a **self-contained** API (no separate .NET install), copies the admin UI and extension under Program Files, registers the **ParentSiteGuardianApi** service, and shows next steps for loading the extension. Details: **[installer/README.md](../installer/README.md)** and `npm run build:installer` on a **developer** machine.

---

## Get the project on this PC

**Repo root** means the **top-level project folder** — the directory that contains **`package.json`** (and folders like `apps\` and `docs\`). All `npm` commands in this guide are run from that folder in PowerShell or Command Prompt.

**Do you need it on the host?** For this Phase 1 guide, **yes.** You build the service, admin UI, and extension from source on the machine (or copy an already-built tree; the usual path is to have the repo there).

**Ways to get the files:**

- **Git (recommended):** install [Git for Windows](https://git-scm.com/download/win), then clone your copy of the repository, for example:
  - `cd %USERPROFILE%\CursorProjects`
  - `git clone <YOUR_REPO_URL> ParentGuardSite`
  - `cd ParentGuardSite` ← this folder is the **repo root**
- **ZIP:** on GitHub (or similar), use **Code → Download ZIP**, extract it, then `cd` into the extracted folder (the one that contains `package.json`).
- **USB / network copy:** copy the whole project folder from another PC; open a terminal in that folder before running `npm install`.

### Public repo: clone or ZIP with **no** other installs?

**From source only:** `git clone` or **Code → Download ZIP** gives **source**. Building that way still needs **.NET 10 SDK**, **Node.js**, and **Chrome or Edge** for the extension (Git is optional if you use a ZIP).

**Pre-built installer:** If maintainers attach **`ParentSiteGuardian-Setup-*.exe`** to **Releases**, end users do **not** need Git, Node, or the .NET SDK on the PC — only the installer and a browser to load the unpacked extension (see **`installer/README.md`**).

---

## Prerequisites

- **.NET 10 SDK** (SDK, not runtime-only)
- **Node.js 20+** and npm
- **Chrome and/or Edge** (for the extension)

Check:

```powershell
dotnet --version
node --version
npm --version
```

---

## Build

From the **repo root** (folder with `package.json`):

- `npm install`
- `npm run build` — builds the .NET service, admin UI, and extension in one step

Per piece, if you prefer:

- `npm run build:service`
- `npm run build:admin`
- `npm run build:extension`

---

## Run the API

- **While developing:** `npm run dev:service`
- **Smoke test:** open `http://127.0.0.1:5050/health` in the browser or run `Invoke-RestMethod http://127.0.0.1:5050/health`

Only run **one** listener on port **5050** (stop dev before trying auto-start, or the other way around).

---

## Optional: start the API with Windows

There is no npm command for Task Scheduler or Windows Services. Easiest path is the **PowerShell helpers** in `scripts\windows\` (see **Troubleshooting** for uninstall, custom folders, and manual `dotnet publish` if you do not want scripts).

- **Publish** the API to a fixed folder (includes `Run-GuardianService.cmd` for tasks):  
  `.\scripts\windows\Build-Service.ps1`  
  Default output: `dist\install\parent-site-guardian-service\`
- **Scheduled task — your user at logon:**  
  `.\scripts\windows\Install-GuardianScheduledTask.ps1 -Trigger AtLogOn`
- **Scheduled task — at machine boot (SYSTEM):** open PowerShell **as Administrator**, then:  
  `.\scripts\windows\Install-GuardianScheduledTask.ps1 -Trigger AtStartup`
- **Windows Service:** PowerShell **as Administrator**:  
  `.\scripts\windows\Install-GuardianWindowsService.ps1`  
  (Registers `dotnet.exe` + the published DLL.)

---

## Admin UI

- **Dev:** `npm run dev:admin` — open the URL Vite prints (often `http://localhost:5173`).
- **After a build:** `npm run preview -w admin-ui` — use the URL shown (often port **4173**).

---

## Browser extension (Chrome / Edge)

- Build or dev-build so `apps\browser-extension\dist` exists and contains `manifest.json` (`npm run build:extension` or `npm run dev:extension`).
- **Chrome:** `chrome://extensions` → **Developer mode** → **Load unpacked** → choose `apps\browser-extension\dist`.
- **Edge:** `edge://extensions` → **Developer mode** → **Load unpacked** → same folder.
- After code changes: rebuild or let the watcher run, then **Reload** on the extension card.

Optional policy / child-account ideas: [windows-hardening.md](windows-hardening.md).

---

## Secrets (stay local)

- No cloud API keys for Phase 1.
- Parent **PIN** lives only in local SQLite (`admin_config`, hashed).
- Optional: set **`GuardianDatabase__Path`** (machine or user env) to a full path to another `.db` file if you do not want the default under `%ProgramData%`.

---

## Troubleshooting and backups

### `dotnet` not recognized

Install the **.NET 10 SDK**, then **close and reopen** the terminal (and the IDE) so `PATH` updates.

### Port 5050 already in use

Stop the other process (second terminal, Windows Service, or scheduled task) so only one API instance runs.

### Publish folder and PowerShell script backups

- **Publish + task launcher:** `.\scripts\windows\Build-Service.ps1` → `dist\install\parent-site-guardian-service\` (adds `Run-GuardianService.cmd`). Same folder the install scripts use by default.
- **Other scripts** (if you prefer not to use npm for a step): `Build-Admin.ps1`, `Build-Extension.ps1`, `Build-All.ps1`.

**Manual publish** (no script), same output folder as the installer scripts expect:

```powershell
dotnet publish .\apps\parent-site-guardian-service\ParentSiteGuardian.Service.csproj -c Release -o .\dist\install\parent-site-guardian-service
```

Scheduled tasks use `Run-GuardianService.cmd`; that file is emitted when you use **`Build-Service.ps1`** (not by plain `dotnet publish` above).

### Auto-start: remove or change install folder

- Scheduled task: `.\scripts\windows\Uninstall-GuardianScheduledTask.ps1` (use an elevated shell if Windows created the task as **SYSTEM** and removal fails).
- Windows Service: `.\scripts\windows\Uninstall-GuardianWindowsService.ps1` (elevated).

Custom publish path (examples):

```powershell
.\scripts\windows\Install-GuardianScheduledTask.ps1 -Trigger AtStartup -PublishDirectory "C:\Apps\ParentSiteGuardian"
.\scripts\windows\Install-GuardianWindowsService.ps1 -PublishDirectory "C:\Apps\ParentSiteGuardian"
```

### SQLite backup

- Default file: `%ProgramData%\ParentSiteGuardian\parent-site-guardian.db` (Explorer: **Win+R** → `shell:Common AppData` → `ParentSiteGuardian`).
- **Script:** `.\scripts\windows\Backup-GuardianDatabase.ps1` — writes a timestamped copy under `%ProgramData%\ParentSiteGuardian\backups\` (honours `GuardianDatabase__Path` or `GUARDIAN_DATABASE_PATH` if set).
- **Manual:** copy the `.db` file when the service is stopped.

### SQLite reset (fresh schema + seed)

1. Stop the API (service, task, or dev process) so the file is not locked.
2. Run `.\scripts\windows\Reset-GuardianDatabase.ps1` (prompts for `RESET`; backs up first unless you pass `-SkipBackup`).

Next start applies EF migrations and first-run seed again.

### Related

- [integration-test-checklist.md](integration-test-checklist.md)

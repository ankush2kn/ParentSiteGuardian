# Windows installer (v1)

Produces **`dist\installer\ParentSiteGuardian-Setup-0.1.0.exe`** (version comes from `ParentSiteGuardian.iss`).

## Prerequisites (build machine only)

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)
- [Inno Setup 6](https://jrsoftware.org/isdl.php) (so `ISCC.exe` is available — default paths under `Program Files` or `Program Files (x86)`)

End users who run the compiled `.exe` **do not** need Node or the .NET SDK (the payload is **self-contained** `win-x64`).

## Build

From the **repository root**:

```powershell
npm run build:installer
```

Or:

```powershell
.\scripts\windows\Build-Installer.ps1
```

- **`staging` only** (no Inno): append `-SkipInno`
- **Skip tests** during iteration: `-SkipTests`

Staging output is under **`installer\staging\`** (`service`, `admin-ui`, `extension`). If Inno Setup is missing, the script finishes after staging and prints a warning.

## What it installs

| Location | Contents |
|----------|-----------|
| `{ProgramFiles}\Parent Site Guardian\service\` | Self-contained API (`ParentSiteGuardian.Service.exe` + runtime) |
| `{ProgramFiles}\Parent Site Guardian\admin-ui\` | Static admin UI (also served by the API at **`http://127.0.0.1:5050/admin/`**) |
| `{ProgramFiles}\Parent Site Guardian\extension\` | Load unpacked in Chrome / Edge |

Registers Windows service **`ParentSiteGuardianApi`**, starts it, and adds a Start-menu shortcut to open the admin URL.

Uninstall stops and removes the service, then removes files.

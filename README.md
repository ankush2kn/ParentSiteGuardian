# Parent Site Guardian

**Help manage screen time on selected websites in Chrome and Edge—on your Windows PC.**

You choose groups of sites and daily limits. A parent PIN helps protect your settings. **Your rules and usage data stay on this computer**—there is no Parent Site Guardian account or cloud service for this app.

---

## Use it on your PC (easiest path)

When an installer is available:

1. Open **[Releases](https://github.com/ankush2kn/ParentSiteGuardian/releases)** and download the latest **`ParentSiteGuardian-Setup-….exe`** file.
2. Run it and allow **Administrator** rights when Windows asks (needed to install the background service).
3. When setup finishes, open your **admin dashboard** in the browser—usually **`http://127.0.0.1:5050/admin/`** (the installer’s last screen repeats this).
4. Add the **browser extension**: in Chrome or Edge, go to the extensions page, turn on **Developer mode**, then **Load unpacked** and pick the **`extension`** folder under where you installed the app (often under **Program Files**—the installer text explains the exact path).

**No release file yet?** The project can also be run by people who build it from code (see *Building from source* below). That needs extra free tools—only if you’re comfortable with that.

**More detail:** [Windows install guide](docs/windows-phase1-install.md) · [What the installer does](installer/README.md)

---

## Before you start (everyone)

| Topic | Notes |
|--------|--------|
| **Computer** | Windows (this version is built for Windows). |
| **Browsers** | Chrome and/or Edge. Other browsers are not covered the same way in this phase. |
| **Who installs it** | A parent or guardian should install and keep the PIN private. |
| **Realistic expectations** | Technical controls can often be bypassed by someone very determined. Family rules, Windows account types, and optional [hardening ideas](docs/windows-hardening.md) still matter. |

---

## Building from source (developers)

Use this if you contribute code or you don’t have an installer download yet.

**You need:** [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), [Node.js 20+](https://nodejs.org/), and Git (or download the code as a ZIP from GitHub).

**Quick start**—from a folder where you cloned or unzipped the project:

```powershell
npm install
npm run dev:service
```

Use another terminal for the admin site and one for the extension, or see the links below. **Smoke check:** open `http://127.0.0.1:5050/health` in a browser—you should see a short “ok” message.

**Common commands**

| Goal | Command |
|------|--------|
| Run all parts while coding | `npm run dev:service` · `npm run dev:admin` · `npm run dev:extension` |
| Build production-style outputs | `npm run build` |
| Build the Windows **installer** (.exe) | `npm run build:installer` (needs [Inno Setup 6](https://jrsoftware.org/isdl.php) on the machine that builds it) |

**Where things live:** the local service is under `apps/parent-site-guardian-service`, the admin web app under `apps/admin-ui`, and the browser extension under `apps/browser-extension`.

**Docs for contributors:** [docs/README.md](docs/README.md) (product notes, phase plan, tests checklist) · [Windows install & scripts](docs/windows-phase1-install.md)

**Unit tests** (optional): from `apps/parent-site-guardian-service`, run  
`dotnet test tests\ParentSiteGuardian.Service.Tests\ParentSiteGuardian.Service.Tests.csproj`  
Stop the running API first if Windows says a file is in use.

**If `dotnet` isn’t recognized:** install the **.NET 10 SDK** (not “runtime only”), fully close and reopen your terminal, then try `dotnet --version` again.

---

## License

Private / unspecified — add a `LICENSE` file when you decide terms.

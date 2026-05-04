# Phase 1 integration test checklist

Use this list before a release or after meaningful changes. **Automated** items run in CI / locally with `dotnet test` (no guardian process required for service tests). **Manual** items need a human, Chrome or Edge, and usually the real extension + local API.

| Area | Automated (service) | Manual (extension / UI) |
|------|----------------------|---------------------------|
| Fresh install / first run | `FreshDatabaseBootstrapTests`, `DatabaseSeedServiceTests`, `LocalDeviceIdentityServiceTests` | Install service → first start creates DB under `%ProgramData%\…`; open admin → bootstrap loads. |
| Seeded YouTube + Video Search | `DatabaseSeedServiceTests`, `UsageAccountingVideoSearchTests` | `GET /api/admin/site-groups?deviceId=<seed device>` shows both groups and rules. |
| Active tab usage counting | `UsageAccountingServiceTests` (incl. cumulative ticks), `UsageAccountingHistoryTests`, extension `e2e/youtube-usage.spec.ts` (needs running API) | YouTube foreground tab: used seconds increase on `GET /api/usage/today` (see checklist steps below). |
| Inactive tab not counting | — (extension-only gating) | Open two tabs; background YouTube tab should **not** receive ticks (`background.ts` requires active + focused tab). |
| Browser unfocused not counting | — | Focus another app; YouTube visible but window unfocused → no ticks (`chrome.windows.get` `focused`). |
| Daily limit exhaustion | `UsageAccountingServiceTests.Decision_blocks_when_daily_limit_exhausted` | Browse matched site until block; `POST /api/decision` shows `allowed: false`. |
| Block page | — | After exhaustion, tab navigates to extension `blocked.html` with query params; PIN + override UI works. |
| Parent override | `AdminOverrideServiceTests`, `UsageAccountingServiceTests.Non_expired_override_increases_effective_limit` | On block page: valid PIN + minutes → `POST /api/admin/overrides` succeeds; browsing resumes until bonus used. |
| Admin UI rule update | `SiteGroupUrlResolverRuleChangeTests` (resolver after rule insert) | Admin: unlock PIN → Site groups → add rule → verify `POST /api/decision` for a URL matching only the new rule. |
| Usage history | `UsageAccountingHistoryTests` | Admin → History: table shows last 30 days; aligns with `GET /api/admin/usage/history`. |

---

## Prerequisites (manual + E2E)

1. **.NET 10** SDK; **Node 20+** for admin UI and extension tooling.
2. From repo root: `npm install`.
3. **Guardian API** on `http://127.0.0.1:5050` (default): `npm run dev:service` in a dedicated terminal.
4. **Extension**: `npm run dev:extension` → load unpacked `apps/browser-extension/dist` in Chrome or Edge (developer mode).

Default seeded **device id** (from README / tests): `20000000-0000-4000-8000-000000000001`.

---

## 1. Fresh install / first run

**Goal:** Empty DB path → migrate → seed once → stable local app id.

### Automated

- [ ] `dotnet test` — `FreshDatabaseBootstrapTests`, `DatabaseSeedServiceTests`, `LocalDeviceIdentityServiceTests`.

### Manual

- [ ] Stop service; point `GuardianDatabase:Path` in `appsettings.json` to a **new** file (or delete existing DB after backup).
- [ ] Start service once; confirm DB file created; `GET /health` returns `ok`.
- [ ] `GET /api/config/effective` returns `localDevice.appDeviceId` (GUID) and fingerprint fields.
- [ ] Second start: **same** `appDeviceId` as first (identity stable).

---

## 2. Seeded YouTube and Video Search groups

### Automated

- [ ] `DatabaseSeedServiceTests` — two groups, four rules, two daily limits; YouTube domain rules + Video Search `bing.com/videos`.
- [ ] `UsageAccountingVideoSearchTests` — `POST /api/decision` matches Video Search for a `bing.com/videos` URL.

### Manual

- [ ] Admin → **Site groups**: “YouTube” and “Video Search” present with expected rules.

---

## 3. Active tab usage counting

**Service behavior:** `POST /api/usage/tick` records `activeDurationSeconds` when URL matches a group.

### Automated

- [ ] `UsageAccountingServiceTests` (ticks, today, exhaustion, overrides).
- [ ] `UsageAccountingHistoryTests`.

### Manual

- [ ] With API running, open `https://www.youtube.com/` in **active, focused** tab (single window).
- [ ] Poll `GET /api/usage/today?deviceId=<seed>` — YouTube `usedSeconds` increases over ~30–60s (tick interval from `GET /api/extension-config`).

### Optional Playwright (API must be up)

```powershell
npm run dev:service   # separate terminal
npm run test:e2e:extension -w parent-site-guardian-extension
```

- [ ] `e2e/youtube-usage.spec.ts` — asserts YouTube used seconds increase while tab stays visible (skips if API down).

---

## 4. Inactive tab (not active) — not counting

**Implementation:** `background.ts` — tick only if tab is **active** in the focused window. `content.ts` — interval only while `document.visibilityState === 'visible'`.

### Automated

- [ ] Not practical in Phase 1 without a full browser driver for MV3; rely on code review + manual pass.

### Manual

- [ ] Open YouTube in **tab A** (active). Open `https://example.com` in **tab B** and select tab B.
- [ ] Wait several tick intervals; `usedSeconds` for YouTube should **not** increase while tab A is inactive.
- [ ] Select tab A again; usage should resume increasing.

---

## 5. Browser unfocused — not counting

**Implementation:** `background.ts` — `isWindowFocused(windowId)` must be true before recording tick.

### Automated

- [ ] None in Phase 1 (same as §4).

### Manual

- [ ] YouTube tab active and visible; **click another application** (e.g. File Explorer) so Chrome loses focus.
- [ ] Wait several intervals; YouTube usage should **not** increase.
- [ ] Click Chrome again; usage should resume.

---

## 6. Daily limit exhaustion

### Automated

- [ ] `UsageAccountingServiceTests.Decision_blocks_when_daily_limit_exhausted`.

### Manual

- [ ] Temporarily set a **very low** YouTube daily limit (Admin → Limits, or SQL) for the seed device; reload YouTube until blocked or `POST /api/decision` returns `matched: true`, `allowed: false`, `reason: daily_limit_exhausted`.

---

## 7. Block page

### Automated

- [ ] None (extension-hosted HTML + `chrome-extension://` origin).

### Manual

- [ ] When blocked, URL is `blocked.html` with `u`, `site`, `reason`, `used`, `base`, `ov`, `reset`, `sid` query params populated sensibly.
- [ ] Styles load; “Daily limit reached” and stats visible.
- [ ] CORS: block page can call `127.0.0.1:5050` APIs (already configured for extension origins).

---

## 8. Parent override

### Automated

- [ ] `AdminOverrideServiceTests` (PIN, minutes validation).
- [ ] `UsageAccountingServiceTests.Non_expired_override_increases_effective_limit`.

### Manual

- [ ] Configure parent PIN (`POST /api/admin/pin/setup` or Admin → Settings).
- [ ] On block page, submit PIN + **5 / 10 / 15 / 30** minutes; page reloads; YouTube works again until bonus consumed.
- [ ] Wrong PIN → override rejected (401 / UI error).

---

## 9. Admin UI rule update

### Automated

- [ ] `SiteGroupUrlResolverRuleChangeTests` — new `site_rules` row changes `POST /api/decision` outcome for a test URL.

### Manual

- [ ] Admin → unlock PIN → **Site groups** → add a **domain** rule for a disposable test host (e.g. `*.invalid`-style lab domain you control) or use a real low-risk host in a test profile.
- [ ] Confirm extension or `curl` decision for that URL matches the expected group.

---

## 10. Usage history

### Automated

- [ ] `UsageAccountingHistoryTests` — multi-day aggregation in `GET /api/admin/usage/history` data path (`IUsageAccountingService.GetHistoryAsync`).

### Manual

- [ ] Admin → **History**: dates and per-group minutes/usage align with ticks you performed over prior days (same machine timezone as service).

---

## Commands reference

```powershell
# Service unit tests (no running API)
cd apps\parent-site-guardian-service\tests\ParentSiteGuardian.Service.Tests
dotnet test

# Extension E2E (requires API on 5050)
cd ..\..\..
npm run test:e2e:extension -w parent-site-guardian-extension
```

---

## Why not automate the extension fully in Phase 1?

Chrome MV3 service workers suspend; ticks are driven from **content scripts** tied to visibility and **background** checks for focus/active tab. Reliable CI needs a headed browser, loaded extension, and stable network targets (`youtube.com`). Playwright **can** cover a subset (see `e2e/youtube-usage.spec.ts`) when the API is already running; broad UI automation is deferred in favor of **service-layer truth** for limits, overrides, seed, and history.

# Parent Site Guardian Phase 1 Cursor Implementation Plan

Tagline: Confident limits, calmly enforced.

## Purpose

This document is a step-by-step build plan for the Phase 1 local Windows MVP of Parent Site Guardian. It is written so it can be given to Cursor AI or another coding assistant.

The goal is to build a local-only Windows application that lets a parent define site groups and daily limits, tracks usage across Chrome and Edge, and blocks matching sites once the daily limit is exhausted.

## Phase 1 Scope

Build:

- Browser extension compatible with Chrome and Edge.
- Local Windows companion app/service with localhost API.
- SQLite local database.
- Parent admin UI.
- Blocking page.
- Parent PIN and temporary override.
- Local usage history.

Do not build:

- Cloud dashboard.
- iPad app.
- Multi-device shared budgets.
- Full anti-tamper system.
- TLS interception.
- Router integration.

## Recommended Tech Stack

Use a stack that is easy to run on Windows and easy for Cursor to modify.

Recommended:

- Backend/service: .NET 10 minimal API
- Local database: SQLite
- Admin UI: React + Vite + TypeScript
- Browser extension: Manifest V3, TypeScript
- Packaging later: Windows service or tray app installer

Alternative acceptable backend:

- Node.js + Fastify + SQLite

This plan assumes .NET 10 for the local API because it is native-friendly on Windows and can later become a Windows service.

## Local Storage: Why SQLite

SQLite is the local database for Phase 1. It stores parent configuration and usage history on the child's Windows laptop without requiring a cloud account or a separate database server.

SQLite should store:

- Children and local device enrollment.
- Site groups and matching rules.
- Daily limits.
- Usage events and daily summaries.
- Parent PIN hash.
- Temporary overrides.
- Local settings.

SQLite is not something the parent installs separately. The app includes the SQLite library through its backend package dependencies, and the service creates a local `.db` file on first run.

Recommended database location for a Windows service:

```text
C:\ProgramData\ParentSiteGuardian\parent-site-guardian.db
```

Alternative location for an early per-user tray-app prototype:

```text
%LOCALAPPDATA%\ParentSiteGuardian\parent-site-guardian.db
```

Use `C:\ProgramData` for the real Phase 1 target because the data belongs to the managed device, not just one interactive app session.

## Repository Structure

Create a monorepo:

```text
parent-site-guardian/
  README.md
  docs/
    PRD.md
    Phase-1-Implementation-Plan.md
  apps/
    parent-site-guardian-service/
      src/
      tests/
    admin-ui/
      src/
      tests/
    browser-extension/
      src/
      public/
      tests/
  packages/
    url-matcher/
      src/
      tests/
  scripts/
```

If using .NET for the service and TypeScript for the UI/extension, duplicate simple URL matching tests across service and TypeScript until a shared cross-language package is justified.

## Core Data Model

Use SQLite tables like:

```sql
children (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  created_at TEXT NOT NULL
)

devices (
  id TEXT PRIMARY KEY,
  child_id TEXT NOT NULL,
  name TEXT NOT NULL,
  machine_fingerprint TEXT,
  windows_user_sid TEXT,
  created_at TEXT NOT NULL
)

site_groups (
  id TEXT PRIMARY KEY,
  child_id TEXT NOT NULL,
  name TEXT NOT NULL,
  color TEXT,
  created_at TEXT NOT NULL
)

site_rules (
  id TEXT PRIMARY KEY,
  site_group_id TEXT NOT NULL,
  pattern TEXT NOT NULL,
  pattern_type TEXT NOT NULL,
  created_at TEXT NOT NULL
)

daily_limits (
  id TEXT PRIMARY KEY,
  device_id TEXT NOT NULL,
  site_group_id TEXT NOT NULL,
  limit_seconds INTEGER NOT NULL,
  created_at TEXT NOT NULL
)

usage_events (
  id TEXT PRIMARY KEY,
  child_id TEXT NOT NULL,
  device_id TEXT NOT NULL,
  site_group_id TEXT NOT NULL,
  browser TEXT NOT NULL,
  url TEXT NOT NULL,
  started_at TEXT NOT NULL,
  ended_at TEXT NOT NULL,
  duration_seconds INTEGER NOT NULL
)

overrides (
  id TEXT PRIMARY KEY,
  device_id TEXT NOT NULL,
  site_group_id TEXT NOT NULL,
  extra_seconds INTEGER NOT NULL,
  expires_at TEXT NOT NULL,
  created_at TEXT NOT NULL
)

settings (
  key TEXT PRIMARY KEY,
  value TEXT NOT NULL
)
```

## URL Matching Rules

Support these pattern types in Phase 1:

- `domain`: matches a domain and its subdomains.
- `pathPrefix`: matches a domain plus path prefix.

Examples:

- `domain:youtube.com` matches `youtube.com`, `www.youtube.com`, `m.youtube.com`.
- `domain:youtu.be` matches `youtu.be`.
- `domain:youtube-nocookie.com` matches `youtube-nocookie.com`.
- `pathPrefix:bing.com/videos` matches `https://www.bing.com/videos`, `https://bing.com/videos/search?q=...`.

Normalize:

- Lowercase host.
- Strip leading `www.` for comparison only where appropriate.
- Treat `http` and `https` the same.
- Ignore query string for matching.
- Reject invalid URLs safely.

## Local API Contract

The browser extension talks to the local service over `http://127.0.0.1:<port>`.

Suggested endpoints:

```text
GET /health
GET /api/config/effective
POST /api/usage/tick
POST /api/decision
GET /api/usage/today
GET /api/usage/history?days=30
POST /api/admin/pin/verify
POST /api/admin/site-groups
PUT /api/admin/site-groups/{id}
DELETE /api/admin/site-groups/{id}
POST /api/admin/limits
PUT /api/admin/limits/{id}
POST /api/admin/overrides
```

`POST /api/usage/tick` request:

```json
{
  "deviceId": "device-id",
  "browser": "chrome",
  "url": "https://www.youtube.com/watch?v=abc",
  "startedAt": "2026-05-04T16:10:00-07:00",
  "endedAt": "2026-05-04T16:10:05-07:00",
  "activeDurationSeconds": 5
}
```

`POST /api/decision` response:

```json
{
  "matched": true,
  "siteGroupId": "group-youtube",
  "siteGroupName": "YouTube",
  "allowed": false,
  "usedSeconds": 2700,
  "limitSeconds": 2700,
  "remainingSeconds": 0,
  "reason": "daily_limit_exhausted"
}
```

## Sub-Phase 0: Project Scaffolding

Goal:

- Create the repo structure and runnable skeletons.

Cursor prompt:

```text
Create a monorepo for a Windows parental-control MVP named Parent Site Guardian.

Use this structure:
- apps/parent-site-guardian-service: .NET 10 minimal API
- apps/admin-ui: React + Vite + TypeScript
- apps/browser-extension: Chrome/Edge Manifest V3 extension with TypeScript
- docs

Add a root README with setup instructions.
Do not implement business logic yet. Add health checks and placeholder pages so each app can run.
Use clear scripts for local development.
```

Testing milestone:

- Service starts and `GET /health` returns OK.
- Admin UI starts and renders a placeholder dashboard.
- Browser extension builds without errors.

## Sub-Phase 1: Service Database and Seed Data

Goal:

- Add SQLite persistence and seed a default child/device/site groups.

Cursor prompt:

```text
In apps/parent-site-guardian-service, add SQLite persistence for Parent Site Guardian's Phase 1 data model.

Implement tables for children, devices, site_groups, site_rules, daily_limits, usage_events, overrides, and settings.

On first run, seed:
- Child: "Default Child"
- Device: "This Windows laptop"
- Site group: "YouTube" with domain rules youtube.com, youtu.be, youtube-nocookie.com
- Site group: "Video Search" with pathPrefix rule bing.com/videos
- Daily limit: YouTube 45 minutes
- Daily limit: Video Search 15 minutes

Add repository/service classes and unit tests for basic CRUD and first-run seed behavior.
```

Testing milestone:

- Delete local database, start service, confirm seed data exists.
- Unit tests pass.

## Sub-Phase 2: URL Matching

Goal:

- Correctly match URLs to site groups.

Cursor prompt:

```text
Implement URL matching in apps/parent-site-guardian-service.

Support pattern types:
- domain
- pathPrefix

Rules:
- domain:youtube.com matches youtube.com and subdomains such as www.youtube.com and m.youtube.com.
- domain:youtu.be matches youtu.be and its subdomains.
- pathPrefix:bing.com/videos matches bing.com/videos and www.bing.com/videos, including deeper paths and query strings.
- Matching should be case-insensitive for host.
- Invalid URLs should return no match, not throw.

Add a focused unit test suite with YouTube, youtu.be, youtube-nocookie.com, bing.com/videos, nonmatching bing.com/search, and invalid URL cases.
```

Testing milestone:

- URL matcher tests pass.
- Manual test endpoint or unit tests prove matching behavior.

## Sub-Phase 3: Usage Accounting and Decisions

Goal:

- Record usage ticks and compute remaining daily budget.

Cursor prompt:

```text
Add usage accounting to apps/parent-site-guardian-service.

Implement:
- POST /api/usage/tick
- POST /api/decision
- GET /api/usage/today

When a tick URL matches a site group, store a usage event with child_id, device_id, site_group_id, browser, URL, start/end timestamps, and duration.
Ignore unmatched URLs.

Daily usage should be calculated by local calendar day.
Decision should return matched, allowed, usedSeconds, limitSeconds, remainingSeconds, and reason.

Include override seconds if a non-expired override exists.
Add tests for daily reset, limit exhaustion, unmatched URLs, and override allowance.
```

Testing milestone:

- Simulate 45 minutes of YouTube ticks and confirm YouTube becomes blocked.
- Simulate Bing search and confirm it does not count unless path is `/videos`.
- Confirm next-day usage resets.

## Sub-Phase 4: Browser Extension Tracking

Goal:

- Track active browsing time from Chrome and Edge.

Cursor prompt:

```text
Build the Manifest V3 browser extension in apps/browser-extension.

The extension should:
- Track the active tab URL.
- Count time only when the browser window is focused and the tab is active.
- Send usage ticks to the local Parent Site Guardian service every 5 seconds while on a matched/possibly managed URL.
- Ask POST /api/decision before allowing a page to remain visible.
- Redirect to an extension block page when the decision says allowed=false.
- Handle local service unavailable gracefully by showing a small non-blocking warning in the extension popup.

Add a popup showing service status and today's remaining time if available.
Use TypeScript and keep permissions minimal.
```

Testing milestone:

- Load unpacked extension in Chrome.
- Visit YouTube and confirm ticks appear in service logs/database.
- Switch tabs and confirm usage stops.
- Minimize or unfocus browser and confirm usage stops.
- Exhaust a small test limit and confirm the block page appears.

## Sub-Phase 5: Block Page and Parent Override

Goal:

- Provide a friendly blocked experience and parent override flow.

Cursor prompt:

```text
Add a polished extension block page.

The block page should show:
- Site group name
- Today's used time
- Daily limit
- When time resets
- A parent override form with PIN and extra time choices: 5, 10, 15, 30 minutes

In the service, implement:
- Parent PIN setup on first run
- POST /api/admin/pin/verify
- POST /api/admin/overrides

Store the PIN securely enough for local MVP using a salted password hash, not plaintext.
After successful override, the block page should retry the original URL.
```

Testing milestone:

- Exhaust YouTube limit.
- Block page appears.
- Wrong PIN fails.
- Correct PIN grants 10 minutes.
- YouTube becomes available again until override time is consumed or expires.

## Sub-Phase 6: Parent Admin UI

Goal:

- Let the parent manage site groups and limits locally.

Cursor prompt:

```text
Build the parent admin UI in apps/admin-ui.

Views:
- Dashboard: today's usage by site group, remaining time, and current status.
- Site Groups: create/edit/delete site groups and rules.
- Limits: set daily limit per site group for the local device.
- History: show daily usage summaries for the last 30 days.
- Settings: parent PIN setup/change and device identity display.

Require parent PIN before allowing admin changes.
Use a clean practical UI. Avoid a marketing landing page. The app should open directly to the dashboard.
```

Testing milestone:

- Parent can create a group named "All Video."
- Parent can add `youtube.com`, `youtu.be`, and `bing.com/videos`.
- Parent can set a 30-minute limit.
- Extension enforces the new group after config refresh.

## Sub-Phase 7: Local Device Identity

Goal:

- Give the device a stable local identity suitable for future cloud enrollment.

Cursor prompt:

```text
Add local device identity to the Parent Site Guardian service.

On first run:
- Generate a stable app device ID and store it in SQLite/settings.
- Capture best-effort machine fingerprint information available without requiring admin privileges.
- Capture current Windows username and SID if available.

Expose this on GET /api/config/effective and the admin Settings page.

Do not use MAC address as the primary identifier. If MAC addresses are collected, display them only as optional labels.
```

Testing milestone:

- Restart service and confirm device ID stays the same.
- Admin UI displays friendly device identity.

## Sub-Phase 8: Browser Policy Hardening Notes

Goal:

- Document and optionally script Chrome/Edge policy setup.

Cursor prompt:

```text
Add documentation under docs/windows-hardening.md explaining how a parent can make Phase 1 harder to bypass.

Include:
- Use a standard Windows child account, not administrator.
- Force-install the Parent Site Guardian extension in Chrome and Edge using browser policies where feasible.
- Disable incognito/private mode or ensure the extension is allowed there.
- Limit installation of unsupported browsers.
- Keep Microsoft Family Safety enabled for broad device/app controls if desired.

Do not implement destructive system changes automatically. Provide optional scripts only if they are clearly documented and safe.
```

Testing milestone:

- Documentation exists and is understandable by a technical parent.
- No hardening scripts run automatically.

## Sub-Phase 9: Integration Testing

Goal:

- Verify the MVP as a parent would use it.

Cursor prompt:

```text
Create an integration test checklist and automated tests where practical.

Cover:
- Fresh install/first run.
- Seeded YouTube and Video Search groups.
- Active tab usage counting.
- Inactive tab not counting.
- Browser unfocused not counting.
- Daily limit exhaustion.
- Block page.
- Parent override.
- Admin UI rule update.
- Usage history.

Add any automated service tests that are straightforward. For browser-extension behavior, document manual test steps if automation is too expensive for Phase 1.
```

Testing milestone:

- Run service tests.
- Manually verify Chrome.
- Manually verify Edge.
- Confirm a short 1-minute limit blocks reliably.

## Sub-Phase 10: Packaging for a Family Laptop

Goal:

- Make the app practical to install on the child's Windows laptop.

Cursor prompt:

```text
Prepare Phase 1 for installation on a Windows laptop.

Add documentation and scripts for:
- Building the .NET service.
- Building the admin UI.
- Building the browser extension.
- Running the service at login or as a Windows service.
- Loading or installing the extension in Chrome and Edge.
- Backing up and resetting the local SQLite database.

Do not require cloud accounts.
Keep secrets local.
```

Testing milestone:

- Clean machine setup instructions work from start to finish.
- Service restarts after reboot/login.
- Extension reconnects to service after restart.

## Suggested Cursor Workflow

Use one sub-phase at a time.

After each sub-phase:

- Run tests.
- Commit changes.
- Manually test the milestone.
- Do not start the next sub-phase until the current one works.

Suggested command to Cursor:

```text
Work only on Sub-Phase N from Phase-1-Cursor-Implementation-Plan.md.
Keep the implementation minimal and testable.
When done, summarize files changed, tests added, tests run, and any manual verification still needed.
```

## Manual Testing Script for MVP

Use this final smoke test:

1. Start the Parent Site Guardian service.
2. Open admin UI.
3. Set YouTube limit to 1 minute.
4. Load unpacked extension in Chrome.
5. Visit `https://www.youtube.com`.
6. Keep Chrome focused for about 60 seconds.
7. Confirm block page appears.
8. Try `https://youtu.be/...` and confirm it is also blocked.
9. Visit `https://www.bing.com/search?q=test` and confirm it is not counted.
10. Visit `https://www.bing.com/videos` and confirm it is counted if the rule exists.
11. Enter parent PIN and grant 5 extra minutes.
12. Confirm YouTube works again.
13. Repeat basic check in Edge.

## Important Engineering Constraints

- Do not collect unrelated browsing history.
- Do not store parent PIN in plaintext.
- Do not count time for inactive tabs.
- Do not block unmatched school sites.
- Do not break Chrome school account usage.
- Do not depend on MAC address as the primary identity.
- Do not require internet access for Phase 1 enforcement.
- Do not promise bypass-proof enforcement in Phase 1.

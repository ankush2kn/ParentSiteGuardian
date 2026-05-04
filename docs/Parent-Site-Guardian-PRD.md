# Parent Site Guardian PRD

Tagline: Confident limits, calmly enforced.

## Product Summary

Parent Site Guardian is a parental-control application that helps a parent set daily time limits for groups of websites on a child's Windows laptop. The first use case is managing YouTube and video-search time across Chrome and Edge, while still allowing the child to use required school websites and accounts.

The long-term product may support multiple children, multiple devices, a cloud parent dashboard, and eventually iPad enforcement through Apple's Screen Time APIs. The first release should prove that local Windows tracking and enforcement are useful and reliable before adding cloud sync or iPad support.

## Problem

Existing parental-control tools do not solve this family's specific need:

- Microsoft Family Safety is useful for Windows screen time, app limits, Edge filtering, and child accounts, but it does not provide practical per-website time budgets across Chrome and Edge.
- Google Family Link can block sites or apps in some cases, but it does not provide flexible daily time limits by custom website group for a Windows laptop.
- Google WiFi / Nest WiFi can pause devices, schedule internet downtime, and block adult content, but it does not reliably provide per-site usage budgets such as "45 minutes for YouTube per day."
- School websites sometimes require Chrome, so forcing Edge-only browsing is not acceptable.

The parent needs a way to say:

- "YouTube domains get 45 minutes per day."
- "Bing video search gets 15 minutes per day."
- "These domains may be grouped together or split into separate groups."
- "Each child and each device can have different rules."

## Goals

- Let a parent define site groups made of domains and URL path patterns.
- Track active browsing time for each site group across Chrome and Edge on a Windows laptop.
- Enforce daily limits by blocking or redirecting pages once the budget is exhausted.
- Support multiple children and devices in the data model, even if Phase 1 runs locally on one device.
- Provide a local parent admin experience for configuring groups, limits, and overrides.
- Store local usage history so the parent can inspect recent usage without a cloud account.
- Design the data model so it can later sync to a cloud dashboard.

## Non-Goals for Phase 1

- No iPad enforcement.
- No shared budget across devices.
- No cloud parent website.
- No remote rule editing.
- No production-grade anti-tamper system.
- No attempt to inspect encrypted HTTPS traffic at the network layer.
- No deep content classification, such as distinguishing educational YouTube videos from entertainment videos.
- No bypass-proof guarantee against a technically skilled child with administrator access.

## Target Users

Primary user:

- Parent or guardian who manages a child's device access.

Secondary user:

- Child using a Windows laptop for school, web browsing, and entertainment.

Future users:

- Multiple children in one household.
- Parent managing Windows laptops and iPads from one dashboard.

## Product Concepts

### Child

A child represents a person whose browsing limits are managed.

Example:

- Alex
- Sam

### Device

A device represents a managed laptop, tablet, or future phone assigned to a child.

For Windows, the device should have:

- App-generated enrollment ID.
- Windows machine GUID or similar local identifier.
- Windows account SID or username as a secondary identifier.
- Friendly name, such as "Alex school laptop."
- Optional MAC address as a label only.

MAC address should not be the primary identity because it can change, especially with randomized/private MAC behavior and network adapter changes.

### Site Group

A site group is a parent-defined bucket of websites that share one limit.

Examples:

- YouTube: `youtube.com`, `www.youtube.com`, `m.youtube.com`, `youtu.be`, `youtube-nocookie.com`
- Video Search: `bing.com/videos`, `www.bing.com/videos`
- All Video: YouTube plus Bing video paths

### Rule

A rule assigns a daily time budget to a site group for a child/device.

Examples:

- Alex laptop: YouTube, 45 minutes/day
- Alex laptop: Video Search, 15 minutes/day
- Sam laptop: All Video, 30 minutes/day

### Usage Event

A usage event records observed active browsing time.

Suggested fields:

- Timestamp start
- Timestamp end
- Device ID
- Child ID
- Browser
- URL
- Matched site group
- Active duration seconds

### Daily Summary

A daily summary aggregates usage per child, device, site group, and local date.

## Phase Roadmap

### Phase 1: Local Windows MVP

Build a local-only Windows guardian app for Chrome and Edge.

Capabilities:

- Parent configures children, local device, site groups, and daily limits.
- Browser extension observes active tab URLs in Chrome and Edge.
- Local service stores usage, computes remaining budget, and returns allow/block decisions.
- Extension blocks pages when a site group has no remaining time.
- Local admin UI shows today's usage and recent history.
- Parent PIN allows temporary override.

Success criteria:

- Parent can create a YouTube site group with `youtube.com` and `youtu.be`.
- Parent can create another group for `bing.com/videos`.
- Parent can combine all three into one group if desired.
- Time accrues only when the target tab is active and the browser window is focused.
- Once a budget is exhausted, Chrome and Edge both show a blocked page for matching sites.
- Usage resets by local calendar day.

### Phase 2: Cloud Parent Dashboard

Add a secure hosted web dashboard and cloud data store.

Capabilities:

- Parent login.
- Children and devices registry.
- Remote site-group and daily-limit configuration.
- Device syncs latest rules from cloud.
- Device uploads usage summaries and selected usage events.
- Dashboard shows today's usage, remaining time, and recent history.

Success criteria:

- Parent can open a website from another device and see usage for the Windows laptop.
- Parent can change a limit remotely and the Windows laptop applies it after sync.
- The local Windows app continues enforcing the last known rules if internet is unavailable.

### Phase 3: Multi-Device Shared Budgets

Allow one daily site-group budget to be shared across multiple devices for the same child.

Example:

- Alex gets 45 minutes/day for YouTube across laptop and iPad combined.

Capabilities:

- Cloud service becomes source of truth for shared budgets.
- Devices reserve or report usage frequently enough to avoid major overrun.
- Conflict handling for offline use.
- Parent dashboard shows usage split by device.

Success criteria:

- If the Windows laptop uses 30 of 45 minutes, the iPad only has about 15 minutes remaining.
- If one device is offline, it enforces a conservative local allowance and reconciles later.

### Phase 4: iPad App

Build an iPad companion app using Apple's Screen Time APIs.

Capabilities:

- Parent authorizes the app through Apple Family Sharing / Screen Time.
- App uses Family Controls, Device Activity, and Managed Settings.
- Parent selects apps, categories, or web domains through Apple's privacy-preserving picker.
- iPad reports usage to the same cloud budget system.
- iPad shields selected apps or web domains when time is exhausted.

Success criteria:

- iPad can enforce web-domain or app limits using Apple-approved APIs.
- iPad usage appears in the same parent dashboard as Windows laptop usage.

## Recommended Phase 1 Implementation

Phase 1 should be browser-extension enforced with a local Windows companion service.

Components:

- Browser extension for Chrome and Edge.
- Local Windows service or tray app exposing a localhost API.
- Local SQLite database.
- Local parent admin UI.
- Shared rule-matching library or equivalent consistent matching logic.

The extension handles visibility and active-tab tracking. The service handles durable storage, rule evaluation, daily budgets, and parent configuration.

## Why Not Router-Only Enforcement?

Router-only enforcement is not suitable for this product.

Reasons:

- Modern HTTPS prevents the router from seeing full paths like `/videos` reliably.
- Router controls may see domains but not enough context.
- Router enforcement only works on the home network.
- A child may use another network, hotspot, VPN, or offline cached content.
- Google WiFi does not provide custom per-site daily time budgets.
- Router-level rules cannot easily distinguish children using shared devices or browser profiles.

Google WiFi remains useful for coarse bedtime/homework internet pauses, but not as the primary product mechanism.

## Why Not Microsoft Family Safety Only?

Microsoft Family Safety is useful but insufficient for this need.

Reasons:

- Web filtering is strongest in Edge.
- Chrome is required for some school workflows.
- Website-specific time limits are not the core supported model.
- Blocking Chrome entirely would interfere with school use.
- The parent needs custom grouped budgets across sites and browsers.

Microsoft Family Safety can still be used alongside Parent Site Guardian for Windows account setup, app limits, and administrator restrictions.

## Why Not Google Family Link Only?

Google Family Link is not enough for this Windows laptop use case.

Reasons:

- It is stronger on Android and ChromeOS than on general Windows browsing.
- It may block websites but does not provide the desired flexible daily site-group budgets.
- It does not manage Edge and Chrome usage on Windows in the desired cross-browser way.

## Why Not Browser Extension Only?

A browser extension alone is fast to build but weak as the whole product.

Risks:

- User may disable or remove the extension.
- User may install another browser.
- Incognito/private browsing may bypass tracking unless disabled by policy.
- The extension may lose local state or be manipulated.

Mitigations:

- Use Chrome and Edge enterprise policies to force-install the extension where possible.
- Use Windows standard child accounts, not administrator accounts.
- Use a local service for authoritative storage and decisions.
- Block or monitor unsupported browsers in a later hardening phase.

## Why Not System-Level Network Filtering First?

System-level filtering through a Windows Filtering Platform driver, local proxy, custom DNS, or TLS inspection would be more complex than needed for an MVP.

Risks:

- Higher implementation complexity.
- Driver signing or certificate installation burden.
- Greater chance of breaking school sites.
- Harder debugging for a parent.
- HTTPS/TLS inspection creates privacy and security concerns.

This may become useful later for tamper resistance, but it should not be Phase 1.

## Why Not iPad in Phase 1?

iPad support requires a separate Apple-native implementation using Screen Time APIs.

Reasons:

- Windows software cannot enforce iPad behavior.
- Apple does not allow arbitrary background monitoring or blocking outside its parental-control frameworks.
- Family Controls entitlements may require Apple approval.
- Cross-device budgets require cloud sync first.

iPad should come after Windows tracking and cloud budget logic are proven.

## Privacy Principles

- Track only what is needed to enforce parent-defined site groups.
- Avoid collecting full browsing history for unrelated sites.
- Prefer storing matched site group, normalized host/path, and duration rather than every full URL.
- Keep local data local in Phase 1.
- In cloud phases, upload summaries by default and full events only when necessary for parent-facing history.
- Do not inspect page content.
- Do not capture keystrokes, screenshots, cookies, account data, or private messages.

## Security and Tamper Resistance

Phase 1 should be honest about its limits.

Minimum safeguards:

- Child Windows account should be standard, not administrator.
- Parent admin UI should require a PIN.
- Local service should own the SQLite database.
- Extension should ask the local service for decisions instead of deciding everything itself.
- Extension should be force-installed by Chrome/Edge policy where feasible.
- Browser private/incognito mode should be disabled or extension should be allowed in incognito only if acceptable.

Future safeguards:

- Signed installer.
- Windows service running under a restricted account.
- Watchdog process.
- Browser allowlist.
- Unsupported browser detection.
- Cloud audit trail for uninstall or stopped-service events.

## Open Product Questions

- Should usage count when audio/video is playing in a background tab, or only when the tab is active?
- Should the parent allow bonus time per group?
- Should there be school-hour exceptions?
- Should certain YouTube channels or URLs be allowed without counting?
- Should the block page allow the child to request more time?
- How much usage history should be stored locally?
- Should parent configuration be local-only in Phase 1, or password-protected through Windows authentication?

## Suggested MVP Defaults

- Count usage only when the browser window is focused and the matching tab is active.
- Use daily budgets based on the laptop's local timezone.
- Reset budgets at midnight local time.
- Store 30 days of local daily summaries.
- Store detailed matched usage events for 7 days.
- Parent PIN required for admin changes and overrides.
- Override options: 5, 10, 15, 30 minutes.
- Start with Chrome and Edge only.

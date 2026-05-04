# Windows hardening for Phase 1 (Parent Site Guardian)

This guide is for **parents or guardians** who want to make Phase 1 harder to bypass on a family Windows PC. Parent Site Guardian is a **local MVP**: it depends on the browser extension talking to the guardian service and on **normal Windows controls** you configure yourself.

**Nothing in this repository automatically changes your system.** Any optional snippets below are **documentation and copy-paste examples** only. You apply policies through Microsoft Intune, Group Policy, or the browser vendor’s documented mechanisms. Test on a spare profile or device first when possible.

---

## 1. Use a standard Windows child account (not Administrator)

**Why:** An administrator can disable extensions, edit policies, install other browsers, or change the guardian service. A **standard user** account cannot do most of that without an admin password.

**What to do:**

1. Create or use a dedicated **Microsoft account** or **local account** for the child.
2. In **Settings → Accounts → Family & other users** (or **Other users**), ensure the child account type is **Standard User**, not Administrator.
3. Keep the **parent** account as Administrator, and do not share that password.

**Verify:** Sign in as the child → **Settings → Accounts** → the account should not say Administrator. If the child needs elevation for school software, prefer **per-app** installs run by the parent rather than promoting the whole account.

---

## 2. Force-install the extension in Chrome and Edge (browser policies)

Enterprise-style **browser policies** can require the Parent Site Guardian extension to be present and harder to remove. This is the most effective technical layer alongside a non-admin child account.

**Feasibility:**

- **Managed Windows** (school or work PC): IT usually deploys Chrome/Edge policies via Intune or Group Policy. Ask IT; do not use personal-policy hacks on a device you do not own.
- **Personal / family PC:** You can still apply policies using documented methods:
  - **Google Chrome:** [Chrome Enterprise: Windows quick start](https://support.google.com/chrome/a/answer/187202) — registry keys under policy paths described in Google’s documentation (e.g. `Software\Policies\Google\Chrome` on Windows).
  - **Microsoft Edge:** [Configure Microsoft Edge policy settings on Windows](https://learn.microsoft.com/en-us/deployedge/configure-microsoft-edge) — similar policy trees under `Software\Policies\Microsoft\Edge`.

**Useful policy concepts (names vary by version; always confirm in vendor docs):**

- **Force-install an extension** so it is pinned/required — Chrome and Edge both support policies such as extension install allowlists / force-install lists (e.g. `ExtensionInstallForcelist` and related settings in vendor policy lists).
- **Extension ID:** Use the extension ID from your build or the store listing when you construct policy values (format is documented by Google/Microsoft).

**Official references (read before editing the registry or policy):**

- Chrome: [List of Chrome policies](https://chromeenterprise.google/policies/) — search for “extension install”.
- Edge: [Microsoft Edge browser policies](https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies) — search for “Extension”.

**No automated installer here:** Policy deployment is environment-specific. Wrong registry edits can confuse the browser; always back up or use the vendor’s ADMX templates where available.

---

## 3. Incognito / InPrivate and the extension

**Risk:** If the child can use **Incognito** or **InPrivate** without the extension, or with extensions disabled, limits enforced by the extension may not apply.

**Options (choose what matches your comfort and vendor support):**

1. **Disable guest / incognito-style browsing** where policies allow (Chrome and Edge document policies for disabling Incognito/InPrivate in managed scenarios — check current policy names in the policy lists linked above).
2. **Allow the extension in Incognito/InPrivate** (Chrome: per-extension “Allow in Incognito”; for managed installs, see Chrome policies for extension behavior). That way private mode still runs the guardian, but you must accept the privacy tradeoffs of extensions in private windows.
3. **Combine both:** Restrict who can open private windows and ensure the force-installed extension is allowed to run there if private mode stays available.

Again, verify exact policy keys in the current Chrome/Edge documentation; names change over time.

---

## 4. Limit installation of unsupported browsers

Phase 1 only covers browsers where you install and enforce the **Parent Site Guardian** extension (today: **Chrome and Edge** in this repo). Other browsers are an easy bypass if the child can install and use them.

**Non-destructive approaches:**

- **Standard user account** (section 1): blocks many per-user installs without an admin password.
- **Microsoft Family Safety** (section 5): can block specific apps or limit screen time at the device level for child accounts.
- **Allowlist philosophy:** Prefer “only install browsers we support and manage” rather than trying to delete every alternative by script.

**Advanced (optional, admin-only):** Windows has **WDAC** (Windows Defender Application Control) and legacy **AppLocker** for application control. These are powerful, easy to misconfigure, and can lock you out of the PC. They are **not** scripted in this repo. If you use them, follow Microsoft’s official guides and test on a non-production device first.

---

## 5. Microsoft Family Safety (optional broad layer)

**Family Safety** can complement the extension with **screen time**, **app and game limits**, **content filters**, and **activity reporting** for child Microsoft accounts.

- It does **not** replace URL-level site groups inside Chrome/Edge the way this project’s extension does, but it raises the cost of bypassing “only the browser” by limiting other apps or total device use.
- Enable and tune it in the **Microsoft Family Safety** app or [account.microsoft.com/family](https://account.microsoft.com/family) as documented by Microsoft.

---

## Optional: example policy *fragments* (documentation only)

The following are **illustrative JSON fragments** for administrators who already use Chrome’s `ExtensionSettings` style policies. They are **not** complete files, **not** validated against your tenant, and **must** be merged with your real extension ID, update URLs, and organizational policy.

**Do not run random scripts from the internet to apply these.** Use Google’s / Microsoft’s policy deployment tools (ADMX, Intune, registry as documented).

**Placeholder values:**

- `YOUR_EXTENSION_ID_HERE` — replace with the real MV3 extension ID (32-letter a–p string from `chrome://extensions` in developer mode).
- `UPDATE_URL` — Chrome Web Store or self-hosted update URL per [Chrome extension updating](https://developer.chrome.com/docs/extensions/mv3/linux_hosting/).

Example shape (Chrome-style; confirm property names against current policy docs):

```json
{
  "YOUR_EXTENSION_ID_HERE": {
    "installation_mode": "force_installed",
    "update_url": "UPDATE_URL"
  }
}
```

Edge uses analogous **ExtensionSettings** under Edge’s policy namespace. See Microsoft’s **ExtensionSettings** policy page for Edge.

---

## Summary checklist

| Measure | Purpose |
|--------|---------|
| Child = **Standard User** | Blocks most bypass without admin password |
| **Force-install** extension (Chrome/Edge policies) | Extension harder to remove or forget |
| **Incognito / InPrivate** policy + extension rules | Closes “private window without limits” gap |
| **Limit other browsers** | Standard account + Family Safety + optional app control |
| **Family Safety** | Broader device/app and time controls |

---

## Disclaimer

Policies and family controls change with OS and browser updates. This document is **guidance only**, not legal or professional IT advice. You are responsible for compliance with school/work rules and for testing changes before relying on them.

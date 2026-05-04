import type { TodayUsageResponse } from "@/api-types";
import { fetchHealth, getTodayUsage } from "@/api";
import { getServiceBaseUrl, getServiceReachability } from "@/storage";

function formatSeconds(sec: number): string {
  if (sec <= 0) return "0:00";
  const m = Math.floor(sec / 60);
  const s = sec % 60;
  return `${m}:${s.toString().padStart(2, "0")}`;
}

function renderUsage(today: TodayUsageResponse | null): string {
  if (!today || today.groups.length === 0) {
    return `<p class="muted">No managed site usage recorded today (or none configured).</p>`;
  }
  const rows = today.groups
    .map(
      (g) =>
        `<tr><td>${escapeHtml(g.siteGroupName)}</td><td class="num">${formatSeconds(g.remainingSeconds)}</td></tr>`,
    )
    .join("");
  return `
    <p class="muted" style="margin:0 0 6px">Local date: ${escapeHtml(today.localDate)}</p>
    <table class="usage-table">
      <thead><tr><th>Site group</th><th class="num">Left</th></tr></thead>
      <tbody>${rows}</tbody>
    </table>`;
}

function escapeHtml(s: string): string {
  return s
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

async function refresh(): Promise<void> {
  const warnEl = document.getElementById("warn");
  const statusEl = document.getElementById("svc-status");
  const endpointEl = document.getElementById("svc-endpoint");
  const usageEl = document.getElementById("usage-body");

  const base = await getServiceBaseUrl();
  if (endpointEl) {
    endpointEl.textContent = base;
  }

  const [healthOk, today] = await Promise.all([fetchHealth(), getTodayUsage()]);
  // Read after API calls so storage reflects the latest fetchHealth / getTodayUsage updates (avoids stale decision errors).
  const reach = await getServiceReachability();

  const offline = !healthOk || !reach.reachable;
  if (warnEl) {
    if (offline) {
      warnEl.classList.remove("hidden");
      const hint = reach.lastError ? ` (${reach.lastError})` : "";
      warnEl.textContent = `Parent Site Guardian service is unreachable${hint}. Browsing is not blocked; usage may not be recorded.`;
    } else {
      warnEl.classList.add("hidden");
    }
  }

  if (statusEl) {
    if (healthOk) {
      statusEl.textContent = "Connected";
      statusEl.className = "value ok";
    } else {
      statusEl.textContent = "Unavailable";
      statusEl.className = "value bad";
    }
  }

  if (usageEl) {
    usageEl.innerHTML = offline ? `<p class="muted">Usage unavailable while service is offline.</p>` : renderUsage(today);
  }
}

void refresh();

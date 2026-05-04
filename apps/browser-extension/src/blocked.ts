import { getDeviceId, getServiceBaseUrl } from "@/storage";

function formatDuration(totalSeconds: number): string {
  const s = Math.max(0, Math.floor(totalSeconds));
  const h = Math.floor(s / 3600);
  const m = Math.floor((s % 3600) / 60);
  const sec = s % 60;
  if (h > 0) return `${h}h ${m}m`;
  if (m > 0) return `${m}m ${sec}s`;
  return `${sec}s`;
}

function parseIntParam(key: string, fallback = 0): number {
  const raw = new URLSearchParams(location.search).get(key);
  if (raw == null || raw === "") return fallback;
  const n = Number.parseInt(raw, 10);
  return Number.isFinite(n) ? n : fallback;
}

function setText(id: string, text: string): void {
  const el = document.getElementById(id);
  if (el) el.textContent = text;
}

function showError(message: string): void {
  const el = document.getElementById("formError");
  if (!el) return;
  el.textContent = message;
  el.hidden = false;
}

function hideError(): void {
  const el = document.getElementById("formError");
  if (!el) return;
  el.hidden = true;
  el.textContent = "";
}

const params = new URLSearchParams(location.search);
const originalUrl = params.get("u") ?? "";
const siteGroupName = params.get("site") ?? "This site group";
const usedSeconds = parseIntParam("used", 0);
const baseLimitSeconds = parseIntParam("base", 0);
const overrideBonusSeconds = parseIntParam("ov", 0);
const resetIso = params.get("reset") ?? "";
const siteGroupId = params.get("sid") ?? "";

setText("groupName", siteGroupName);
setText("usedDisplay", formatDuration(usedSeconds));
setText(
  "limitDisplay",
  overrideBonusSeconds > 0
    ? `${formatDuration(baseLimitSeconds)} + ${formatDuration(overrideBonusSeconds)} bonus`
    : formatDuration(baseLimitSeconds),
);

if (resetIso) {
  try {
    const d = new Date(resetIso);
    setText("resetDisplay", d.toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" }));
  } catch {
    setText("resetDisplay", resetIso);
  }
} else {
  setText("resetDisplay", "Midnight (local time)");
}

const reason = params.get("reason") ?? "";
const reasonLine = document.getElementById("reasonLine");
if (reasonLine) {
  reasonLine.textContent =
    reason === "daily_limit_exhausted"
      ? "You have used the allowed time for today. A parent can grant more time below."
      : reason
        ? `Reason: ${reason.replaceAll("_", " ")}.`
        : "";
}

if (originalUrl) {
  try {
    const host = new URL(originalUrl).hostname;
    setText("originalHost", `Blocked page: ${host}`);
  } catch {
    setText("originalHost", "");
  }
}

const minuteChoices = document.getElementById("minuteChoices");
const minutes = [5, 10, 15, 30] as const;
if (minuteChoices) {
  for (const m of minutes) {
    const id = `min-${m}`;
    const label = document.createElement("label");
    const input = document.createElement("input");
    input.type = "radio";
    input.name = "extraMinutes";
    input.value = String(m);
    input.id = id;
    if (m === 10) input.checked = true;
    const span = document.createElement("span");
    span.textContent = `${m} min`;
    label.appendChild(input);
    label.appendChild(span);
    minuteChoices.appendChild(label);
  }
}

async function postOverride(pin: string, extraMinutes: number): Promise<{ ok: boolean; status: number; code?: string }> {
  const base = await getServiceBaseUrl();
  const deviceId = await getDeviceId();
  const res = await fetch(`${base}/api/admin/overrides`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      deviceId,
      siteGroupId,
      pin,
      extraMinutes,
    }),
  });
  if (res.ok) return { ok: true, status: res.status };
  let code: string | undefined;
  try {
    const body = (await res.json()) as { error?: string };
    code = body.error;
  } catch {
    /* ignore */
  }
  return { ok: false, status: res.status, code };
}

const form = document.getElementById("overrideForm");
form?.addEventListener("submit", (ev) => {
  ev.preventDefault();
  void (async () => {
    hideError();
    const pin = (document.getElementById("pinInput") as HTMLInputElement | null)?.value ?? "";
    const selected = document.querySelector<HTMLInputElement>('input[name="extraMinutes"]:checked');
    const extraMinutes = selected ? Number.parseInt(selected.value, 10) : 10;
    if (!siteGroupId) {
      showError("Missing site group. Close this tab and try again.");
      return;
    }
    if (!pin) {
      showError("Enter the parent PIN.");
      return;
    }

    const btn = document.getElementById("submitBtn") as HTMLButtonElement | null;
    if (btn) btn.disabled = true;

    const result = await postOverride(pin, extraMinutes);
    if (result.ok) {
      if (originalUrl) {
        location.replace(originalUrl);
      } else {
        setText("groupName", "Time granted");
        showError("");
        hideError();
        setText("reasonLine", "You can close this tab.");
      }
      return;
    }

    if (btn) btn.disabled = false;
    if (result.status === 401) {
      showError("Incorrect PIN. Try again.");
      return;
    }
    if (result.status === 409 && result.code === "pin_not_configured") {
      showError("Parent PIN is not set yet. On the PC, set it once via POST /api/admin/pin/setup or the admin app.");
      return;
    }
    if (result.code === "invalid_extra_minutes") {
      showError("Invalid time choice.");
      return;
    }
    showError("Could not grant time. Check the service is running and try again.");
  })();
});

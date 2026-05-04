import type {
  DecisionResponse,
  ExtensionConfigResponse,
  TodayUsageResponse,
  UsageTickResponse,
} from "@/api-types";
import { STORAGE_TICK_INTERVAL_SECONDS } from "@/config";
import { getDeviceId, getServiceBaseUrl, setServiceReachability } from "@/storage";

function getBrowserLabel(): string {
  const ua = self.navigator?.userAgent ?? "";
  if (/Edg\//.test(ua)) return "edge";
  if (/Chrome\//.test(ua)) return "chrome";
  return "chromium";
}

async function markReachable(): Promise<void> {
  await setServiceReachability({ reachable: true });
}

async function markUnreachable(message: string): Promise<void> {
  await setServiceReachability({ reachable: false, lastError: message });
}

/** Loads extension-facing settings (tick interval). Does not update reachability flags. */
export async function fetchExtensionConfig(): Promise<ExtensionConfigResponse | null> {
  try {
    const base = await getServiceBaseUrl();
    const res = await fetch(`${base}/api/extension-config`, { method: "GET" });
    if (!res.ok) {
      return null;
    }
    const body = (await res.json()) as ExtensionConfigResponse;
    if (
      typeof body.usageTickIntervalSeconds !== "number" ||
      !Number.isInteger(body.usageTickIntervalSeconds) ||
      body.usageTickIntervalSeconds < 1 ||
      body.usageTickIntervalSeconds > 3600
    ) {
      return null;
    }
    await chrome.storage.local.set({ [STORAGE_TICK_INTERVAL_SECONDS]: body.usageTickIntervalSeconds });
    return body;
  } catch {
    return null;
  }
}

export async function fetchHealth(): Promise<boolean> {
  try {
    const base = await getServiceBaseUrl();
    const res = await fetch(`${base}/health`, { method: "GET" });
    if (!res.ok) {
      await markUnreachable(`HTTP ${res.status}`);
      return false;
    }
    await markReachable();
    return true;
  } catch (e) {
    const msg = e instanceof Error ? e.message : "unreachable";
    await markUnreachable(msg);
    return false;
  }
}

export async function postDecision(url: string): Promise<DecisionResponse | null> {
  try {
    const base = await getServiceBaseUrl();
    const deviceId = await getDeviceId();
    const res = await fetch(`${base}/api/decision`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ deviceId, url }),
    });
    if (!res.ok) {
      await markUnreachable(res.status === 404 ? "device not registered (404)" : `decision HTTP ${res.status}`);
      return null;
    }
    const body = (await res.json()) as DecisionResponse;
    await markReachable();
    return body;
  } catch (e) {
    const msg = e instanceof Error ? e.message : "decision failed";
    await markUnreachable(msg);
    return null;
  }
}

export async function postUsageTick(params: {
  url: string;
  startedAt: Date;
  endedAt: Date;
  activeDurationSeconds: number;
}): Promise<UsageTickResponse | null> {
  try {
    const base = await getServiceBaseUrl();
    const deviceId = await getDeviceId();
    const res = await fetch(`${base}/api/usage/tick`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        deviceId,
        browser: getBrowserLabel(),
        url: params.url,
        startedAt: params.startedAt.toISOString(),
        endedAt: params.endedAt.toISOString(),
        activeDurationSeconds: params.activeDurationSeconds,
      }),
    });
    if (!res.ok) {
      await markUnreachable(`tick HTTP ${res.status}`);
      return null;
    }
    const body = (await res.json()) as UsageTickResponse;
    await markReachable();
    return body;
  } catch (e) {
    const msg = e instanceof Error ? e.message : "tick failed";
    await markUnreachable(msg);
    return null;
  }
}

export async function getTodayUsage(): Promise<TodayUsageResponse | null> {
  try {
    const base = await getServiceBaseUrl();
    const deviceId = await getDeviceId();
    const q = new URLSearchParams({ deviceId });
    const res = await fetch(`${base}/api/usage/today?${q.toString()}`, { method: "GET" });
    if (!res.ok) {
      await markUnreachable(`today HTTP ${res.status}`);
      return null;
    }
    const body = (await res.json()) as TodayUsageResponse;
    await markReachable();
    return body;
  } catch (e) {
    const msg = e instanceof Error ? e.message : "today failed";
    await markUnreachable(msg);
    return null;
  }
}

import {
  DEFAULT_DEVICE_ID,
  DEFAULT_SERVICE_BASE_URL,
  DEFAULT_TICK_INTERVAL_SECONDS,
  STORAGE_DEVICE_ID,
  STORAGE_SERVICE_BASE_URL,
  STORAGE_SERVICE_LAST_ERROR,
  STORAGE_SERVICE_REACHABLE,
  STORAGE_TICK_INTERVAL_SECONDS,
} from "@/config";

export type ServiceReachability = {
  reachable: boolean;
  lastError?: string;
};

export async function getDeviceId(): Promise<string> {
  const { [STORAGE_DEVICE_ID]: id } = await chrome.storage.local.get(STORAGE_DEVICE_ID);
  if (typeof id === "string" && id.trim().length > 0) {
    return id.trim();
  }
  await chrome.storage.local.set({ [STORAGE_DEVICE_ID]: DEFAULT_DEVICE_ID });
  return DEFAULT_DEVICE_ID;
}

export async function getServiceBaseUrl(): Promise<string> {
  const { [STORAGE_SERVICE_BASE_URL]: u } = await chrome.storage.local.get(STORAGE_SERVICE_BASE_URL);
  if (typeof u === "string" && u.trim().length > 0) {
    return u.trim().replace(/\/$/, "");
  }
  await chrome.storage.local.set({ [STORAGE_SERVICE_BASE_URL]: DEFAULT_SERVICE_BASE_URL });
  return DEFAULT_SERVICE_BASE_URL;
}

export async function setServiceReachability(state: ServiceReachability): Promise<void> {
  await chrome.storage.local.set({
    [STORAGE_SERVICE_REACHABLE]: state.reachable,
    [STORAGE_SERVICE_LAST_ERROR]: state.lastError ?? "",
  });
}

export async function getServiceReachability(): Promise<ServiceReachability> {
  const v = await chrome.storage.local.get([STORAGE_SERVICE_REACHABLE, STORAGE_SERVICE_LAST_ERROR]);
  const reachable = v[STORAGE_SERVICE_REACHABLE];
  const lastError = v[STORAGE_SERVICE_LAST_ERROR];
  return {
    reachable: reachable !== false,
    lastError: typeof lastError === "string" && lastError.length > 0 ? lastError : undefined,
  };
}

export async function getTickIntervalSeconds(): Promise<number> {
  const { [STORAGE_TICK_INTERVAL_SECONDS]: v } = await chrome.storage.local.get(STORAGE_TICK_INTERVAL_SECONDS);
  if (typeof v === "number" && Number.isInteger(v) && v >= 1 && v <= 3600) {
    return v;
  }
  return DEFAULT_TICK_INTERVAL_SECONDS;
}

export async function getTickIntervalMs(): Promise<number> {
  const s = await getTickIntervalSeconds();
  return s * 1000;
}

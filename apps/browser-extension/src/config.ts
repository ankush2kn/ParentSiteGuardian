/** Default device from first-run seed (see README). */
export const DEFAULT_DEVICE_ID = "20000000-0000-4000-8000-000000000001";

export const DEFAULT_SERVICE_BASE_URL = "http://127.0.0.1:5050";

export const STORAGE_DEVICE_ID = "guardianDeviceId";
export const STORAGE_SERVICE_BASE_URL = "guardianServiceBaseUrl";
export const STORAGE_SERVICE_REACHABLE = "guardianServiceReachable";
export const STORAGE_SERVICE_LAST_ERROR = "guardianServiceLastError";
export const STORAGE_TICK_INTERVAL_SECONDS = "guardianTickIntervalSeconds";

/** Fallback when the service has not been reached yet; overridden by `GET /api/extension-config`. */
export const DEFAULT_TICK_INTERVAL_SECONDS = 10;

export const DECISION_CACHE_MS = 4000;

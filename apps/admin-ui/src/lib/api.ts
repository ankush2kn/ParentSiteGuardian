import { apiUrl } from '@/lib/apiBase'
import type {
  AdminBootstrapResponse,
  AdminSiteGroupsResponse,
  DailyLimitRow,
  EffectiveConfigResponse,
  TodayUsageResponse,
  UsageHistoryResponse,
} from '@/lib/types'

async function parseJson<T>(res: Response): Promise<T> {
  const text = await res.text()
  if (!text) return undefined as T
  return JSON.parse(text) as T
}

export class ApiError extends Error {
  readonly status: number
  readonly body: unknown

  constructor(message: string, status: number, body: unknown) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.body = body
  }
}

export async function fetchBootstrap(): Promise<AdminBootstrapResponse> {
  const res = await fetch(apiUrl('/api/admin/bootstrap'))
  if (!res.ok) throw new ApiError('Failed to load devices', res.status, await res.text())
  return parseJson<AdminBootstrapResponse>(res)
}

export async function fetchEffectiveConfig(): Promise<EffectiveConfigResponse> {
  const res = await fetch(apiUrl('/api/config/effective'))
  if (!res.ok) throw new ApiError('Failed to load effective config', res.status, await res.text())
  return parseJson<EffectiveConfigResponse>(res)
}

export async function fetchTodayUsage(deviceId: string): Promise<TodayUsageResponse> {
  const q = new URLSearchParams({ deviceId })
  const res = await fetch(apiUrl(`/api/usage/today?${q}`))
  if (!res.ok) throw new ApiError('Failed to load today usage', res.status, await res.text())
  return parseJson<TodayUsageResponse>(res)
}

export async function fetchSiteGroups(deviceId: string): Promise<AdminSiteGroupsResponse> {
  const q = new URLSearchParams({ deviceId })
  const res = await fetch(apiUrl(`/api/admin/site-groups?${q}`))
  if (!res.ok) throw new ApiError('Failed to load site groups', res.status, await res.text())
  return parseJson<AdminSiteGroupsResponse>(res)
}

export async function fetchUsageHistory(deviceId: string, days = 30): Promise<UsageHistoryResponse> {
  const q = new URLSearchParams({ deviceId, days: String(days) })
  const res = await fetch(apiUrl(`/api/admin/usage/history?${q}`))
  if (!res.ok) throw new ApiError('Failed to load history', res.status, await res.text())
  return parseJson<UsageHistoryResponse>(res)
}

export async function fetchDailyLimits(deviceId: string): Promise<DailyLimitRow[]> {
  const q = new URLSearchParams({ deviceId })
  const res = await fetch(apiUrl(`/api/admin/daily-limits?${q}`))
  if (!res.ok) throw new ApiError('Failed to load limits', res.status, await res.text())
  return parseJson<DailyLimitRow[]>(res)
}

/**
 * `retryAfterSeconds` > 0 means the next PIN attempt is refused until that many seconds pass
 * (the service escalates the wait after repeated wrong PINs).
 */
export type PinVerifyResult =
  | { ok: true }
  | { ok: false; lockedOut: boolean; retryAfterSeconds: number }

export async function verifyPin(pin: string): Promise<PinVerifyResult> {
  const res = await fetch(apiUrl('/api/admin/pin/verify'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ pin }),
  })
  if (res.status === 429) {
    const data = await parseJson<{ retryAfterSeconds?: number }>(res).catch(() => undefined)
    return { ok: false, lockedOut: true, retryAfterSeconds: data?.retryAfterSeconds ?? 60 }
  }
  if (!res.ok) return { ok: false, lockedOut: false, retryAfterSeconds: 0 }
  const data = await parseJson<{ valid: boolean; retryAfterSeconds?: number }>(res)
  if (data.valid === true) return { ok: true }
  return { ok: false, lockedOut: false, retryAfterSeconds: data.retryAfterSeconds ?? 0 }
}

/** "45 seconds", "3 minutes", "2 hours 15 minutes". */
export function formatPinWait(seconds: number): string {
  const s = Math.max(1, Math.ceil(seconds))
  if (s < 60) return `${s} second${s === 1 ? '' : 's'}`
  const totalMinutes = Math.ceil(s / 60)
  const h = Math.floor(totalMinutes / 60)
  const m = totalMinutes % 60
  const hPart = h > 0 ? `${h} hour${h === 1 ? '' : 's'}` : ''
  const mPart = m > 0 ? `${m} minute${m === 1 ? '' : 's'}` : ''
  return [hPart, mPart].filter(Boolean).join(' ')
}

/**
 * User-facing message for a PIN rejection from any PIN-protected call (401 wrong PIN, 429 locked out),
 * or null when the error is not PIN related.
 */
export function pinErrorMessage(e: unknown): string | null {
  if (!(e instanceof ApiError) || (e.status !== 401 && e.status !== 429)) return null
  let wait = 0
  try {
    const body = typeof e.body === 'string' ? (JSON.parse(e.body) as { retryAfterSeconds?: number }) : null
    wait = body?.retryAfterSeconds ?? 0
  } catch {
    // Non-JSON body: no wait information.
  }
  if (e.status === 429) {
    return `Too many wrong PINs. Try again in ${formatPinWait(wait || 60)}.`
  }
  return wait > 0
    ? `That PIN is not correct. Too many wrong tries — wait ${formatPinWait(wait)} before trying again.`
    : 'That PIN is not correct.'
}

export async function setupPin(pin: string): Promise<void> {
  const res = await fetch(apiUrl('/api/admin/pin/setup'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ pin }),
  })
  if (!res.ok) {
    const body = await res.text()
    throw new ApiError('PIN setup failed', res.status, body)
  }
}

export async function changePin(currentPin: string, newPin: string): Promise<void> {
  const res = await fetch(apiUrl('/api/admin/pin/change'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ currentPin, newPin }),
  })
  if (!res.ok) {
    const body = await res.text()
    throw new ApiError('PIN change failed', res.status, body)
  }
}

export async function createSiteGroup(deviceId: string, pin: string, name: string): Promise<string> {
  const res = await fetch(apiUrl('/api/admin/site-groups'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ deviceId, pin, name }),
  })
  if (!res.ok) throw new ApiError('Create group failed', res.status, await res.text())
  const data = await parseJson<{ id: string }>(res)
  return data.id
}

export async function updateSiteGroup(
  groupId: string,
  deviceId: string,
  pin: string,
  name: string,
): Promise<void> {
  const res = await fetch(apiUrl(`/api/admin/site-groups/${encodeURIComponent(groupId)}`), {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ deviceId, pin, name }),
  })
  if (!res.ok) throw new ApiError('Update group failed', res.status, await res.text())
}

export async function deleteSiteGroup(groupId: string, deviceId: string, pin: string): Promise<void> {
  const res = await fetch(apiUrl(`/api/admin/site-groups/${encodeURIComponent(groupId)}`), {
    method: 'DELETE',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ deviceId, pin }),
  })
  if (!res.ok) throw new ApiError('Delete group failed', res.status, await res.text())
}

export async function addSiteRule(
  groupId: string,
  deviceId: string,
  pin: string,
  pattern: string,
  patternType: string,
): Promise<void> {
  const res = await fetch(apiUrl(`/api/admin/site-groups/${encodeURIComponent(groupId)}/rules`), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ deviceId, pin, pattern, patternType }),
  })
  if (!res.ok) throw new ApiError('Add rule failed', res.status, await res.text())
}

export async function deleteSiteRule(ruleId: string, deviceId: string, pin: string): Promise<void> {
  const res = await fetch(apiUrl('/api/admin/site-rules/delete'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ deviceId, pin, ruleId }),
  })
  if (!res.ok) throw new ApiError('Delete rule failed', res.status, await res.text())
}

export async function upsertDailyLimit(
  deviceId: string,
  siteGroupId: string,
  pin: string,
  limitSeconds: number,
): Promise<void> {
  const res = await fetch(apiUrl('/api/admin/daily-limits'), {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ deviceId, siteGroupId, pin, limitSeconds }),
  })
  if (!res.ok) throw new ApiError('Save limit failed', res.status, await res.text())
}

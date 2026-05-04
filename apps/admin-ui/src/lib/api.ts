import { apiUrl } from '@/lib/apiBase'
import type {
  AdminBootstrapResponse,
  AdminSiteGroupsResponse,
  DailyLimitRow,
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

export async function verifyPin(pin: string): Promise<boolean> {
  const res = await fetch(apiUrl('/api/admin/pin/verify'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ pin }),
  })
  if (!res.ok) return false
  const data = await parseJson<{ valid: boolean }>(res)
  return data.valid === true
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

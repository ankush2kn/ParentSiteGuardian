export type AdminBootstrapDevice = {
  id: string
  name: string
  childId: string
  createdAt: string
  machineFingerprint: string | null
  windowsUserSid: string | null
}

export type AdminBootstrapResponse = {
  pinConfigured: boolean
  devices: AdminBootstrapDevice[]
}

export type AdminSiteRule = {
  id: string
  pattern: string
  patternType: string
  createdAt: string
}

export type AdminSiteGroup = {
  id: string
  name: string
  color: string | null
  createdAt: string
  rules: AdminSiteRule[]
}

export type AdminSiteGroupsResponse = {
  groups: AdminSiteGroup[]
}

export type TodaySiteGroupUsage = {
  siteGroupId: string
  siteGroupName: string
  usedSeconds: number
  baseLimitSeconds: number
  overrideBonusSeconds: number
  effectiveLimitSeconds: number
  remainingSeconds: number
}

export type TodayUsageResponse = {
  localDate: string
  deviceId: string
  groups: TodaySiteGroupUsage[]
}

export type UsageHistoryGroup = {
  siteGroupId: string
  siteGroupName: string
  usedSeconds: number
}

export type UsageHistoryDay = {
  localDate: string
  groups: UsageHistoryGroup[]
}

export type UsageHistoryResponse = {
  deviceId: string
  days: UsageHistoryDay[]
}

export type DailyLimitRow = {
  id: string
  deviceId: string
  siteGroupId: string
  siteGroupName: string
  limitSeconds: number
  createdAt: string
}

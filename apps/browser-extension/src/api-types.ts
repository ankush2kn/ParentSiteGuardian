export type DecisionResponse = {
  matched: boolean;
  siteGroupId?: string;
  siteGroupName?: string;
  allowed: boolean;
  usedSeconds: number;
  limitSeconds: number;
  remainingSeconds: number;
  reason: string;
  baseLimitSeconds?: number;
  overrideBonusSeconds?: number;
  nextResetAt?: string;
};

export type UsageTickResponse = {
  recorded: boolean;
  siteGroupId?: string;
  reason: string;
};

export type TodaySiteGroupUsage = {
  siteGroupId: string;
  siteGroupName: string;
  usedSeconds: number;
  baseLimitSeconds: number;
  overrideBonusSeconds: number;
  effectiveLimitSeconds: number;
  remainingSeconds: number;
};

export type TodayUsageResponse = {
  localDate: string;
  deviceId: string;
  groups: TodaySiteGroupUsage[];
};

export type ExtensionConfigResponse = {
  usageTickIntervalSeconds: number;
};

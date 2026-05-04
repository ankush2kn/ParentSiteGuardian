import type { DecisionResponse } from "@/api-types";
import { DECISION_CACHE_MS } from "@/config";
import { fetchExtensionConfig, fetchHealth, postDecision, postUsageTick } from "@/api";
import { getTickIntervalSeconds } from "@/storage";
import { MSG_DECISION, MSG_TICK } from "@/messages";

type DecisionMessage = { type: typeof MSG_DECISION; url: string };
type TickMessage = { type: typeof MSG_TICK; url: string };
type DecisionReply = { ok: true } | { redirect: string };

const decisionCache = new Map<string, { at: number; decision: DecisionResponse }>();

function isTrackableUrl(url: string): boolean {
  return url.startsWith("http://") || url.startsWith("https://");
}

function blockPageUrl(originalUrl: string, decision: DecisionResponse): string {
  const u = new URL(chrome.runtime.getURL("blocked.html"));
  u.searchParams.set("u", originalUrl);
  if (decision.siteGroupName) u.searchParams.set("site", decision.siteGroupName);
  u.searchParams.set("reason", decision.reason);
  u.searchParams.set("used", String(decision.usedSeconds));
  u.searchParams.set("base", String(decision.baseLimitSeconds ?? 0));
  u.searchParams.set("ov", String(decision.overrideBonusSeconds ?? 0));
  if (decision.nextResetAt) u.searchParams.set("reset", decision.nextResetAt);
  if (decision.siteGroupId) u.searchParams.set("sid", decision.siteGroupId);
  return u.href;
}

async function refreshDecision(url: string): Promise<DecisionResponse | null> {
  const d = await postDecision(url);
  const now = Date.now();
  if (d) {
    decisionCache.set(url, { at: now, decision: d });
  } else {
    decisionCache.delete(url);
  }
  return d;
}

async function getDecisionCached(url: string): Promise<DecisionResponse | null> {
  const now = Date.now();
  const hit = decisionCache.get(url);
  if (hit && now - hit.at < DECISION_CACHE_MS) {
    return hit.decision;
  }
  return refreshDecision(url);
}

async function isWindowFocused(windowId: number): Promise<boolean> {
  try {
    const w = await chrome.windows.get(windowId);
    return w.focused === true;
  } catch {
    return false;
  }
}

async function enforceBlockedIfNeeded(tabId: number, url: string): Promise<void> {
  if (!isTrackableUrl(url)) return;
  const decision = await refreshDecision(url);
  if (!decision) {
    return;
  }
  if (decision.matched && !decision.allowed) {
    await chrome.tabs.update(tabId, {
      url: blockPageUrl(url, decision),
    });
  }
}

async function enforceIfActiveFocusedTab(tabId: number, url: string): Promise<void> {
  if (!isTrackableUrl(url)) return;
  const win = await chrome.windows.getLastFocused({ populate: false });
  if (!win.focused || win.id === undefined) return;
  const active = await chrome.tabs.query({ active: true, windowId: win.id });
  if (active[0]?.id !== tabId) return;
  decisionCache.delete(url);
  await enforceBlockedIfNeeded(tabId, url);
}

async function handleVisibilityDecision(url: string): Promise<DecisionReply> {
  if (!isTrackableUrl(url)) {
    return { ok: true };
  }
  const decision = await refreshDecision(url);
  if (!decision) {
    return { ok: true };
  }
  if (decision.matched && !decision.allowed) {
    return {
      redirect: blockPageUrl(url, decision),
    };
  }
  return { ok: true };
}

/**
 * Usage ticks run here (not on a service-worker timer). MV3 workers suspend after ~30s idle,
 * which stops setTimeout; the content script keeps an interval while the page is visible.
 */
async function handleContentTick(sender: chrome.runtime.MessageSender, urlFromContent: string): Promise<void> {
  const tabId = sender.tab?.id;
  const windowId = sender.tab?.windowId;
  if (tabId === undefined || windowId === undefined) return;

  if (!(await isWindowFocused(windowId))) return;

  const active = await chrome.tabs.query({ active: true, windowId });
  if (active[0]?.id !== tabId) return;

  let url = sender.tab?.url ?? urlFromContent;
  try {
    const t = await chrome.tabs.get(tabId);
    if (t.url && isTrackableUrl(t.url)) {
      url = t.url;
    }
  } catch {
    return;
  }

  if (!isTrackableUrl(url)) return;

  const decision = await getDecisionCached(url);
  if (!decision?.matched || !decision.allowed) return;

  const tickSeconds = await getTickIntervalSeconds();
  const endedAt = Date.now();
  const startedAt = endedAt - tickSeconds * 1000;
  const tick = await postUsageTick({
    url,
    startedAt: new Date(startedAt),
    endedAt: new Date(endedAt),
    activeDurationSeconds: tickSeconds,
  });
  if (tick != null) {
    decisionCache.delete(url);
  }
}

chrome.runtime.onInstalled.addListener(() => {
  void fetchHealth();
  void fetchExtensionConfig();
});

chrome.runtime.onStartup.addListener(() => {
  void fetchHealth();
  void fetchExtensionConfig();
});

void fetchHealth();
void fetchExtensionConfig();

setInterval(() => {
  void fetchHealth();
  void fetchExtensionConfig();
}, 60_000);

chrome.runtime.onMessage.addListener((message: unknown, sender, sendResponse): boolean => {
  if (message && typeof message === "object" && (message as DecisionMessage).type === MSG_DECISION) {
    const url = (message as DecisionMessage).url;
    void handleVisibilityDecision(url)
      .then(sendResponse)
      .catch(() => {
        sendResponse({ ok: true } satisfies DecisionReply);
      });
    return true;
  }
  if (message && typeof message === "object" && (message as TickMessage).type === MSG_TICK) {
    const url = (message as TickMessage).url;
    void handleContentTick(sender, url);
    return false;
  }
  return false;
});

chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
  const url =
    changeInfo.url ?? (changeInfo.status === "complete" && tab.url ? tab.url : undefined);
  if (!url) return;
  void enforceIfActiveFocusedTab(tabId, url);
});

chrome.webNavigation.onHistoryStateUpdated.addListener((details) => {
  if (details.frameId !== 0) return;
  if (!isTrackableUrl(details.url)) return;
  void enforceIfActiveFocusedTab(details.tabId, details.url);
});

chrome.tabs.onActivated.addListener((activeInfo) => {
  void (async () => {
    try {
      const tab = await chrome.tabs.get(activeInfo.tabId);
      if (!tab.url || !isTrackableUrl(tab.url)) return;
      const focused = await isWindowFocused(activeInfo.windowId);
      if (!focused) return;
      decisionCache.delete(tab.url);
      await enforceBlockedIfNeeded(activeInfo.tabId, tab.url);
    } catch {
      /* tab may be closing */
    }
  })();
});

chrome.windows.onFocusChanged.addListener((windowId) => {
  if (windowId === chrome.windows.WINDOW_ID_NONE) {
    return;
  }
  void (async () => {
    try {
      const tabs = await chrome.tabs.query({ active: true, windowId });
      const tab = tabs[0];
      if (!tab?.id || !tab.url || !isTrackableUrl(tab.url)) return;
      decisionCache.delete(tab.url);
      await enforceBlockedIfNeeded(tab.id, tab.url);
    } catch {
      /* ignore */
    }
  })();
});

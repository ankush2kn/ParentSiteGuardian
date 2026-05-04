import { MSG_DECISION, MSG_TICK } from "@/messages";
import { getTickIntervalMs } from "@/storage";

type DecisionReply = { ok: true } | { redirect: string };

function isRedirectReply(value: unknown): value is DecisionReply & { redirect: string } {
  return (
    value !== null &&
    typeof value === "object" &&
    "redirect" in value &&
    typeof (value as { redirect?: unknown }).redirect === "string"
  );
}

function sendDecisionMessage(url: string): Promise<DecisionReply> {
  return new Promise((resolve) => {
    try {
      chrome.runtime.sendMessage({ type: MSG_DECISION, url }, (reply: unknown) => {
        if (chrome.runtime.lastError) {
          resolve({ ok: true });
          return;
        }
        if (isRedirectReply(reply)) {
          resolve(reply);
          return;
        }
        resolve({ ok: true });
      });
    } catch {
      resolve({ ok: true });
    }
  });
}

let tickInterval: ReturnType<typeof setInterval> | undefined;

function stopUsageTicker(): void {
  if (tickInterval !== undefined) {
    clearInterval(tickInterval);
    tickInterval = undefined;
  }
}

async function startUsageTicker(): Promise<void> {
  stopUsageTicker();
  const ms = await getTickIntervalMs();
  tickInterval = setInterval(() => {
    if (document.visibilityState !== "visible") return;
    try {
      chrome.runtime.sendMessage({ type: MSG_TICK, url: location.href }, () => {
        void chrome.runtime.lastError;
      });
    } catch {
      /* extension reload / invalidated context */
    }
  }, ms);
}

async function syncTickerToVisibility(): Promise<void> {
  if (document.visibilityState === "visible") {
    await startUsageTicker();
  } else {
    stopUsageTicker();
  }
}

document.addEventListener("visibilitychange", () => {
  void syncTickerToVisibility();
});

const html = document.documentElement;
const previousVisibility = html.style.visibility;
html.style.visibility = "hidden";

void (async () => {
  try {
    const reply = await sendDecisionMessage(location.href);
    if (isRedirectReply(reply)) {
      location.replace(reply.redirect);
      return;
    }
  } catch {
    /* fail open */
  }
  html.style.visibility = previousVisibility || "";
  void syncTickerToVisibility();
})();

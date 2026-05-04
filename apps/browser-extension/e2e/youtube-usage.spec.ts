import { test, expect } from "./fixtures";

const DEVICE_ID = "20000000-0000-4000-8000-000000000001";
const BASE = "http://127.0.0.1:5050";

async function youtubeUsedSeconds(): Promise<number> {
  const r = await fetch(`${BASE}/api/usage/today?deviceId=${encodeURIComponent(DEVICE_ID)}`);
  if (!r.ok) {
    throw new Error(`today HTTP ${r.status}`);
  }
  const j: { groups: { siteGroupName: string; usedSeconds: number }[] } = await r.json();
  return j.groups.find((g) => g.siteGroupName === "YouTube")?.usedSeconds ?? 0;
}

test.describe("YouTube usage ticks", () => {
  test.beforeAll(async ({}, testInfo) => {
    const healthOk = await fetch(`${BASE}/health`)
      .then((r) => r.ok)
      .catch(() => false);
    if (!healthOk) {
      testInfo.skip(true, `Start the guardian API first (${BASE}) so usage can be recorded.`);
      return;
    }
    const todayProbe = await fetch(
      `${BASE}/api/usage/today?deviceId=${encodeURIComponent(DEVICE_ID)}`,
    );
    if (!todayProbe.ok) {
      testInfo.skip(
        true,
        `GET /api/usage/today must return 200 (got ${todayProbe.status}). Check the service database / seed.`,
      );
    }
  });

  test("increases YouTube used seconds while the tab stays visible", async ({ persistentContext }) => {
    const before = await youtubeUsedSeconds();

    const page = await persistentContext.newPage();
    await page.goto("https://www.youtube.com/", {
      waitUntil: "domcontentloaded",
      timeout: 90_000,
    });

    await expect
      .poll(() => youtubeUsedSeconds(), {
        timeout: 90_000,
        intervals: [1_000, 2_000, 3_000],
      })
      .toBeGreaterThanOrEqual(before + 5);
  });
});

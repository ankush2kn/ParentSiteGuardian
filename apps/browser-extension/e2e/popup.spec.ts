import { test, expect } from "./fixtures";

test("popup page renders status", async ({ persistentContext }) => {
  let worker = persistentContext.serviceWorkers()[0];
  if (!worker) {
    worker = await persistentContext.waitForEvent("serviceworker");
  }

  const extId = new URL(worker.url()).hostname;
  const page = await persistentContext.newPage();
  await page.goto(`chrome-extension://${extId}/popup.html`);

  await expect(page.getByRole("heading", { name: "Parent Site Guardian" })).toBeVisible();
  await expect(page.getByText("Service", { exact: true })).toBeVisible();
});

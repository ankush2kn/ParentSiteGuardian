import { test as base, chromium, type BrowserContext } from "@playwright/test";
import * as fs from "node:fs/promises";
import * as os from "node:os";
import * as path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const pathToExtension = path.join(__dirname, "..", "dist");

export const test = base.extend<{ persistentContext: BrowserContext }>({
  persistentContext: async ({}, use) => {
    const userDataDir = await fs.mkdtemp(path.join(os.tmpdir(), "psg-ext-"));
    const context = await chromium.launchPersistentContext(userDataDir, {
      headless: false,
      args: [`--disable-extensions-except=${pathToExtension}`, `--load-extension=${pathToExtension}`],
    });
    await use(context);
    await context.close();
    await fs.rm(userDataDir, { recursive: true, force: true });
  },
});

export const expect = test.expect;

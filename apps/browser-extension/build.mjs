import * as esbuild from "esbuild";
import { copyFileSync, mkdirSync, existsSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const dist = join(__dirname, "dist");
const watch = process.argv.includes("--watch");

if (!existsSync(dist)) {
  mkdirSync(dist, { recursive: true });
}

function copyStatic() {
  copyFileSync(join(__dirname, "public", "manifest.json"), join(dist, "manifest.json"));
  copyFileSync(join(__dirname, "popup.html"), join(dist, "popup.html"));
  copyFileSync(join(__dirname, "popup.css"), join(dist, "popup.css"));
  copyFileSync(join(__dirname, "public", "blocked.html"), join(dist, "blocked.html"));
  copyFileSync(join(__dirname, "public", "blocked.css"), join(dist, "blocked.css"));
}

copyStatic();

const buildOptions = {
  entryPoints: {
    background: join(__dirname, "src", "background.ts"),
    popup: join(__dirname, "src", "popup.ts"),
    content: join(__dirname, "src", "content.ts"),
    blocked: join(__dirname, "src", "blocked.ts"),
  },
  bundle: true,
  outdir: dist,
  entryNames: "[name]",
  format: "iife",
  platform: "browser",
  target: "chrome109",
  alias: {
    "@": join(__dirname, "src"),
  },
};

if (watch) {
  const ctx = await esbuild.context(buildOptions);
  await ctx.watch();
  console.info("[browser-extension] Watching for changes…");
} else {
  await esbuild.build(buildOptions);
  console.info("[browser-extension] Build finished → dist/");
}

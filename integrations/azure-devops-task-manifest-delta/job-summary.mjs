#!/usr/bin/env node
/**
 * Runs the shared `fetch-manifest-delta.mjs` and publishes the Markdown to the
 * Azure Pipelines run summary via `##vso[task.uploadsummary]`.
 */
import { mkdtempSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const fetchScript = process.env.ARCHLUCID_JOB_SUMMARY_FETCH_SCRIPT_OVERRIDE?.trim()
  ? path.resolve(process.env.ARCHLUCID_JOB_SUMMARY_FETCH_SCRIPT_OVERRIDE.trim())
  : path.resolve(__dirname, "../github-action-manifest-delta/fetch-manifest-delta.mjs");

export function buildUploadSummaryVsoLine(absoluteMarkdownPath) {
  return `##vso[task.uploadsummary]${absoluteMarkdownPath}`;
}

const isCli = (() => {
  if (!process.argv[1]) return false;

  try {
    return path.resolve(fileURLToPath(import.meta.url)) === path.resolve(process.argv[1]);
  }
  catch {
    return false;
  }
})();

if (isCli) {
  const r = spawnSync(process.execPath, [fetchScript], {
    env: process.env,
    encoding: "utf8",
    maxBuffer: 10 * 1024 * 1024,
  });

  if (r.error) {
    console.error(r.error);
    process.exit(1);
  }

  if (r.status !== 0) {
    process.stderr.write(r.stderr ?? "");
    process.exit(r.status ?? 1);
  }

  const dir = mkdtempSync(path.join(tmpdir(), "archlucid-job-summary-"));
  const summaryPath = path.join(dir, "archlucid-manifest-delta.md");

  writeFileSync(summaryPath, r.stdout, "utf8");

  console.log(buildUploadSummaryVsoLine(summaryPath));
}

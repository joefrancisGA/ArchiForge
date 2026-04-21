/**
 * Run: node --test integrations/azure-devops-task-manifest-delta/job-summary.test.mjs
 */
import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { test } from "node:test";

import { buildUploadSummaryVsoLine } from "./job-summary.mjs";

const __dirname = dirname(fileURLToPath(import.meta.url));
const jobSummary = join(__dirname, "job-summary.mjs");
const stubFetch = join(__dirname, "test-stub-fetch.mjs");

test("buildUploadSummaryVsoLine matches Azure Pipelines command shape", () => {
  const line = buildUploadSummaryVsoLine("C:\\temp\\a.md");

  assert.equal(line, "##vso[task.uploadsummary]C:\\temp\\a.md");
});

test("job-summary writes stub fetch output and prints vso upload line", () => {
  const r = spawnSync(process.execPath, [jobSummary], {
    env: {
      ...process.env,
      ARCHLUCID_JOB_SUMMARY_FETCH_SCRIPT_OVERRIDE: stubFetch,
    },
    encoding: "utf8",
    maxBuffer: 10 * 1024 * 1024,
  });

  assert.equal(r.status, 0, r.stderr);

  const line = r.stdout.trim();
  const prefix = "##vso[task.uploadsummary]";

  assert.ok(line.startsWith(prefix));

  const outPath = line.slice(prefix.length);
  const body = readFileSync(outPath, "utf8");

  assert.ok(body.includes("## ArchLucid manifest delta"));
  assert.ok(body.includes("stub line"));
});

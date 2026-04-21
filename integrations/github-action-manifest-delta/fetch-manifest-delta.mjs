#!/usr/bin/env node
/**
 * Calls GET /v1/compare?baseRunId=&targetRunId= and prints Markdown for GITHUB_STEP_SUMMARY.
 * Requires: ARCHLUCID_API_BASE, ARCHLUCID_API_TOKEN, ARCHLUCID_BASE_RUN_ID, ARCHLUCID_TARGET_RUN_ID
 */
const base = (process.env.ARCHLUCID_API_BASE || "").replace(/\/$/, "");
const token = process.env.ARCHLUCID_API_TOKEN || "";
const baseRunId = process.env.ARCHLUCID_BASE_RUN_ID || "";
const targetRunId = process.env.ARCHLUCID_TARGET_RUN_ID || "";
const compareTemplate = process.env.ARCHLUCID_COMPARE_URL_TEMPLATE || "";

if (!base || !token || !baseRunId || !targetRunId) {
  console.error("Missing required env: ARCHLUCID_API_BASE, ARCHLUCID_API_TOKEN, ARCHLUCID_BASE_RUN_ID, ARCHLUCID_TARGET_RUN_ID");
  process.exit(1);
}

const url = new URL(`${base}/v1/compare`);
url.searchParams.set("baseRunId", baseRunId);
url.searchParams.set("targetRunId", targetRunId);

const res = await fetch(url, {
  headers: {
    Accept: "application/json",
    "X-Api-Key": token,
  },
});

if (!res.ok) {
  const text = await res.text();
  const warnOnly = process.env.ARCHLUCID_COMPARE_WARN_ONLY === "1";
  if (warnOnly && res.status === 404) {
    console.warn(
      `WARNING: target run not yet committed (or scope mismatch): HTTP ${res.status}. ${text.slice(0, 500)}`,
    );
    process.stdout.write(
      [
        "## ArchLucid manifest delta",
        "",
        "> **WARNING:** `GET /v1/compare` returned 404 — the baseline/target runs may not be committed yet, or the API key cannot see them. Re-run after both runs exist.",
        "",
      ].join("\n"),
    );
    process.exit(0);
  }

  console.error(`ArchLucid compare failed: HTTP ${res.status} ${text}`);
  process.exit(1);
}

/** @type {{ totalDeltaCount?: number, summaryHighlights?: string[], decisionChanges?: unknown[], requirementChanges?: unknown[], securityChanges?: unknown[], topologyChanges?: unknown[], costChanges?: unknown[] }} */
const body = await res.json();

const lines = [];
lines.push("## ArchLucid manifest delta");
lines.push("");
lines.push(`- **Base run:** \`${baseRunId}\``);
lines.push(`- **Target run:** \`${targetRunId}\``);
lines.push(`- **Total delta rows:** ${typeof body.totalDeltaCount === "number" ? body.totalDeltaCount : "?"}`);
lines.push("");

if (Array.isArray(body.summaryHighlights) && body.summaryHighlights.length) {
  lines.push("### Highlights");
  lines.push("");
  for (const h of body.summaryHighlights.slice(0, 20)) {
    lines.push(`- ${h}`);
  }
  lines.push("");
}

const counts = [
  ["Decision changes", body.decisionChanges],
  ["Requirement changes", body.requirementChanges],
  ["Security changes", body.securityChanges],
  ["Topology changes", body.topologyChanges],
  ["Cost changes", body.costChanges],
];

lines.push("### Delta buckets (counts)");
lines.push("");
lines.push("| Bucket | Count |");
lines.push("| --- | ---: |");
for (const [label, arr] of counts) {
  const n = Array.isArray(arr) ? arr.length : 0;
  lines.push(`| ${label} | ${n} |`);
}
lines.push("");

if (compareTemplate.includes("{baseRunId}") && compareTemplate.includes("{targetRunId}")) {
  const href = compareTemplate.replaceAll("{baseRunId}", baseRunId).replaceAll("{targetRunId}", targetRunId);
  lines.push(`[Open operator compare](${href})`);
  lines.push("");
}

process.stdout.write(lines.join("\n"));

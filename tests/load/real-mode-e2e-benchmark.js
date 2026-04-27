/**
 * Real-mode end-to-end benchmark — measures wall-clock time for a full
 * authority run (create → execute → poll until done → commit → retrieve manifest)
 * against a live API with real Azure OpenAI agent execution (not simulator).
 *
 * This produces the defensible "time-to-value" number for marketing and SLA
 * purposes. Results are written to handleSummary JSON.
 *
 * Prerequisites:
 *   - API running with AgentExecution:Mode=AzureOpenAI (real mode)
 *   - Valid ARCHLUCID_API_KEY or DevelopmentBypass
 *   - Azure OpenAI endpoint configured and reachable
 *
 * Usage:
 *   ARCHLUCID_BASE_URL=https://staging.archlucid.net k6 run tests/load/real-mode-e2e-benchmark.js
 *
 * Environment variables:
 *   ARCHLUCID_BASE_URL       API base URL (default: http://127.0.0.1:5001)
 *   ARCHLUCID_API_KEY        API key for authentication (optional if DevelopmentBypass)
 *   K6_POLL_INTERVAL_MS      Polling interval in ms (default: 2000)
 *   K6_POLL_TIMEOUT_MS       Max wait before declaring timeout (default: 300000 = 5 min)
 *   K6_ITERATIONS            Number of benchmark iterations (default: 3)
 *   K6_SUMMARY_PATH          Output path for handleSummary JSON
 */
import http from "k6/http";
import { check, sleep } from "k6";
import { Trend, Counter } from "k6/metrics";

const BASE = __ENV.ARCHLUCID_BASE_URL || __ENV.BASE_URL || "http://127.0.0.1:5001";
const POLL_INTERVAL_MS = Number(__ENV.K6_POLL_INTERVAL_MS || 2000);
const POLL_TIMEOUT_MS = Number(__ENV.K6_POLL_TIMEOUT_MS || 300000);
const ITERATIONS = Number(__ENV.K6_ITERATIONS || 3);

const e2eWallClock = new Trend("e2e_wall_clock_ms", true);
const createDuration = new Trend("step_create_ms", true);
const executeDuration = new Trend("step_execute_ms", true);
const pollWait = new Trend("step_poll_wait_ms", true);
const commitDuration = new Trend("step_commit_ms", true);
const manifestDuration = new Trend("step_manifest_retrieve_ms", true);
const successCount = new Counter("e2e_success_count");
const failCount = new Counter("e2e_fail_count");

export const options = {
  scenarios: {
    benchmark: {
      executor: "per-vu-iterations",
      vus: 1,
      iterations: ITERATIONS,
      maxDuration: `${POLL_TIMEOUT_MS * ITERATIONS + 60000}ms`,
    },
  },
  thresholds: {
    e2e_wall_clock_ms: ["p(50)<120000", "p(95)<180000"],
    e2e_fail_count: ["count<1"],
  },
};

function headers() {
  const h = {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-Correlation-ID": `k6-realmode-${__VU}-${__ITER}-${Date.now()}`,
  };

  const key = __ENV.ARCHLUCID_API_KEY;

  if (key) {
    h["X-Api-Key"] = key;
  }

  return h;
}

export default function realModeBenchmark() {
  const e2eStart = Date.now();

  const requestBody = JSON.stringify({
    requestId: `k6-realmode-${__VU}-${__ITER}-${Date.now()}`,
    description: "k6 real-mode benchmark — single architecture request for time-to-value measurement",
    systemName: "K6RealModeBenchmark",
    environment: "prod",
    cloudProvider: 1,
    constraints: [],
    requiredCapabilities: ["SQL", "Messaging", "Caching"],
    assumptions: ["Single region", "< 1000 concurrent users"],
    priorManifestVersion: null,
  });

  const createStart = Date.now();
  const createRes = http.post(`${BASE}/v1/architecture/request`, requestBody, {
    headers: headers(),
    tags: { endpoint: "realmode_create" },
  });
  createDuration.add(Date.now() - createStart);

  const createOk = check(createRes, {
    "create run 2xx": (r) => r.status >= 200 && r.status < 300,
  });

  if (!createOk) {
    failCount.add(1);
    return;
  }

  let runId = null;

  try {
    const j = JSON.parse(createRes.body);
    runId = j.run?.runId || j.Run?.RunId;
  } catch {
    failCount.add(1);
    return;
  }

  if (!runId) {
    failCount.add(1);
    return;
  }

  const executeStart = Date.now();
  const executeRes = http.post(
    `${BASE}/v1/architecture/run/${encodeURIComponent(runId)}/execute`,
    "{}",
    { headers: headers(), tags: { endpoint: "realmode_execute" } }
  );
  executeDuration.add(Date.now() - executeStart);

  check(executeRes, {
    "execute 2xx": (r) => r.status >= 200 && r.status < 300,
  });

  const pollStart = Date.now();
  let runReady = false;

  while (Date.now() - pollStart < POLL_TIMEOUT_MS) {
    sleep(POLL_INTERVAL_MS / 1000);

    const statusRes = http.get(
      `${BASE}/v1/authority/runs/${encodeURIComponent(runId)}`,
      { headers: headers(), tags: { endpoint: "realmode_poll" } }
    );

    if (statusRes.status >= 200 && statusRes.status < 300) {
      try {
        const j = JSON.parse(statusRes.body);
        const status = (j.status || j.Status || "").toLowerCase();

        if (status === "completed" || status === "committed" || status === "readytocommit") {
          runReady = true;
          break;
        }

        if (status === "failed" || status === "faulted") {
          failCount.add(1);
          return;
        }
      } catch {
        /* continue polling */
      }
    }
  }

  pollWait.add(Date.now() - pollStart);

  if (!runReady) {
    failCount.add(1);
    return;
  }

  const commitStart = Date.now();
  const commitRes = http.post(
    `${BASE}/v1/architecture/run/${encodeURIComponent(runId)}/commit`,
    "{}",
    { headers: headers(), tags: { endpoint: "realmode_commit" } }
  );
  commitDuration.add(Date.now() - commitStart);

  const commitOk = check(commitRes, {
    "commit 2xx": (r) => r.status >= 200 && r.status < 300,
  });

  if (!commitOk) {
    failCount.add(1);
    return;
  }

  let manifestId = null;

  try {
    const j = JSON.parse(commitRes.body);
    manifestId = j.manifest?.manifestId || j.Manifest?.ManifestId;
  } catch {
    /* ignore */
  }

  if (manifestId) {
    const manifestStart = Date.now();
    const manifestRes = http.get(
      `${BASE}/v1/authority/manifests/${encodeURIComponent(manifestId)}/summary`,
      { headers: headers(), tags: { endpoint: "realmode_manifest" } }
    );
    manifestDuration.add(Date.now() - manifestStart);

    check(manifestRes, {
      "manifest summary 2xx": (r) => r.status >= 200 && r.status < 300,
    });
  }

  const e2eTotal = Date.now() - e2eStart;
  e2eWallClock.add(e2eTotal);
  successCount.add(1);
}

export function handleSummary(data) {
  const defaultPath = "tests/load/results/real-mode-e2e-benchmark.json";
  const out = __ENV.K6_SUMMARY_PATH || defaultPath;

  return {
    stdout: textSummary(data),
    [out]: JSON.stringify(data, null, 2),
  };
}

function textSummary(data) {
  const metrics = data.metrics || {};
  const e2e = metrics.e2e_wall_clock_ms || {};
  const vals = e2e.values || {};

  const lines = [
    "",
    "=== Real-Mode E2E Benchmark ===",
    `  Iterations:  ${ITERATIONS}`,
    `  Target:      ${BASE}`,
    "",
    `  e2e_wall_clock  p50=${fmt(vals["p(50)"])}  p95=${fmt(vals["p(95)"])}  p99=${fmt(vals["p(99)"])}  max=${fmt(vals.max)}`,
    "",
  ];

  for (const step of ["step_create_ms", "step_execute_ms", "step_poll_wait_ms", "step_commit_ms", "step_manifest_retrieve_ms"]) {
    const m = (metrics[step] || {}).values || {};
    lines.push(`  ${step.padEnd(26)} p50=${fmt(m["p(50)"])}  p95=${fmt(m["p(95)"])}  max=${fmt(m.max)}`);
  }

  const success = ((metrics.e2e_success_count || {}).values || {}).count || 0;
  const fail = ((metrics.e2e_fail_count || {}).values || {}).count || 0;
  lines.push("");
  lines.push(`  Success: ${success}  Fail: ${fail}`);
  lines.push("");

  return lines.join("\n");
}

function fmt(v) {
  if (v === undefined || v === null) return "N/A";
  return `${Math.round(v)}ms`;
}

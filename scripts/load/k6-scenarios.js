/**
 * Multi-scenario k6 profile for read-heavy API paths (compare, run detail, advisory list).
 *
 * Prerequisites:
 *   - API running with ReadAuthority (e.g. X-Api-Key or JWT).
 *   - Scope headers aligned with seeded data (defaults match dev ScopeIds).
 *
 * Usage:
 *   k6 run scripts/load/k6-scenarios.js
 *   k6 run --vus 10 --duration 30s -e ARCHIFORGE_API_KEY=secret scripts/load/k6-scenarios.js
 *
 * Env:
 *   ARCHIFORGE_BASE_URL     (default http://127.0.0.1:5128)
 *   ARCHIFORGE_API_KEY      X-Api-Key (omit for anonymous-only local setups — may 401)
 *   ARCHIFORGE_TENANT_ID    x-tenant-id GUID
 *   ARCHIFORGE_WORKSPACE_ID x-workspace-id GUID
 *   ARCHIFORGE_PROJECT_ID   x-project-id GUID
 *   ARCHIFORGE_COMPARE_BASE_RUN_ID / ARCHIFORGE_COMPARE_TARGET_RUN_ID  (for compare scenario)
 *   ARCHIFORGE_RUN_ID       run GUID for run-detail + advisory scenarios
 *
 * Write path (opt-in — creates runs; use Simulator mode + non-prod data):
 *   ARCHIFORGE_LOAD_TEST_WRITES=true  → adds POST /v1/architecture/request (requires ExecuteAuthority on the key).
 *   K6_ARCH_REQUEST_VUS / K6_ARCH_REQUEST_DURATION tune the write scenario.
 */

import http from "k6/http";
import { check, sleep } from "k6";

const base = (__ENV.ARCHIFORGE_BASE_URL || "http://127.0.0.1:5128").replace(/\/$/, "");
const apiKey = __ENV.ARCHIFORGE_API_KEY || "";
const tenant = __ENV.ARCHIFORGE_TENANT_ID || "11111111-1111-1111-1111-111111111111";
const workspace = __ENV.ARCHIFORGE_WORKSPACE_ID || "22222222-2222-2222-2222-222222222222";
const project = __ENV.ARCHIFORGE_PROJECT_ID || "33333333-3333-3333-3333-333333333333";

const compareBase = __ENV.ARCHIFORGE_COMPARE_BASE_RUN_ID || "00000000-0000-0000-0000-000000000001";
const compareTarget = __ENV.ARCHIFORGE_COMPARE_TARGET_RUN_ID || "00000000-0000-0000-0000-000000000002";
const runId = __ENV.ARCHIFORGE_RUN_ID || "00000000-0000-0000-0000-000000000001";

const loadTestWrites = __ENV.ARCHIFORGE_LOAD_TEST_WRITES === "true";

const architectureRequestScenario = loadTestWrites
  ? {
      architecture_request: {
        executor: "constant-vus",
        vus: Number(__ENV.K6_ARCH_REQUEST_VUS || 2),
        duration: __ENV.K6_ARCH_REQUEST_DURATION || "20s",
        exec: "postArchitectureRequest",
        startTime: "15s",
      },
    }
  : {};

function headers() {
  const h = {
    Accept: "application/json",
    "x-tenant-id": tenant,
    "x-workspace-id": workspace,
    "x-project-id": project,
  };
  if (apiKey) {
    h["X-Api-Key"] = apiKey;
  }
  return h;
}

function okRead(res) {
  return (
    (res.status >= 200 && res.status < 300) ||
    res.status === 404 ||
    res.status === 401 ||
    res.status === 429
  );
}

export const options = {
  scenarios: {
    compare: {
      executor: "constant-vus",
      vus: Number(__ENV.K6_COMPARE_VUS || 3),
      duration: __ENV.K6_COMPARE_DURATION || "20s",
      exec: "compare",
      startTime: "0s",
    },
    run_detail: {
      executor: "constant-vus",
      vus: Number(__ENV.K6_RUN_DETAIL_VUS || 3),
      duration: __ENV.K6_RUN_DETAIL_DURATION || "20s",
      exec: "runDetail",
      startTime: "5s",
    },
    advisory_list: {
      executor: "constant-vus",
      vus: Number(__ENV.K6_ADVISORY_VUS || 2),
      duration: __ENV.K6_ADVISORY_DURATION || "20s",
      exec: "advisoryRecommendations",
      startTime: "10s",
    },
    ...architectureRequestScenario,
  },
  thresholds: {
    http_req_failed: ["rate<0.99"],
  },
};

export function compare() {
  const url = `${base}/v1/compare?baseRunId=${compareBase}&targetRunId=${compareTarget}`;
  const res = http.get(url, { headers: headers() });
  check(res, { "compare 2xx/404/401/429": (r) => okRead(r) });
  sleep(0.05);
}

export function runDetail() {
  const url = `${base}/v1/architecture/run/${runId}`;
  const res = http.get(url, { headers: headers() });
  check(res, { "run detail 2xx/404/401/429": (r) => okRead(r) });
  sleep(0.05);
}

export function advisoryRecommendations() {
  const url = `${base}/v1/advisory/runs/${runId}/recommendations`;
  const res = http.get(url, { headers: headers() });
  check(res, { "advisory list 2xx/404/401/429": (r) => okRead(r) });
  sleep(0.05);
}

export function postArchitectureRequest() {
  const url = `${base}/v1/architecture/request`;
  const payload = JSON.stringify({
    requestId: `k6-${__VU}-${__ITER}-${Date.now()}`,
    description:
      "Simulator-mode k6 write load: gated by ARCHIFORGE_LOAD_TEST_WRITES; POST /v1/architecture/request.",
    systemName: "K6SimulatorWrites",
    environment: "loadtest",
    cloudProvider: "Azure",
    constraints: [],
    requiredCapabilities: [],
    assumptions: [],
    inlineRequirements: [],
    documents: [],
    policyReferences: [],
    topologyHints: [],
    securityBaselineHints: [],
    infrastructureDeclarations: [],
  });
  const h = headers();
  h["Content-Type"] = "application/json";
  const res = http.post(url, payload, { headers: h });
  check(res, {
    "architecture request acceptable status": (r) =>
      (r.status >= 200 && r.status < 300) ||
      r.status === 400 ||
      r.status === 409 ||
      r.status === 401 ||
      r.status === 403 ||
      r.status === 429 ||
      r.status === 503,
  });
  sleep(0.2);
}

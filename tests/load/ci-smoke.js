/**
 * k6 CI smoke — read + write operator API paths (DevelopmentBypass-friendly).
 * Scenarios: health (live+ready), version, create_run, list_runs, audit_search, get_run_detail, client_error_telemetry.
 * Run: BASE_URL=http://127.0.0.1:5128 k6 run tests/load/ci-smoke.js --summary-export /tmp/k6-ci-summary.json
 */
import http from "k6/http";
import { check } from "k6";

const BASE = __ENV.ARCHLUCID_BASE_URL || __ENV.BASE_URL || "http://127.0.0.1:5128";

function req(scenario, method, url, body = null) {
  const params = {
    headers: {
      "X-Correlation-ID": `k6-ci-${scenario}-${__VU}-${__ITER}`,
      Accept: "application/json",
    },
    tags: { k6ci: scenario },
  };

  if (body !== null) {
    params.headers["Content-Type"] = "application/json";
  }

  if (method === "GET") {
    return http.get(url, params);
  }

  return http.request(method, url, body, params);
}

export function healthFn() {
  let r = req("health_live", "GET", `${BASE}/health/live`);
  check(r, { "health live 200": (res) => res.status === 200 });

  r = req("health_ready", "GET", `${BASE}/health/ready`);
  check(r, { "health ready 200": (res) => res.status === 200 });
}

export function createRunFn() {
  const body = JSON.stringify({
    requestId: `k6-ci-${__VU}-${__ITER}-${Date.now()}`,
    description: "k6 CI smoke write-path test",
    systemName: "K6CiSmokeSystem",
    environment: "prod",
    cloudProvider: 1,
    constraints: [],
    requiredCapabilities: ["SQL"],
    assumptions: [],
    priorManifestVersion: null,
  });

  const r = req("create_run", "POST", `${BASE}/v1/architecture/request`, body);
  check(r, { "create run 2xx": (res) => res.status >= 200 && res.status < 300 });
}

export function listRunsFn() {
  const r = req("list_runs", "GET", `${BASE}/v1/architecture/runs`);
  check(r, { "list runs 200": (res) => res.status === 200 });
}

export function auditSearchFn() {
  const r = req("audit_search", "GET", `${BASE}/v1/audit/search?take=20`);
  check(r, { "audit search 200": (res) => res.status === 200 });
}

export function versionFn() {
  const r = req("version", "GET", `${BASE}/version`);
  check(r, { "version 200": (res) => res.status === 200 });
}

export function getRunDetailFn() {
  const list = req("list_for_get_run", "GET", `${BASE}/v1/architecture/runs`);
  check(list, { "list for get run 200": (res) => res.status === 200 });

  let runId = null;

  try {
    const rows = JSON.parse(list.body);

    if (Array.isArray(rows) && rows.length > 0 && rows[0].runId) {
      runId = rows[0].runId;
    }
  } catch {
    /* ignore parse errors — second check will fail clearly */
  }

  if (runId === null || runId === undefined) {
    return;
  }

  const detail = req("get_run_detail", "GET", `${BASE}/v1/architecture/run/${encodeURIComponent(runId)}`);
  check(detail, { "get run detail 200": (res) => res.status === 200 });
}

export function clientErrorTelemetryFn() {
  const body = JSON.stringify({
    message: "k6 ci smoke diagnostics probe",
    pathname: "/k6-ci-smoke-probe",
    context: { source: "k6-ci-smoke" },
  });
  const r = req("client_error_telemetry", "POST", `${BASE}/v1/diagnostics/client-error`, body);
  check(r, { "client error telemetry 204": (res) => res.status === 204 });
}

export const options = {
  scenarios: {
    health: {
      executor: "constant-vus",
      vus: 5,
      duration: "20s",
      exec: "healthFn",
    },
    create_run: {
      executor: "constant-vus",
      vus: 2,
      duration: "30s",
      exec: "createRunFn",
    },
    list_runs: {
      executor: "constant-vus",
      startTime: "5s",
      vus: 3,
      duration: "20s",
      exec: "listRunsFn",
    },
    audit_search: {
      executor: "constant-vus",
      vus: 2,
      duration: "20s",
      exec: "auditSearchFn",
    },
    version: {
      executor: "constant-vus",
      vus: 2,
      duration: "20s",
      exec: "versionFn",
    },
    get_run_detail: {
      executor: "constant-vus",
      startTime: "8s",
      vus: 2,
      duration: "20s",
      exec: "getRunDetailFn",
    },
    client_error_telemetry: {
      executor: "constant-vus",
      startTime: "10s",
      vus: 1,
      duration: "18s",
      exec: "clientErrorTelemetryFn",
    },
  },
  thresholds: {
    "http_req_failed": ["rate<0.02"],
    // Split tags: live stays lightweight; ready runs dependency probes (SQL, etc.) and is noisy on Actions — do not merge into one threshold.
    "http_req_duration{k6ci:health_live}": ["p(95)<500"],
    "http_req_duration{k6ci:health_ready}": ["p(95)<1500"],
    "http_req_duration{k6ci:create_run}": ["p(95)<3000"],
    "http_req_duration{k6ci:list_runs}": ["p(95)<1500"],
    "http_req_duration{k6ci:audit_search}": ["p(95)<1500"],
    "http_req_duration{k6ci:version}": ["p(95)<1500"],
    "http_req_duration{k6ci:list_for_get_run}": ["p(95)<1500"],
    "http_req_duration{k6ci:get_run_detail}": ["p(95)<2500"],
    "http_req_duration{k6ci:client_error_telemetry}": ["p(95)<1500"],
  },
};

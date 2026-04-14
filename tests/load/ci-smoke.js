/**
 * k6 CI smoke — read + write operator API paths (DevelopmentBypass-friendly).
 * Run: BASE_URL=http://127.0.0.1:5128 k6 run tests/load/ci-smoke.js --summary-export /tmp/k6-ci-summary.json
 */
import http from "k6/http";
import { check } from "k6";

const BASE = __ENV.BASE_URL || "http://127.0.0.1:5128";

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
  let r = req("health", "GET", `${BASE}/health/live`);
  check(r, { "health live 200": (res) => res.status === 200 });

  r = req("health", "GET", `${BASE}/health/ready`);
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
  },
  thresholds: {
    "http_req_failed": ["rate<0.02"],
    "http_req_duration{k6ci:health}": ["p(95)<300"],
    "http_req_duration{k6ci:create_run}": ["p(95)<3000"],
    "http_req_duration{k6ci:list_runs}": ["p(95)<1500"],
    "http_req_duration{k6ci:audit_search}": ["p(95)<1500"],
  },
};

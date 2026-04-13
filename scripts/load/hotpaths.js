/**
 * k6 smoke/load script for five API hot paths (Improvement 9 baseline):
 *   1) POST /v1.0/architecture/request  — create run
 *   2) GET  /v1.0/architecture/runs     — list runs
 *   3) GET  /v1.0/architecture/manifest/v1 — get manifest (404 acceptable on empty DB)
 *   4) GET  /v1.0/architecture/comparisons — compare history (paged)
 *   5) GET  /v1.0/retrieval/search     — semantic search (“ask” path)
 *
 * Env:
 *   BASE_URL  — API root (default http://127.0.0.1:5000)
 *   API_KEY   — optional X-Api-Key when not using DevelopmentBypass
 */
import http from "k6/http";
import { check, sleep } from "k6";

const base = (__ENV.BASE_URL || "http://127.0.0.1:5000").replace(/\/$/, "");
const version = __ENV.API_VERSION || "v1.0";

export const options = {
  scenarios: {
    hotpaths: {
      executor: "constant-vus",
      vus: Number(__ENV.VUS || 5),
      duration: __ENV.DURATION || "2m",
    },
  },
  thresholds: {
    // After a baseline: set p(95) from `python scripts/ci/print_k6_summary_metrics.py k6-summary.json` (suggested cap line).
    http_req_duration: ["p(95)<8000"],
    checks: ["rate>0.85"],
  },
};

function headers() {
  const h = { "Content-Type": "application/json", Accept: "application/json" };
  const key = __ENV.API_KEY;
  if (key) {
    h["X-Api-Key"] = key;
  }
  return h;
}

export default function () {
  const payload = JSON.stringify({
    requestId: `k6-${__VU}-${__ITER}-${Date.now()}`,
    description:
      "k6 load-test baseline: synthetic architecture request for throughput and latency measurement only.",
    systemName: "K6LoadTest",
    environment: "test",
    cloudProvider: 1,
    constraints: [],
    requiredCapabilities: [],
    assumptions: [],
  });

  let res = http.post(`${base}/${version}/architecture/request`, payload, {
    headers: headers(),
    tags: { name: "create_run" },
  });
  check(res, {
    "create_run 2xx": (r) => r.status >= 200 && r.status < 300,
  });

  res = http.get(`${base}/${version}/architecture/runs`, {
    headers: headers(),
    tags: { name: "list_runs" },
  });
  check(res, { "list_runs 200": (r) => r.status === 200 });

  res = http.get(`${base}/${version}/architecture/manifest/v1`, {
    headers: headers(),
    tags: { name: "get_manifest" },
  });
  check(res, {
    "get_manifest 200_or_404": (r) => r.status === 200 || r.status === 404,
  });

  res = http.get(`${base}/${version}/architecture/comparisons?limit=10&skip=0`, {
    headers: headers(),
    tags: { name: "list_comparisons" },
  });
  check(res, { "comparisons 200": (r) => r.status === 200 });

  res = http.get(
    `${base}/${version}/retrieval/search?q=loadtest&topK=8`,
    { headers: headers(), tags: { name: "retrieval_search" } },
  );
  check(res, {
    "retrieval_search 200_or_400": (r) => r.status === 200 || r.status === 400,
  });

  sleep(Number(__ENV.SLEEP_SEC || 1));
}

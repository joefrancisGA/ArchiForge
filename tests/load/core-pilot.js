/**
 * k6 — core pilot + read-heavy + mixed (DevelopmentBypass, InMemory-friendly).
 *
 *   k6 run tests/load/core-pilot.js
 *   K6_LOAD_PROFILE=read k6 run tests/load/core-pilot.js
 *   K6_LOAD_PROFILE=mixed k6 run tests/load/core-pilot.js
 *
 * Default URL: http://127.0.0.1:5001 (ARCHLUCID_BASE_URL or BASE_URL)
 * Full baseline durations: 5m / 5m / 10m. Quick mode: K6_COMPRESS=1
 */
import http from "k6/http";
import { check } from "k6";

const Base = __ENV.ARCHLUCID_BASE_URL || __ENV.BASE_URL || "http://127.0.0.1:5001";
const projectSlug = __ENV.ARCHLUCID_AUTHORITY_PROJECT || "default";
const loadProfile = (__ENV.K6_LOAD_PROFILE || "core").toLowerCase();
const compress = __ENV.K6_COMPRESS === "1" || __ENV.K6_COMPRESS === "true";
const dCore = compress ? "20s" : __ENV.K6_CORE_DURATION || "5m";
const dRead = compress ? "20s" : __ENV.K6_READ_DURATION || "5m";
const dMixed = compress ? "30s" : __ENV.K6_MIXED_DURATION || "10m";
const vuCore = Number(__ENV.K6_CORE_VUS || 10);
const vuRead = Number(__ENV.K6_READ_VUS || 50);
const vuWrite = Number(__ENV.K6_MIXED_WRITERS || 5);
const vuReadMixed = Number(__ENV.K6_MIXED_READERS || 25);
const _dateEnv = __ENV.K6_BASELINE_DATE;
const _today =
  _dateEnv && _dateEnv.length
    ? _dateEnv
    : (() => {
        const d = new Date();
        const m = d.getMonth() + 1;
        const day = d.getDate();
        return d.getFullYear() + "-" + (m < 10 ? "0" : "") + m + "-" + (day < 10 ? "0" : "") + day;
      })();
const resultPath = __ENV.K6_BASELINE_PATH || `tests/load/results/baseline-${_today}.json`;

const pilotThresholds = {
  http_req_failed: ["rate<0.02"],
  "http_req_duration{endpoint:pilot_request}": ["p(95)<2000", "p(99)<5000"],
  "http_req_duration{endpoint:pilot_execute}": ["p(95)<120000"],
  "http_req_duration{endpoint:pilot_commit}": ["p(95)<60000"],
  "http_req_duration{endpoint:pilot_manifest_summary}": ["p(95)<2000", "p(99)<5000"],
  "http_req_duration{endpoint:pilot_artifacts_list}": ["p(95)<2000", "p(99)<5000"],
};

const readThresholds = {
  http_req_failed: ["rate<0.02"],
  "http_req_duration{endpoint:read_list_runs}": ["p(95)<2000", "p(99)<5000"],
  "http_req_duration{endpoint:read_run_detail}": ["p(95)<4000", "p(99)<8000"],
  // read_manifest_summary is only emitted when goldenManifest is present; do not threshold it.
};

function makeOptions() {
  if (loadProfile === "read") {
    return {
      scenarios: {
        read: {
          executor: "constant-vus",
          vus: vuRead,
          duration: dRead,
          exec: "readHeavy",
        },
      },
      thresholds: readThresholds,
    };
  }
  if (loadProfile === "mixed") {
    return {
      scenarios: {
        writers: {
          executor: "constant-vus",
          vus: vuWrite,
          duration: dMixed,
          exec: "corePilot",
        },
        readers: {
          executor: "constant-vus",
          vus: vuReadMixed,
          duration: dMixed,
          startTime: "0s",
          exec: "readHeavy",
        },
      },
      thresholds: Object.assign({}, pilotThresholds, readThresholds),
    };
  }
  return {
    scenarios: {
      core: {
        executor: "constant-vus",
        vus: vuCore,
        duration: dCore,
        exec: "corePilot",
      },
    },
    thresholds: pilotThresholds,
  };
}

export const options = makeOptions();

function h(sc) {
  const o = {
    "X-Correlation-ID": `k6-cp-${sc}-vu${__VU}-it${__ITER}-${Date.now()}`,
    Accept: "application/json",
  };
  if (__ENV.ARCHLUCID_API_KEY) o["X-Api-Key"] = __ENV.ARCHLUCID_API_KEY;
  return o;
}

function postRequest(tag, path, body) {
  return http.post(path, body, {
    headers: Object.assign({}, h("core"), { "Content-Type": "application/json" }),
    tags: { endpoint: tag },
    timeout: "120s",
  });
}

function postNoBody(tag, path) {
  return http.post(path, null, { headers: h("core"), tags: { endpoint: tag }, timeout: "120s" });
}

export function corePilot() {
  const b = postRequest(
    "pilot_request",
    `${Base}/v1/architecture/request`,
    JSON.stringify({
      requestId: `k6-pilot-${__VU}-${__ITER}-${Date.now()}`,
      description: "k6 core pilot run — long enough request description for validation (10+ chars).",
      systemName: "K6CorePilot",
      environment: "prod",
      cloudProvider: 1,
      constraints: [],
      requiredCapabilities: ["SQL"],
      assumptions: [],
      priorManifestVersion: null,
    })
  );
  if (!check(b, { pilot_request_2xx: (r) => r.status >= 200 && r.status < 300 })) return;
  let j;
  try {
    j = JSON.parse(b.body);
  } catch {
    return;
  }
  const runId = j.run && j.run.runId;
  if (!runId) return;
  const ex = postNoBody("pilot_execute", `${Base}/v1/architecture/run/${runId}/execute`);
  if (!check(ex, { pilot_execute_200: (r) => r.status === 200 })) return;
  const co = postNoBody("pilot_commit", `${Base}/v1/architecture/run/${runId}/commit`);
  if (!check(co, { pilot_commit_200: (r) => r.status === 200 })) return;
  let cj;
  try {
    cj = JSON.parse(co.body);
  } catch {
    return;
  }
  const mid = cj.manifest && cj.manifest.manifestId;
  if (!mid) return;
  const m = http.get(`${Base}/v1/authority/manifests/${mid}/summary`, {
    headers: h("read"),
    tags: { endpoint: "pilot_manifest_summary" },
  });
  check(m, { pilot_manifest_summary_200: (r) => r.status === 200 });
  const a = http.get(`${Base}/v1/artifacts/manifests/${mid}`, {
    headers: h("read"),
    tags: { endpoint: "pilot_artifacts_list" },
  });
  check(a, { pilot_artifacts_list_200: (r) => r.status === 200 });
}

export function readHeavy() {
  const listUrl = `${Base}/v1/authority/projects/${encodeURIComponent(projectSlug)}/runs?take=20`;
  const list = http.get(listUrl, { headers: h("read"), tags: { endpoint: "read_list_runs" } });
  if (!check(list, { read_list_200: (r) => r.status === 200 })) return;
  let rows;
  try {
    rows = JSON.parse(list.body);
  } catch {
    return;
  }
  if (!Array.isArray(rows) || rows.length === 0) return;
  const runId = rows[0].runId;
  if (!runId) return;
  const d = http.get(`${Base}/v1/authority/runs/${runId}`, {
    headers: h("read"),
    tags: { endpoint: "read_run_detail" },
  });
  if (!check(d, { read_run_detail_200: (r) => r.status === 200 })) return;
  let det;
  try {
    det = JSON.parse(d.body);
  } catch {
    return;
  }
  const g = det.goldenManifest;
  if (!g || !g.manifestId) return;
  const sm = http.get(`${Base}/v1/authority/manifests/${g.manifestId}/summary`, {
    headers: h("read"),
    tags: { endpoint: "read_manifest_summary" },
  });
  check(sm, { read_manifest_summary_200: (r) => r.status === 200 });
}

export function handleSummary(data) {
  const envelope = {
    schema: "archlucid.k6-baseline-fragment.v1",
    path: resultPath,
    baseUrl: Base,
    loadProfile: loadProfile,
    compress: compress,
    k6: data,
  };
  return { [resultPath]: JSON.stringify(envelope, null, 2) };
}

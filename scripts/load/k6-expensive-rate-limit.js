/**
 * k6 load script: detect 429 responses under burst (expensive rate limit policy).
 * Usage: k6 run -e ARCHLUCID_BASE_URL=https://host:port scripts/load/k6-expensive-rate-limit.js
 *
 * Replace the default path with a safe expensive-class endpoint in your environment.
 */
import http from "k6/http";
import { check, sleep } from "k6";

const base = __ENV.ARCHLUCID_BASE_URL || "http://127.0.0.1:5128";
// Default: health is NOT rate-limited — swap for an execute/replay path when testing 429s.
const path = __ENV.ARCHLUCID_EXPENSIVE_PATH || "/health/live";

export default function () {
  const res = http.get(`${base.replace(/\/$/, "")}${path.startsWith("/") ? path : `/${path}`}`, {
    headers: { Accept: "application/json, text/plain, */*" },
  });

  check(res, {
    "status is 2xx or 429": (r) => (r.status >= 200 && r.status < 300) || r.status === 429,
  });
  sleep(0.05);
}

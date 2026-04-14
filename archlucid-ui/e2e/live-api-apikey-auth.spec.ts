/**
 * Production-like **ApiKey** auth gates. Skipped unless `LIVE_API_KEY` is set (see `docs/LIVE_E2E_AUTH_ASSUMPTIONS.md`).
 *
 * Env (CI `ui-e2e-live-apikey`):
 * - `LIVE_API_KEY` — admin key (must match `Authentication:ApiKey:AdminKey` on the API).
 * - `LIVE_API_KEY_READONLY` — optional; when set, asserts read-only cannot `POST /v1/architecture/request` (403).
 */
import { expect, test } from "@playwright/test";

import {
  isApiKeyMode,
  liveAcceptHeaders,
  liveApiBase,
  liveApiKeyReadOnly,
  liveJsonHeaders,
} from "./helpers/live-api-client";

test.describe("live-api-apikey-auth", () => {
  test.skip(!isApiKeyMode, "Set LIVE_API_KEY to run ApiKey auth production-like gates.");

  test.beforeAll(async ({ request }) => {
    const health = await request.get(`${liveApiBase}/health/ready`, { timeout: 60_000 });

    if (!health.ok()) {
      throw new Error(`Live API not ready at ${liveApiBase}/health/ready (status ${health.status()}).`);
    }
  });

  test("health/ready allows anonymous access (200 without X-Api-Key)", async ({ request }) => {
    const res = await request.get(`${liveApiBase}/health/ready`, {
      headers: { Accept: "application/json" },
    });

    expect(res.status()).toBe(200);
  });

  test("GET /v1/architecture/runs without API key returns 401", async ({ request }) => {
    const res = await request.get(`${liveApiBase}/v1/architecture/runs`, {
      headers: liveAcceptHeaders(""),
    });

    expect(res.status()).toBe(401);
  });

  test("GET /v1/architecture/runs with invalid API key returns 401", async ({ request }) => {
    const res = await request.get(`${liveApiBase}/v1/architecture/runs`, {
      headers: liveAcceptHeaders("definitely-not-the-configured-admin-key"),
    });

    expect(res.status()).toBe(401);
  });

  test("GET /v1/architecture/runs with valid admin key returns 200", async ({ request }) => {
    const res = await request.get(`${liveApiBase}/v1/architecture/runs`, {
      headers: liveAcceptHeaders(),
    });

    expect(res.status()).toBe(200);
  });

  test("readonly key: GET runs 200; POST /v1/architecture/request returns 403", async ({ request }) => {
    test.skip(!liveApiKeyReadOnly, "Set LIVE_API_KEY_READONLY to assert read-only key is denied for ExecuteAuthority.");

    const list = await request.get(`${liveApiBase}/v1/architecture/runs`, {
      headers: liveAcceptHeaders(liveApiKeyReadOnly),
    });

    expect(list.status()).toBe(200);

    const create = await request.post(`${liveApiBase}/v1/architecture/request`, {
      headers: liveJsonHeaders(liveApiKeyReadOnly),
      data: {
        requestId: `E2E-RO-${Date.now()}`,
        description:
          "Live E2E read-only key must not create runs (ExecuteAuthority requires Operator/Admin role).".padEnd(80, " "),
        systemName: "ReadOnlyKeyGate",
        environment: "prod",
        cloudProvider: 1,
        constraints: [] as string[],
        requiredCapabilities: ["SQL"],
        assumptions: [] as string[],
        priorManifestVersion: null as string | null,
      },
    });

    expect(create.status()).toBe(403);
  });
});

/**
 * Requires a running ArchLucid.Api (Sql + DevelopmentBypass by default in CI).
 * Not part of the mock `playwright.config.ts` suite — run:
 *   npx playwright test -c playwright.live.config.ts
 * Set `LIVE_API_URL` if the API is not on http://127.0.0.1:5128.
 */
import { expect, test } from "@playwright/test";

import {
  commitRun,
  createRun,
  executeRun,
  liveApiBase,
  searchAudit,
  waitForReadyForCommit,
  waitForRunDetailCommitted,
} from "./helpers/live-api-client";

test.describe("live-api-advisory-flow", () => {
  test.beforeAll(async ({ request }) => {
    const health = await request.get(`${liveApiBase}/health/ready`, { timeout: 60_000 });

    if (!health.ok()) {
      throw new Error(
        `Live API not ready at ${liveApiBase}/health/ready (status ${health.status()}). Start ArchLucid.Api with Sql + DevelopmentBypass.`,
      );
    }
  });

  test("schedule advisory scan after committed run and verify audit trail", async ({ request }) => {
    test.setTimeout(180_000);

    const createBody = {
      requestId: `E2E-LIVE-ADVISORY-${Date.now()}`,
      description: "Live E2E: advisory scan scheduling after commit.",
      systemName: "AdvisoryFlowTest",
      environment: "prod",
      cloudProvider: 1,
      constraints: [] as string[],
      requiredCapabilities: ["SQL"],
      assumptions: [] as string[],
      priorManifestVersion: null as string | null,
    };

    const { runId } = await createRun(request, createBody);
    test.info().annotations.push({ type: "e2e-run-id", description: runId });

    await executeRun(request, runId);
    await waitForReadyForCommit(request, runId, 90_000);
    await commitRun(request, runId);
    await waitForRunDetailCommitted(request, runId, 60_000);

    const scanRes = await request.post(`${liveApiBase}/v1/advisory/scans`, {
      data: { runId, description: "E2E advisory scan test" },
      headers: { Accept: "application/json", "Content-Type": "application/json" },
    });

    if (scanRes.status() === 404) {
      test.skip(true, "Advisory scan scheduling endpoint not available in this build");
      return;
    }

    expect
      .soft(scanRes.ok() || scanRes.status() === 409, `advisory scan POST expected 2xx or 409, got ${scanRes.status()}`)
      .toBe(true);

    const auditEvents = await searchAudit(request, { runId, take: "200" });
    const types = new Set(auditEvents.map((e) => e.eventType).filter(Boolean) as string[]);

    expect
      .soft(types.has("AdvisoryScanScheduled") || types.has("AdvisoryScanExecuted"), "Expected advisory audit event")
      .toBe(true);
  });
});

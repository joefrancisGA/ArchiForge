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
  getRunExportZip,
  liveApiBase,
  searchAudit,
  waitForReadyForCommit,
  waitForRunDetailCommitted,
} from "./helpers/live-api-client";

test.describe("live-api-replay-export", () => {
  test.beforeAll(async ({ request }) => {
    const health = await request.get(`${liveApiBase}/health/ready`, { timeout: 60_000 });

    if (!health.ok()) {
      throw new Error(
        `Live API not ready at ${liveApiBase}/health/ready (status ${health.status()}). Start ArchLucid.Api with Sql + DevelopmentBypass.`,
      );
    }
  });

  test("replay committed run and verify export + audit trail", async ({ request }) => {
    test.setTimeout(180_000);

    const createBody = {
      requestId: `E2E-LIVE-REPLAY-${Date.now()}`,
      description: "Live E2E: replay and re-export after commit.",
      systemName: "ReplayExportTest",
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

    const replayRes = await request.post(`${liveApiBase}/v1/replay/run/${runId}`, {
      headers: { Accept: "application/json" },
    });

    if (replayRes.status() === 404) {
      test.skip(true, "Replay endpoint not available in this build");
      return;
    }

    expect(replayRes.ok(), `replay POST expected 2xx, got ${replayRes.status()}`).toBe(true);

    const exportRes = await getRunExportZip(request, runId);

    expect(exportRes.ok(), `export GET expected 2xx, got ${exportRes.status()}`).toBe(true);

    const contentType = exportRes.headers()["content-type"] ?? "";

    expect
      .soft(
        contentType.includes("zip") || contentType.includes("octet-stream"),
        `export content-type should be zip/octet-stream, got ${contentType}`,
      )
      .toBe(true);

    const auditEvents = await searchAudit(request, { runId, take: "200" });
    const types = new Set(auditEvents.map((e) => e.eventType).filter(Boolean) as string[]);

    expect.soft(types.has("ReplayExecuted"), "Expected ReplayExecuted audit event").toBe(true);
    expect.soft(types.has("RunExported"), "Expected RunExported audit event").toBe(true);
  });
});

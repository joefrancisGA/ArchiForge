/**
 * Requires a running ArchLucid.Api (Sql + DevelopmentBypass by default in CI).
 * Default `playwright.config.ts` is live-backed; run `npx playwright test` (or mock: `-c playwright.mock.config.ts`).
 * Set `LIVE_API_URL` if the API is not on http://127.0.0.1:5128.
 *
 * Covers the operator-shell `/why-archlucid` Core Pilot proof page:
 *   - Best-effort POST /v1/demo/seed to materialize the Contoso Retail demo run (idempotent; 400/403/404
 *     are tolerated so the spec also passes against a build with `Demo:Enabled = false`).
 *   - Drive a tiny create → execute → commit cycle so the in-process counters incremented since process
 *     start are non-zero (the page surfaces them from `ArchLucid.Core.Diagnostics.ArchLucidInstrumentation`).
 *   - Navigate to /why-archlucid via the operator shell and assert the three live-data sections render.
 */
import { expect, test } from "@playwright/test";

import {
  commitRun,
  createRun,
  executeRun,
  liveApiBase,
  liveJsonHeaders,
  waitForReadyForCommit,
  waitForRunDetailCommitted,
} from "./helpers/live-api-client";

test.describe("live-api-why-archlucid", () => {
  test.beforeAll(async ({ request }) => {
    const health = await request.get(`${liveApiBase}/health/ready`, { timeout: 60_000 });

    if (!health.ok()) {
      throw new Error(
        `Live API not ready at ${liveApiBase}/health/ready (status ${health.status()}). Start ArchLucid.Api with Sql + DevelopmentBypass.`,
      );
    }

    // Best-effort: seed the Contoso Retail demo so the first-value-report endpoint has a committed run.
    // Tolerate 400 (Demo:Enabled = false), 403 (insufficient authority in non-bypass auth modes), and
    // 404 (non-Development environment) so this spec stays useful in CI configurations that disable seed.
    const seed = await request.post(`${liveApiBase}/v1/demo/seed`, {
      headers: liveJsonHeaders(),
      timeout: 60_000,
    });

    const seedStatus = seed.status();
    const seedAcceptable = seedStatus === 204 || seedStatus === 400 || seedStatus === 403 || seedStatus === 404;

    if (!seedAcceptable) {
      const body = await seed.text();
      throw new Error(`POST /v1/demo/seed unexpected status ${seedStatus}: ${body.slice(0, 500)}`);
    }
  });

  test("renders the proof page sections backed by the live API", async ({ page, request }) => {
    test.setTimeout(180_000);

    // Drive at least one full create → execute → commit so the in-process counters that the page
    // surfaces (`archlucid_runs_created_total`, `archlucid_findings_produced_total`) are non-zero.
    const createBody = {
      requestId: `E2E-WHY-ARCHLUCID-${Date.now()}`,
      description: "Live E2E: drive counters for /why-archlucid proof page.",
      systemName: "WhyArchLucidProof",
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

    await page.goto("/why-archlucid");

    await expect(page.getByRole("heading", { name: "Why ArchLucid", level: 1 })).toBeVisible({
      timeout: 60_000,
    });

    // Process counters section: snapshot endpoint should return at least one run since process start.
    const counters = page.getByTestId("why-archlucid-counters");

    await expect(counters).toBeVisible({ timeout: 30_000 });
    await expect(counters).toContainText("Runs created");
    await expect(counters).toContainText("Audit rows (demo scope)");
    await expect(counters).toContainText("archlucid_runs_created_total");

    // First-value-report section is always rendered; if the demo seed was accepted the Markdown body
    // is shown, otherwise the "demo run has not been committed yet" hint renders. Either is acceptable.
    const reportSection = page.getByTestId("why-archlucid-first-value-report");

    await expect(reportSection).toBeVisible();
    await expect(reportSection).toContainText("Sponsor first-value report");

    // Aggregate explanation section also always renders; with seed the citations panel is populated.
    const explanationSection = page.getByTestId("why-archlucid-run-explanation");

    await expect(explanationSection).toBeVisible();
    await expect(explanationSection).toContainText("Run explanation and citations");
    await expect(page.getByTestId("why-archlucid-citations")).toBeVisible();
  });
});

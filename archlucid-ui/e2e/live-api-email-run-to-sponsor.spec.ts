/**
 * Requires a running ArchLucid.Api (Sql + DevelopmentBypass by default in CI).
 * Default `playwright.config.ts` is live-backed; run `npx playwright test` (or mock: `-c playwright.mock.config.ts`).
 * Set `LIVE_API_URL` if the API is not on http://127.0.0.1:5128.
 *
 * Covers the **Time-to-Value** in-product CTA on `/runs/[runId]`:
 *   - Drive a full create → execute → commit cycle so the run-detail page renders the post-commit
 *     `EmailRunToSponsorBanner`.
 *   - Click the banner's primary action and assert the browser receives an `application/pdf` download
 *     whose body starts with the `%PDF` magic bytes (sponsor-shareable PDF projection of the
 *     canonical first-value-report Markdown).
 */
import { expect, test } from "@playwright/test";

import {
  commitRun,
  createRun,
  executeRun,
  liveApiBase,
  waitForReadyForCommit,
  waitForRunDetailCommitted,
} from "./helpers/live-api-client";

test.describe("live-api-email-run-to-sponsor", () => {
  test.beforeAll(async ({ request }) => {
    const health = await request.get(`${liveApiBase}/health/ready`, { timeout: 60_000 });

    if (!health.ok()) {
      throw new Error(
        `Live API not ready at ${liveApiBase}/health/ready (status ${health.status()}). Start ArchLucid.Api with Sql + DevelopmentBypass.`,
      );
    }
  });

  test("post-commit banner downloads a PDF derived from the first-value-report Markdown", async ({
    page,
    request,
  }) => {
    test.setTimeout(180_000);

    const createBody = {
      requestId: `E2E-EMAIL-SPONSOR-${Date.now()}`,
      description: "Live E2E: drive a committed run so the sponsor PDF CTA renders on /runs/[runId].",
      systemName: "EmailSponsorPdf",
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

    await page.goto(`/runs/${runId}`);

    const banner = page.getByTestId("email-run-to-sponsor-banner");

    await expect(banner).toBeVisible({ timeout: 60_000 });
    await expect(banner).toContainText(/Time to value/i);

    const primary = page.getByTestId("email-run-to-sponsor-primary-action");

    await expect(primary).toBeEnabled();

    // Capture the browser download triggered by the same-origin `<a download>` click in the helper.
    const downloadPromise = page.waitForEvent("download", { timeout: 60_000 });
    await primary.click();
    const download = await downloadPromise;

    const filename = download.suggestedFilename();

    expect(filename).toMatch(/first-value-report/i);
    expect(filename.toLowerCase()).toMatch(/\.pdf$/);

    // Also verify the bytes are a real PDF (not an HTML error masquerading as a download).
    const path = await download.path();

    if (path === null) {
      throw new Error("Browser download has no resolved path; cannot verify PDF magic bytes.");
    }

    const fs = await import("node:fs/promises");
    const buf = await fs.readFile(path);

    expect(buf.byteLength).toBeGreaterThan(64);
    expect(buf.subarray(0, 4).toString("utf8")).toBe("%PDF");
  });
});

/**
 * Requires a running ArchLucid.Api (Sql + DevelopmentBypass by default in CI).
 * Not part of the mock `playwright.config.ts` suite — run:
 *   npx playwright test -c playwright.live.config.ts
 * Set `LIVE_API_URL` if the API is not on http://127.0.0.1:5128.
 */
import { expect, test } from "@playwright/test";

import {
  approveGovernanceRequest,
  commitRun,
  createApprovalRequest,
  createRun,
  executeRun,
  getRunDetailsWithTransientRetries,
  getRunExportZip,
  listArchitectureRuns,
  liveApiBase,
  liveAuthActorName,
  livePeerReviewerActorName,
  normalizeRunIdForCompare,
  waitForArchitectureRunListCommitted,
  waitForReadyForCommit,
  waitForRunDetailCommitted,
  postGovernanceApproveRaw,
  searchAudit,
} from "./helpers/live-api-client";

const liveE2eForensics: { runId?: string; approvalRequestId?: string; auditCorrelationId?: string } = {};

test.describe("live-api-journey", () => {
  test.afterAll(() => {
    if (liveE2eForensics.runId) {
      console.log(
        `[live-api-journey] runId=${liveE2eForensics.runId} approvalRequestId=${liveE2eForensics.approvalRequestId ?? ""} auditCorrelationId=${liveE2eForensics.auditCorrelationId ?? ""}`,
      );
    }
  });

  test.beforeAll(async ({ request }) => {
    const health = await request.get(`${liveApiBase}/health/ready`, { timeout: 60_000 });

    if (!health.ok()) {
      throw new Error(
        `Live API not ready at ${liveApiBase}/health/ready (status ${health.status()}). Start ArchLucid.Api with Sql + DevelopmentBypass.`,
      );
    }
  });

  test("operator happy path: create → execute → commit → manifest → export → governance → audit", async ({
    page,
    request,
  }) => {
    test.setTimeout(180_000);

    const createBody = {
      requestId: `E2E-LIVE-${Date.now()}`,
      description:
        "Design a secure Azure RAG system for enterprise internal documents using Azure AI Search, managed identity, private endpoints, SQL metadata storage, and moderate cost sensitivity.",
      systemName: "EnterpriseRag",
      environment: "prod",
      cloudProvider: 1,
      constraints: ["Private endpoints required", "Use managed identity"],
      requiredCapabilities: ["Azure AI Search", "SQL", "Managed Identity", "Private Networking"],
      assumptions: [] as string[],
      priorManifestVersion: null as string | null,
    };

    const { runId } = await createRun(request, createBody);

    liveE2eForensics.runId = runId;
    test.info().annotations.push({ type: "e2e-run-id", description: runId });

    await executeRun(request, runId);

    await waitForReadyForCommit(request, runId, 90_000);

    const commitJson = await commitRun(request, runId);
    const manifestVersion = commitJson.manifest?.metadata?.manifestVersion;

    if (!manifestVersion) {
      throw new Error("Commit response missing manifest.metadata.manifestVersion");
    }

    await waitForRunDetailCommitted(request, runId, 60_000);

    const afterCommit = await getRunDetailsWithTransientRetries(request, runId);
    const goldenManifestId = afterCommit.run?.goldenManifestId;

    if (!goldenManifestId) {
      throw new Error("Run detail after commit missing run.goldenManifestId");
    }

    await page.goto("/runs");

    await expect(page.getByRole("heading", { name: /runs/i }).first()).toBeVisible({ timeout: 60_000 });

    await page.goto(`/runs/${runId}`);

    await expect(page.getByRole("heading", { name: "Run detail", level: 2 })).toBeVisible({ timeout: 60_000 });

    // RSC: wait for loading shell to detach (JWT / slow SQL); error states keep h2 without the Run metadata section.
    await expect(page.getByText(/Loading run detail/)).toHaveCount(0, { timeout: 60_000 });

    // Scope to the **Run** summary section (same as mock `run-manifest-journey`). "Pipeline progress" also shows Run ID in a <code>.
    const runSummarySection = page
      .locator("main")
      .locator("section")
      .filter({ has: page.getByRole("heading", { name: "Run", level: 3 }) });

    await expect(runSummarySection.getByText("Run ID:", { exact: true })).toBeVisible({ timeout: 60_000 });

    // Architecture create returns RunId "N" (no hyphens); authority run detail uses JSON Guid "D" (hyphenated).
    const runIdCode = runSummarySection.locator("code").first();

    await expect(runIdCode).toBeVisible({ timeout: 60_000 });

    const shownRunId = (await runIdCode.textContent())?.trim() ?? "";

    expect(normalizeRunIdForCompare(shownRunId)).toBe(normalizeRunIdForCompare(runId));

    // Prefer href: accessible name for the GUID link can differ by a11y tree / Next Link behavior in Chromium CI.
    const manifestHref = `/manifests/${goldenManifestId}`;
    const manifestLink = page.locator(`a[href="${manifestHref}"]`);

    await expect(
      manifestLink,
      `Golden manifest link missing (expected href ${manifestHref}). Server run detail may lack goldenManifestId or UI/API mismatch.`,
    ).toBeVisible({ timeout: 60_000 });

    await manifestLink.click();

    const manifestMain = page.locator("main");

    // RSC: `h2` can stream before the summary `<p>`; default 5s is flaky in CI. Loading.tsx has no `h2`, so wait
    // for the loading notice to detach first, then allow up to 60s for success markup (or surface summary API errors).
    await expect(manifestMain.getByText(/Fetching manifest summary and artifacts/)).toHaveCount(0, {
      timeout: 60_000,
    });

    await expect(manifestMain.getByRole("heading", { name: "Manifest", level: 2 })).toBeVisible({ timeout: 60_000 });

    // Success path uses <strong>Manifest ID:</strong>; error/malformed/empty-summary branches omit it.
    await expect(
      manifestMain.locator("strong", { hasText: "Manifest ID:" }),
      "Manifest summary success UI missing — check GET /v1/authority/manifests/{id}/summary, proxy scope, and operator-response-guards.",
    ).toBeVisible({ timeout: 60_000 });

    await expect(manifestMain.getByText(goldenManifestId)).toBeVisible({ timeout: 60_000 });
    await expect(manifestMain.getByRole("heading", { name: "Artifacts", level: 3 })).toBeVisible({ timeout: 60_000 });
    await expect(manifestMain.getByRole("table")).toBeVisible({ timeout: 60_000 });
    await expect(manifestMain.getByRole("columnheader", { name: "Artifact" })).toBeVisible({ timeout: 60_000 });
    await expect(manifestMain.getByRole("link", { name: "Download bundle (ZIP)" })).toBeVisible({ timeout: 60_000 });

    const exportRes = await getRunExportZip(request, runId);

    expect(exportRes.ok(), `GET run export expected 200, got ${exportRes.status()}`).toBeTruthy();

    const exportCt = exportRes.headers()["content-type"] ?? "";

    expect(
      exportCt.includes("application/zip") || exportCt.includes("octet-stream"),
      `export content-type unexpected: ${exportCt}`,
    ).toBeTruthy();

    const exportBody = await exportRes.body();

    expect(exportBody.length).toBeGreaterThan(0);

    const submitted = await createApprovalRequest(request, {
      runId,
      manifestVersion,
      sourceEnvironment: "dev",
      targetEnvironment: "test",
      requestComment: "E2E live happy path",
    });

    const approvalRequestId = submitted.approvalRequestId;

    if (!approvalRequestId) {
      throw new Error("Governance submit response missing approvalRequestId");
    }

    liveE2eForensics.approvalRequestId = approvalRequestId;
    test.info().annotations.push({ type: "e2e-approval-request-id", description: approvalRequestId });

    const selfApprovalRes = await postGovernanceApproveRaw(request, approvalRequestId, {
      reviewedBy: liveAuthActorName,
      reviewComment: "should be blocked (same as submitter)",
    });

    expect.soft(selfApprovalRes.ok(), `self-approval should fail, got ${selfApprovalRes.status()}`).toBe(false);
    expect.soft(selfApprovalRes.status()).toBe(400);

    const approved = await approveGovernanceRequest(request, approvalRequestId, {
      reviewedBy: livePeerReviewerActorName,
      reviewComment: "E2E test auto-approve",
    });

    expect(approved.status).toBe("Approved");

    const duplicateApprove = await postGovernanceApproveRaw(request, approvalRequestId, {
      reviewedBy: livePeerReviewerActorName,
      reviewComment: "second approve should fail",
    });

    expect.soft(duplicateApprove.ok(), `duplicate approve should fail, got ${duplicateApprove.status()}`).toBe(false);
    expect.soft(duplicateApprove.status()).toBe(400);

    const auditEvents = await searchAudit(request, { runId, take: "100" });

    for (const ev of auditEvents) {
      expect
        .soft(ev.correlationId != null && ev.correlationId.length > 0, `audit event ${ev.eventType ?? "?"} should have correlationId`)
        .toBe(true);
    }

    const firstCorrelation = auditEvents.find((e) => e.correlationId != null && e.correlationId.length > 0)?.correlationId;

    if (firstCorrelation) {
      liveE2eForensics.auditCorrelationId = firstCorrelation;
      test.info().annotations.push({ type: "e2e-audit-correlation-id", description: firstCorrelation });

      const byCorrelation = await searchAudit(request, { correlationId: firstCorrelation, take: "100" });

      expect.soft(byCorrelation.length, "audit search by correlationId should return at least one row").toBeGreaterThan(0);
    }

    const types = new Set(auditEvents.map((e) => e.eventType).filter(Boolean) as string[]);

    const required = [
      "RunStarted",
      "ManifestGenerated",
      "GovernanceApprovalSubmitted",
      "GovernanceApprovalApproved",
      "RunExported",
    ];
    const missing = required.filter((t) => !types.has(t));

    if (missing.length > 0) {
      throw new Error(
        `Missing audit event types: ${missing.join(", ")}. Found: ${[...types].sort().join(", ")}`,
      );
    }

    await waitForArchitectureRunListCommitted(request, runId, 90_000);

    const runsList = await listArchitectureRuns(request);
    const listed = runsList.find((r) => r.runId === runId);

    expect(listed, `run ${runId} should appear in GET /v1/architecture/runs`).toBeTruthy();
    expect.soft(listed?.status).toMatch(/^committed$/i);

    await page.goto(`/governance?runId=${encodeURIComponent(runId)}`);

    await expect(page.getByRole("heading", { name: /governance workflow/i })).toBeVisible({
      timeout: 60_000,
    });

    await expect(page.locator("#gov-query-run")).toHaveValue(runId, { timeout: 15_000 });

    await page.getByRole("button", { name: /^Load$/i }).click();

    await expect(page.getByText(approvalRequestId).first()).toBeVisible({ timeout: 60_000 });
    await expect(page.getByText("Approved").first()).toBeVisible({ timeout: 60_000 });

    await page.goto("/audit");

    await expect(page.getByRole("heading", { name: /audit log/i })).toBeVisible({ timeout: 30_000 });

    await page.getByLabel(/run id/i).fill(runId);
    await page.getByRole("button", { name: /^Search$/i }).click();

    await expect(page.locator('[role="alert"]').filter({ hasText: /problem|error|failed/i })).toHaveCount(0, {
      timeout: 60_000,
    });
  });
});

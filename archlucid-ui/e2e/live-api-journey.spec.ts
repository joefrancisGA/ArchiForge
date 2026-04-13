/**
 * Requires a running ArchLucid.Api (Sql + DevelopmentBypass by default in CI).
 * Not part of the mock `playwright.config.ts` suite — run:
 *   npx playwright test -c playwright.live.config.ts
 * Set `LIVE_API_URL` if the API is not on http://127.0.0.1:5128.
 */
import { expect, test, type APIRequestContext } from "@playwright/test";

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
  postGovernanceApproveRaw,
  searchAudit,
} from "./helpers/live-api-client";

const peerReviewerActor = "e2e-peer-reviewer";

/** Matches default `ArchLucidAuth:DevUserName` in DevelopmentBypass (submitter for governance requests). */
const developmentBypassActorName = "Developer";

const liveE2eForensics: { runId?: string; approvalRequestId?: string; auditCorrelationId?: string } = {};

async function waitForReadyForCommit(runId: string, request: APIRequestContext, timeoutMs: number): Promise<void> {
  const deadline = Date.now() + timeoutMs;

  while (Date.now() < deadline) {
    const detail = await getRunDetailsWithTransientRetries(request, runId);
    const status = detail.run?.status;

    if (status === 4 || status === "ReadyForCommit") {
      return;
    }

    if (status === 5 || status === "Committed") {
      return;
    }

    if (status === 6 || status === "Failed") {
      throw new Error(`Run ${runId} reached Failed before ReadyForCommit`);
    }

    await new Promise((r) => setTimeout(r, 2000));
  }

  throw new Error(`Run ${runId} did not reach ReadyForCommit within ${timeoutMs}ms`);
}

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

    await waitForReadyForCommit(runId, request, 90_000);

    const commitJson = await commitRun(request, runId);
    const manifestVersion = commitJson.manifest?.metadata?.manifestVersion;

    if (!manifestVersion) {
      throw new Error("Commit response missing manifest.metadata.manifestVersion");
    }

    const afterCommit = await getRunDetailsWithTransientRetries(request, runId);
    const goldenManifestId = afterCommit.run?.goldenManifestId;

    if (!goldenManifestId) {
      throw new Error("Run detail after commit missing run.goldenManifestId");
    }

    await page.goto("/runs");

    await expect(page.getByRole("heading", { name: /runs/i }).first()).toBeVisible({ timeout: 60_000 });

    await page.goto(`/runs/${runId}`);

    await expect(page.getByRole("heading", { name: "Run detail", level: 2 })).toBeVisible({ timeout: 60_000 });

    const manifestLink = page.getByRole("link", { name: goldenManifestId });

    await expect(manifestLink).toBeVisible({ timeout: 60_000 });

    await manifestLink.click();

    await expect(page.getByRole("heading", { name: "Manifest", level: 2 })).toBeVisible({ timeout: 60_000 });
    await expect(page.getByText("Manifest ID:", { exact: false })).toBeVisible();
    await expect(page.getByText(goldenManifestId)).toBeVisible();
    await expect(page.getByRole("heading", { name: "Artifacts", level: 3 })).toBeVisible();
    await expect(page.getByRole("table")).toBeVisible();
    await expect(page.getByRole("columnheader", { name: "Artifact" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Download bundle (ZIP)" })).toBeVisible();

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
      reviewedBy: developmentBypassActorName,
      reviewComment: "should be blocked (same as submitter)",
    });

    expect.soft(selfApprovalRes.ok(), `self-approval should fail, got ${selfApprovalRes.status()}`).toBe(false);
    expect.soft(selfApprovalRes.status()).toBe(400);

    const approved = await approveGovernanceRequest(request, approvalRequestId, {
      reviewedBy: peerReviewerActor,
      reviewComment: "E2E test auto-approve",
    });

    expect(approved.status).toBe("Approved");

    const duplicateApprove = await postGovernanceApproveRaw(request, approvalRequestId, {
      reviewedBy: peerReviewerActor,
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

    const runsList = await listArchitectureRuns(request);
    const listed = runsList.find((r) => r.runId === runId);

    expect(listed, `run ${runId} should appear in GET /v1/architecture/runs`).toBeTruthy();
    expect.soft(listed?.status).toMatch(/committed/i);

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

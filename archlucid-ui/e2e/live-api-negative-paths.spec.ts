/**
 * Negative-path API checks (live ArchLucid.Api + Sql). Run:
 *   npx playwright test -c playwright.live.config.ts
 */
import { expect, test } from "@playwright/test";

import {
  commitRun,
  createApprovalRequest,
  createRun,
  executeRun,
  getRunDetailsRaw,
  liveApiBase,
  liveAuthActorName,
  postArchitectureRequestRaw,
  postGovernanceApproveRaw,
  searchAudit,
  waitForReadyForCommit,
  waitForRunDetailCommitted,
} from "./helpers/live-api-client";

function readProblemType(body: unknown): string {
  if (typeof body !== "object" || body === null) {
    return "";
  }

  const o = body as Record<string, unknown>;
  const t = o.type ?? o.Type;

  return typeof t === "string" ? t : "";
}

test.describe("live-api-negative-paths", () => {
  test.beforeAll(async ({ request }) => {
    const health = await request.get(`${liveApiBase}/health/ready`, { timeout: 60_000 });

    if (!health.ok()) {
      throw new Error(
        `Live API not ready at ${liveApiBase}/health/ready (status ${health.status()}). Start ArchLucid.Api with Sql + DevelopmentBypass.`,
      );
    }
  });

  test("governance self-approval blocked: submit then approve as same actor → 400 + audit", async ({
    request,
  }) => {
    test.setTimeout(180_000);

    const createBody = {
      requestId: `E2E-LIVE-SELF-APPR-${Date.now()}`,
      description: "Live E2E: governance self-approval must return 400 and emit GovernanceSelfApprovalBlocked.",
      systemName: "SelfApprovalTest",
      environment: "prod",
      cloudProvider: 1,
      constraints: [] as string[],
      requiredCapabilities: ["SQL"],
      assumptions: [] as string[],
      priorManifestVersion: null as string | null,
    };

    const { runId } = await createRun(request, createBody);

    await executeRun(request, runId);
    await waitForReadyForCommit(request, runId, 90_000);

    const commitJson = await commitRun(request, runId);
    const manifestVersion = commitJson.manifest?.metadata?.manifestVersion;

    if (!manifestVersion) {
      throw new Error("Commit response missing manifest.metadata.manifestVersion");
    }

    await waitForRunDetailCommitted(request, runId, 60_000);

    const submitted = await createApprovalRequest(request, {
      runId,
      manifestVersion,
      sourceEnvironment: "dev",
      targetEnvironment: "test",
      requestComment: "E2E self-approval negative path",
    });

    const approvalRequestId = submitted.approvalRequestId;

    if (!approvalRequestId) {
      throw new Error("Governance submit response missing approvalRequestId");
    }

    const selfApprove = await postGovernanceApproveRaw(request, approvalRequestId, {
      reviewedBy: liveAuthActorName,
      reviewComment: "same actor as submitter — must fail",
    });

    expect(selfApprove.ok(), `self-approve expected 400, got ${selfApprove.status()}`).toBe(false);
    expect(selfApprove.status()).toBe(400);

    const problemBody: unknown = await selfApprove.json();
    const typeUri = readProblemType(problemBody);

    expect(typeUri, "problem type should reference governance-self-approval").toContain("#governance-self-approval");

    const auditEvents = await searchAudit(request, { runId, take: "200" });
    const types = new Set(auditEvents.map((e) => e.eventType).filter(Boolean) as string[]);

    expect(types.has("GovernanceSelfApprovalBlocked")).toBe(true);
  });

  test("GET run detail for unknown run id returns 404 with run-not-found problem type", async ({ request }) => {
    test.setTimeout(60_000);

    const fakeRunId = crypto.randomUUID();
    const res = await getRunDetailsRaw(request, fakeRunId);

    expect(res.status()).toBe(404);

    const problemBody: unknown = await res.json();
    const typeUri = readProblemType(problemBody);

    expect(typeUri).toContain("#run-not-found");
  });

  test("POST create run with empty JSON object returns 400 or 422", async ({ request }) => {
    test.setTimeout(60_000);

    const res = await postArchitectureRequestRaw(request, {});

    expect(res.ok(), `empty create body should fail, got ${res.status()}`).toBe(false);
    expect([400, 422]).toContain(res.status());
  });
});

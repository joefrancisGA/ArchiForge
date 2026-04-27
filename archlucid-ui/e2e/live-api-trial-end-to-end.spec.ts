/**
 * Merge-blocking **V1 self-serve trial** acceptance: register → audits → scoped UI → trial metering → expiry →
 * billing checkout (Noop) → harness “Stripe-style” activation → converted tenant writes + metrics.
 *
 * Requires **DevelopmentBypass**, **Sql**, **Noop** billing, **Simulator** agents, **Prometheus** scrape enabled,
 * and **`LIVE_E2E_HARNESS_SECRET`** matching **`ArchLucid:E2eHarness:SharedSecret`** on the API (see `docs/runbooks/TRIAL_END_TO_END.md`).
 */
import { expect, test } from "@playwright/test";

import {
  createRun,
  executeRun,
  getTenantTrialStatus,
  liveApiBase,
  liveAuthMode,
  liveE2eHarnessHeaders,
  liveJsonHeaders,
  liveTenantScopeHeaders,
  postArchitectureRequestRaw,
  postHarnessBillingSimulateActivated,
  postHarnessTrialSetExpires,
  searchAudit,
  waitForReadyForCommit,
} from "./helpers/live-api-client";

type Register201 = {
  tenantId: string;
  defaultWorkspaceId: string;
  defaultProjectId: string;
};

function readCounterValue(text: string, metricPrefix: string, labelNeedles: string[]): number {
  let sum = 0;

  for (const line of text.split("\n")) {
    if (!line.startsWith(metricPrefix)) {
      continue;
    }

    if (!labelNeedles.every((n) => line.includes(n))) {
      continue;
    }

    const parts = line.trim().split(/\s+/);
    const v = Number.parseFloat(parts[parts.length - 1] ?? "");

    if (Number.isFinite(v)) {
      sum += v;
    }
  }

  return sum;
}

function readOtelHistogramCount(text: string, baseMetric: string): number {
  const key = `${baseMetric}_count`;

  for (const line of text.split("\n")) {
    if (line.startsWith(key)) {
      const parts = line.trim().split(/\s+/);
      const v = Number.parseFloat(parts[parts.length - 1] ?? "0");

      return Number.isFinite(v) ? v : 0;
    }
  }

  return 0;
}

test.describe("live-api-trial-end-to-end", () => {
  test.beforeAll(async ({ request }) => {
    const health = await request.get(`${liveApiBase}/health/ready`, { timeout: 60_000 });

    if (!health.ok()) {
      throw new Error(`Live API not ready at ${liveApiBase}/health/ready (status ${health.status()}).`);
    }
  });

  test("self-serve trial: register → UI → limits → expiry → checkout → activate → metrics", async ({
    page,
    request,
  }) => {
    test.setTimeout(600_000);
    test.skip(liveAuthMode !== "bypass", "Requires DevelopmentBypass (default ui-e2e-live API).");

    let harnessOk = false;

    try {
      liveE2eHarnessHeaders();
      harnessOk = true;
    } catch {
      harnessOk = false;
    }

    test.skip(!harnessOk, "Set LIVE_E2E_HARNESS_SECRET (>= 16 chars) on the API and Playwright (see TRIAL_END_TO_END.md).");

    const metricsProbe = await request.get(`${liveApiBase}/metrics`, { timeout: 15_000 });

    test.skip(!metricsProbe.ok(), `GET /metrics returned ${metricsProbe.status()} — enable Observability:Prometheus:Enabled for this gate.`);

    const metricsBefore = await metricsProbe.text();
    const signupsBefore = readCounterValue(metricsBefore, "archlucid_trial_signups_total", ['source="self_service"']);
    const conversionsBefore = readCounterValue(metricsBefore, "archlucid_trial_conversion_total", [
      'from_state="Active"',
      'to_tier="Team"',
    ]);
    const checkoutCompletedBefore = readCounterValue(metricsBefore, "archlucid_billing_checkouts_total", [
      'outcome="completed"',
      'provider="Noop"',
    ]);
    const firstRunCountBefore = readOtelHistogramCount(metricsBefore, "archlucid_trial_first_run_seconds");

    const suffix = `${Date.now()}-${Math.random().toString(16).slice(2, 10)}`;
    const adminEmail = `e2e-b8-${suffix}@example.com`;
    const orgName = `E2E B8 Trial Org ${suffix}`;

    const reg = await request.post(`${liveApiBase}/v1/register`, {
      headers: { Accept: "application/json", "Content-Type": "application/json" },
      data: {
        organizationName: orgName,
        adminEmail,
        adminDisplayName: "E2E B8 Admin",
        baselineReviewCycleHours: 18.5,
        baselineReviewCycleSource: "e2e live self-serve trial estimate",
      },
    });

    expect(reg.status(), await reg.text()).toBe(201);

    const provisioned = (await reg.json()) as Register201;

    expect(provisioned.tenantId.length).toBeGreaterThan(0);
    expect(provisioned.defaultWorkspaceId.length).toBeGreaterThan(0);
    expect(provisioned.defaultProjectId.length).toBeGreaterThan(0);

    const scope = {
      tenantId: provisioned.tenantId,
      workspaceId: provisioned.defaultWorkspaceId,
      projectId: provisioned.defaultProjectId,
    };

    const auditDeadline = Date.now() + 120_000;
    let sawSelfReg = false;
    let sawProvisioned = false;

    while (Date.now() < auditDeadline) {
      const selfRows = await searchAudit(request, {
        eventType: "TenantSelfRegistered",
        tenantId: scope.tenantId,
        workspaceId: scope.workspaceId,
        projectId: scope.projectId,
        take: "50",
      });

      const trialRows = await searchAudit(request, {
        eventType: "TrialProvisioned",
        tenantId: scope.tenantId,
        workspaceId: scope.workspaceId,
        projectId: scope.projectId,
        take: "50",
      });

      sawSelfReg = selfRows.some((r) => r.eventType === "TenantSelfRegistered");
      sawProvisioned = trialRows.some((r) => r.eventType === "TrialProvisioned");

      if (sawSelfReg && sawProvisioned) {
        break;
      }

      await new Promise((r) => setTimeout(r, 2000));
    }

    expect(sawSelfReg, "TenantSelfRegistered audit expected after POST /v1/register.").toBe(true);
    expect(sawProvisioned, "TrialProvisioned audit expected after trial bootstrap (email trigger path).").toBe(true);

    const trialJson = await getTenantTrialStatus(request, scope);

    expect(trialJson.status).toBe("Active");
    expect(trialJson.daysRemaining).toBeGreaterThanOrEqual(13);
    expect(trialJson.daysRemaining).toBeLessThanOrEqual(14);
    expect(trialJson.trialRunsUsed).toBe(0);
    expect(trialJson.trialRunsLimit).toBe(10);
    expect(trialJson.trialSeatsUsed).toBe(1);
    expect(trialJson.trialSeatsLimit).toBe(3);
    expect(trialJson.trialSampleRunId).toBeTruthy();
    expect(trialJson.baselineReviewCycleHours).toBe(18.5);
    expect(trialJson.baselineReviewCycleSource).toBe("e2e live self-serve trial estimate");
    expect(trialJson.baselineReviewCycleCapturedUtc).toBeTruthy();

    const commercialPackagingProbe = await request.get(`${liveApiBase}/v1/policy-packs`, {
      headers: { Accept: "application/json", ...liveTenantScopeHeaders(scope) },
    });
    expect(commercialPackagingProbe.status(), await commercialPackagingProbe.text()).toBe(404);
    expect((commercialPackagingProbe.headers()["content-type"] ?? "").toLowerCase()).toContain("application/problem");
    const packagingProblem = (await commercialPackagingProbe.json()) as {
      type?: string;
      status?: number;
    };
    expect(packagingProblem.type ?? "", "tier-hidden route should not disclose packaging; use not-found type").toMatch(
      /resource-not-found/i,
    );

    await page.addInitScript(
      (payload) => {
        window.sessionStorage.setItem("archlucid_last_registration", JSON.stringify(payload));
      },
      {
        tenantId: scope.tenantId,
        defaultWorkspaceId: scope.workspaceId,
        defaultProjectId: scope.projectId,
        adminEmail,
        organizationName: orgName,
      },
    );

    await page.goto("/getting-started?source=registration");
    await expect(page.getByTestId("onboarding-open-sample-run")).toBeVisible({ timeout: 120_000 });

    const sampleHref = (await page.getByTestId("onboarding-open-sample-run").getAttribute("href")) ?? "";

    expect(sampleHref).toMatch(/^\/runs\//);

    await page.getByTestId("onboarding-open-sample-run").click();

    await expect(page.getByRole("heading", { name: "Run detail", level: 2 })).toBeVisible({ timeout: 120_000 });

    await expect(page.getByText(/Loading run detail/i)).toHaveCount(0, { timeout: 120_000 });

    const manifestLink = page.locator("main").locator('a[href^="/manifests/"]').first();

    await expect(manifestLink, "Sample run should link a manifest once summaries hydrate.").toBeVisible({
      timeout: 120_000,
    });

    await manifestLink.click();

    const manifestMain = page.locator("main");

    await expect(manifestMain.getByText(/Fetching manifest summary and artifacts/i)).toHaveCount(0, {
      timeout: 120_000,
    });

    await expect(manifestMain.getByRole("heading", { name: "Manifest", level: 2 })).toBeVisible({ timeout: 120_000 });

    await expect(manifestMain.getByRole("heading", { name: "Artifacts", level: 3 })).toBeVisible({ timeout: 120_000 });

    const sampleRunId = trialJson.trialSampleRunId!.includes("-")
      ? trialJson.trialSampleRunId!
      : `${trialJson.trialSampleRunId!.slice(0, 8)}-${trialJson.trialSampleRunId!.slice(8, 12)}-${trialJson.trialSampleRunId!.slice(12, 16)}-${trialJson.trialSampleRunId!.slice(16, 20)}-${trialJson.trialSampleRunId!.slice(20, 32)}`;

    await page.goto(`/runs/new?sampleRunId=${encodeURIComponent(sampleRunId)}`);

    await expect(page.getByRole("heading", { name: "New request", level: 2 })).toBeVisible({ timeout: 60_000 });

    await page.getByRole("button", { name: "Use defaults" }).click();

    for (let step = 0; step < 5; step += 1) {
      await page.getByRole("button", { name: "Next" }).click();
    }

    const createRespPromise = page.waitForResponse(
      (r) =>
        r.url().includes("/api/proxy/v1/architecture/request") && r.request().method() === "POST",
      { timeout: 120_000 },
    );

    await page.getByRole("button", { name: "Create request" }).click();

    const createResp = await createRespPromise;
    const createJson = (await createResp.json()) as { run?: { runId?: string } };
    const wizardRunId = createJson.run?.runId ?? "";

    expect(wizardRunId.length).toBeGreaterThan(0);
    expect(createResp.status()).toBe(201);

    await expect(page.getByText(/Run .* created/i)).toBeVisible({ timeout: 120_000 });

    await executeRun(request, wizardRunId, scope);
    await waitForReadyForCommit(request, wizardRunId, 120_000, scope);

    await page.goto(`/runs/${wizardRunId}`);

    await expect(page.getByRole("heading", { name: "Run detail", level: 2 })).toBeVisible({ timeout: 120_000 });

    await expect(page.getByText(/Loading run detail/i)).toHaveCount(0, { timeout: 120_000 });

    await page.getByRole("button", { name: "Finalize manifest" }).first().click();
    await page.getByRole("alertdialog").getByRole("button", { name: "Finalize manifest" }).click();

    await expect(page.getByText(/This run is already finalized/i)).toBeVisible({ timeout: 120_000 });

    await expect(page.getByTestId("email-run-to-sponsor-first-commit-badge")).toBeVisible({ timeout: 120_000 });
    await expect(page.getByText(/Day \d+ since first finalization/i)).toBeVisible({ timeout: 10_000 });

    const afterFirstCommit = await getTenantTrialStatus(request, scope);

    expect(afterFirstCommit.trialRunsUsed).toBe(1);
    expect(afterFirstCommit.firstCommitUtc, "first commit should anchor sponsor day badge (trial-status)").toBeTruthy();

    const burnBody = {
      requestId: `B8-BURN-${suffix}`,
      description:
        "Burn trial run allowance for E2E. Design a minimal secure API with private endpoints and managed identity.",
      systemName: "BurnRun",
      environment: "prod",
      cloudProvider: 1,
      constraints: ["Private networking"],
      requiredCapabilities: ["Managed Identity"],
      assumptions: [] as string[],
      priorManifestVersion: null as string | null,
    };

    for (let i = 0; i < 9; i += 1) {
      await createRun(
        request,
        {
          ...burnBody,
          requestId: `B8-BURN-${suffix}-${i}`,
        },
        scope,
      );
    }

    const atLimit = await getTenantTrialStatus(request, scope);

    expect(atLimit.trialRunsUsed).toBe(10);

    const blocked = await postArchitectureRequestRaw(
      request,
      {
        ...burnBody,
        requestId: `B8-BLOCK-RUNS-${suffix}`,
      },
      scope,
    );

    expect(blocked.status(), await blocked.text()).toBe(402);
    expect(blocked.headers()["content-type"] ?? "").toContain("application/problem+json");

    const problemRuns = (await blocked.json()) as { type?: string; extensions?: { trialReason?: string } };

    expect(problemRuns.type).toBe("https://archlucid.dev/problem/trial-expired");

    const warpRes = await postHarnessTrialSetExpires(
      request,
      scope.tenantId,
      new Date(Date.now() - 5000).toISOString(),
    );

    expect(warpRes.status(), await warpRes.text()).toBe(204);

    const expiredRead = await getTenantTrialStatus(request, scope);

    expect(expiredRead.status).toBe("Active");
    expect(expiredRead.daysRemaining).toBe(0);

    const blockedExpired = await postArchitectureRequestRaw(
      request,
      {
        ...burnBody,
        requestId: `B8-BLOCK-EXP-${suffix}`,
      },
      scope,
    );

    expect(blockedExpired.status()).toBe(402);

    const checkout = await request.post(`${liveApiBase}/v1/tenant/billing/checkout`, {
      headers: { ...liveJsonHeaders(), ...liveTenantScopeHeaders(scope) },
      data: {
        targetTier: "Team",
        returnUrl: "https://example.com/billing/return",
        cancelUrl: "https://example.com/billing/cancel",
      },
    });

    expect(checkout.status(), await checkout.text()).toBe(200);

    const checkoutJson = (await checkout.json()) as { checkoutUrl?: string; providerSessionId?: string };

    expect(checkoutJson.checkoutUrl ?? "").toContain("https://billing.archlucid.local/noop-checkout");
    expect(checkoutJson.providerSessionId ?? "").toMatch(/^noop_sess_/);

    const sim = await postHarnessBillingSimulateActivated(request, {
      tenantId: scope.tenantId,
      workspaceId: scope.workspaceId,
      projectId: scope.projectId,
      providerSubscriptionId: checkoutJson.providerSessionId,
      checkoutTier: "Team",
      provider: "Noop",
    });

    expect(sim.status(), await sim.text()).toBe(204);

    const convRows = await searchAudit(request, {
      eventType: "TenantTrialConverted",
      tenantId: scope.tenantId,
      workspaceId: scope.workspaceId,
      projectId: scope.projectId,
      take: "50",
    });

    expect(convRows.some((r) => r.eventType === "TenantTrialConverted")).toBe(true);

    const unblock = await postArchitectureRequestRaw(
      request,
      {
        ...burnBody,
        requestId: `B8-POST-CONVERT-${suffix}`,
      },
      scope,
    );

    expect([200, 201].includes(unblock.status()), await unblock.text()).toBe(true);

    await page.goto("/runs");

    await expect(page.getByRole("region", { name: "Trial subscription" })).toHaveCount(0, { timeout: 60_000 });

    const metricsAfterRes = await request.get(`${liveApiBase}/metrics`, { timeout: 30_000 });

    expect(metricsAfterRes.ok()).toBeTruthy();

    const metricsAfter = await metricsAfterRes.text();
    const signupsAfter = readCounterValue(metricsAfter, "archlucid_trial_signups_total", ['source="self_service"']);
    const conversionsAfter = readCounterValue(metricsAfter, "archlucid_trial_conversion_total", [
      'from_state="Active"',
      'to_tier="Team"',
    ]);
    const checkoutCompletedAfter = readCounterValue(metricsAfter, "archlucid_billing_checkouts_total", [
      'outcome="completed"',
      'provider="Noop"',
    ]);
    const firstRunCountAfter = readOtelHistogramCount(metricsAfter, "archlucid_trial_first_run_seconds");

    expect(signupsAfter - signupsBefore).toBeGreaterThanOrEqual(1);
    expect(conversionsAfter - conversionsBefore).toBeGreaterThanOrEqual(1);
    expect(checkoutCompletedAfter - checkoutCompletedBefore).toBeGreaterThanOrEqual(1);
    expect(firstRunCountAfter - firstRunCountBefore).toBeGreaterThanOrEqual(1);
  });
});

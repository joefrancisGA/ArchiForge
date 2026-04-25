/**
 * Live **self-service registration** (`POST /v1/register`) with optional **trial auth matrix** tags.
 *
 * Matrix tags (comma-separated `LIVE_TRIAL_E2E_MODES`, default `register-baseline`):
 * - `register-baseline` — anonymous org registration returns **201** (no local identity preconditions).
 * - `local-identity` — requires API with `Auth:Trial:Modes` including **LocalIdentity** and env
 *   `LIVE_TRIAL_LOCAL_PASSWORD`; performs `/v1/auth/trial/local/register` then `/v1/register` with matching email.
 *
 * **MsaExternalId** live checks are intentionally not automated here (long-lived CIAM secrets); use manual smoke
 * against a configured External ID tenant when that mode is enabled.
 */
import { expect, test } from "@playwright/test";

import {
  commitRun,
  createRun,
  executeRun,
  liveApiBase,
  liveAuthMode,
  liveJsonHeaders,
  liveTenantScopeHeaders,
  searchAudit,
  waitForReadyForCommit,
  waitForRunDetailCommitted,
} from "./helpers/live-api-client";

const modes = (process.env.LIVE_TRIAL_E2E_MODES ?? "register-baseline")
  .split(",")
  .map((s) => s.trim().toLowerCase())
  .filter(Boolean);

function modeEnabled(tag: string): boolean {
  return modes.includes(tag);
}

/** Base metric names (histograms appear as `_bucket` / `_sum` lines in Prometheus text). */
function expectPrometheusTextContainsTrialFunnelMetrics(text: string): void {
  const needles = [
    "archlucid_trial_signups_total",
    "archlucid_trial_signup_failures_total",
    "archlucid_trial_first_run_seconds",
    "archlucid_trial_active_tenants",
    "archlucid_trial_runs_used_ratio",
    "archlucid_trial_conversion_total",
    "archlucid_billing_checkouts_total",
  ];

  for (const m of needles) {
    expect(text, `Prometheus scrape should mention ${m}`).toContain(m);
  }

  // archlucid_trial_expirations_total is emitted by TrialLifecycleTransitionEngine (Worker); see docs/runbooks/TRIAL_FUNNEL.md.
}

test.describe("live-api-trial-signup", () => {
  test.beforeAll(async ({ request }) => {
    const health = await request.get(`${liveApiBase}/health/ready`, { timeout: 60_000 });

    if (!health.ok()) {
      throw new Error(`Live API not ready at ${liveApiBase}/health/ready (status ${health.status()}).`);
    }
  });

  test("register-baseline: POST /v1/register provisions org (201)", async ({ request }) => {
    test.skip(!modeEnabled("register-baseline"), 'Set LIVE_TRIAL_E2E_MODES to include "register-baseline".');

    const suffix = `${Date.now()}-${Math.random().toString(16).slice(2, 8)}`;
    const res = await request.post(`${liveApiBase}/v1/register`, {
      headers: { Accept: "application/json", "Content-Type": "application/json" },
      data: {
        organizationName: `Live Trial Org ${suffix}`,
        adminEmail: `trial-admin-${suffix}@example.com`,
        adminDisplayName: "Live Trial Admin",
      },
    });

    expect(res.status(), await res.text()).toBe(201);
    const body = (await res.json()) as { tenantId?: string };

    expect(body.tenantId).toBeTruthy();
  });

  test('local-identity: register flow when API exposes trial local routes', async ({ request }) => {
    test.skip(!modeEnabled("local-identity"), 'Set LIVE_TRIAL_E2E_MODES to include "local-identity".');

    const password = process.env.LIVE_TRIAL_LOCAL_PASSWORD?.trim() ?? "";
    test.skip(password.length < 8, "Set LIVE_TRIAL_LOCAL_PASSWORD (>= 8 chars) for local-identity matrix row.");

    const suffix = `${Date.now()}-${Math.random().toString(16).slice(2, 8)}`;
    const email = `trial-local-${suffix}@example.com`;

    const localReg = await request.post(`${liveApiBase}/v1/auth/trial/local/register`, {
      headers: { Accept: "application/json", "Content-Type": "application/json" },
      data: { email, password },
    });

    if (localReg.status() === 404) {
      test.skip(true, "Local trial identity is disabled on this API (Auth:Trial:Modes).");
    }

    expect(localReg.status(), await localReg.text()).toBe(201);

    const orgRes = await request.post(`${liveApiBase}/v1/register`, {
      headers: { Accept: "application/json", "Content-Type": "application/json" },
      data: {
        organizationName: `Live Trial Local Org ${suffix}`,
        adminEmail: email,
        adminDisplayName: "Live Trial Local Admin",
      },
    });

    expect(orgRes.status(), await orgRes.text()).toBe(201);
  });

  test("ui: signup → verify → onboarding → sample run → manifest (DevelopmentBypass)", async ({ page, request }) => {
    test.setTimeout(240_000);
    test.skip(liveAuthMode !== "bypass", "Requires DevelopmentBypass auth (default ui-e2e-live API).");

    const suffix = `${Date.now()}-${Math.random().toString(16).slice(2, 8)}`;
    const adminEmail = `trial-ui-${suffix}@example.com`;
    const orgName = `Trial UI Org ${suffix}`;

    await page.goto("/signup");
    await page.getByLabel(/^Work email$/i).fill(adminEmail);
    await page.getByLabel(/^Full name$/i).fill("Trial UI User");
    await page.getByLabel(/^Organization name$/i).fill(orgName);
    await page.getByRole("button", { name: /Create trial workspace/i }).click();

    await page.waitForURL(/\/signup\/verify/i, { timeout: 60_000 });

    const rawSession = await page.evaluate(() => window.sessionStorage.getItem("archlucid_last_registration") ?? "");
    const parsed = JSON.parse(rawSession) as {
      tenantId: string;
      defaultWorkspaceId: string;
      defaultProjectId: string;
    };

    expect(parsed.tenantId.length).toBeGreaterThan(0);

    await page.setExtraHTTPHeaders({
      "x-tenant-id": parsed.tenantId,
      "x-workspace-id": parsed.defaultWorkspaceId,
      "x-project-id": parsed.defaultProjectId,
    });

    await page.getByTestId("signup-verify-continue-onboarding").click();
    await page.waitForURL(/\/getting-started/i, { timeout: 60_000 });

    const sampleLink = page.getByTestId("onboarding-open-sample-run");
    await expect(sampleLink).toBeVisible({ timeout: 120_000 });

    const sampleHref = (await sampleLink.getAttribute("href")) ?? "";
    expect(sampleHref).toMatch(/^\/runs\//);

    const deadline = Date.now() + 120_000;
    let sawTrialProvisioned = false;

    while (Date.now() < deadline) {
      const rows = await searchAudit(request, {
        eventType: "TrialProvisioned",
        tenantId: parsed.tenantId,
        workspaceId: parsed.defaultWorkspaceId,
        projectId: parsed.defaultProjectId,
        take: "50",
      });

      if (rows.some((r) => r.eventType === "TrialProvisioned")) {
        sawTrialProvisioned = true;
        break;
      }

      await new Promise((r) => setTimeout(r, 2000));
    }

    expect(sawTrialProvisioned, "TrialProvisioned audit should appear after demo seed completes.").toBe(true);

    const selfReg = await searchAudit(request, {
      eventType: "TenantSelfRegistered",
      tenantId: parsed.tenantId,
      workspaceId: parsed.defaultWorkspaceId,
      projectId: parsed.defaultProjectId,
      take: "50",
    });

    expect(selfReg.some((e) => e.eventType === "TenantSelfRegistered")).toBe(true);

    await sampleLink.click();

    await expect(page.getByRole("heading", { name: "Run detail", level: 2 })).toBeVisible({ timeout: 120_000 });

    await expect(page.getByText(/Loading run detail/i)).toHaveCount(0, { timeout: 120_000 });

    const manifestLink = page.locator("main").locator('a[href^="/manifests/"]').first();

    await expect(
      manifestLink,
      "Seeded sample run should expose a golden manifest link once summaries hydrate.",
    ).toBeVisible({ timeout: 120_000 });

    await manifestLink.click();

    const manifestMain = page.locator("main");

    await expect(manifestMain.getByText(/Fetching manifest summary and artifacts/i)).toHaveCount(0, {
      timeout: 120_000,
    });

    await expect(manifestMain.getByRole("heading", { name: "Manifest", level: 2 })).toBeVisible({ timeout: 120_000 });

    const metricsRes = await request.get(`${liveApiBase}/metrics`, { timeout: 30_000 });

    if (!metricsRes.ok()) {
      test.skip(true, `GET /metrics returned ${metricsRes.status()} (enable Observability:Prometheus:Enabled for trial funnel checks).`);
    }

    const metricsText = await metricsRes.text();

    expect(metricsText).toContain("archlucid_trial_signups_total");
  });

  test("trial funnel: /metrics lists trial instruments after register + coordinator + billing + convert", async ({
    request,
  }) => {
    test.setTimeout(300_000);
    test.skip(liveAuthMode !== "bypass", "Requires DevelopmentBypass (AdminAuthority) for tenant billing and convert.");

    const suffix = `${Date.now()}-${Math.random().toString(16).slice(2, 8)}`;
    const orgName = `Metrics Funnel Org ${suffix}`;
    const adminEmail = `metrics-funnel-${suffix}@example.com`;

    const first = await request.post(`${liveApiBase}/v1/register`, {
      headers: { Accept: "application/json", "Content-Type": "application/json" },
      data: {
        organizationName: orgName,
        adminEmail,
        adminDisplayName: "Metrics Funnel Admin",
      },
    });

    expect(first.status(), await first.text()).toBe(201);

    const provisioned = (await first.json()) as {
      tenantId?: string;
      defaultWorkspaceId?: string;
      defaultProjectId?: string;
    };

    expect(provisioned.tenantId).toBeTruthy();
    expect(provisioned.defaultWorkspaceId).toBeTruthy();
    expect(provisioned.defaultProjectId).toBeTruthy();

    const dup = await request.post(`${liveApiBase}/v1/register`, {
      headers: { Accept: "application/json", "Content-Type": "application/json" },
      data: {
        organizationName: orgName,
        adminEmail: `other-${suffix}@example.com`,
        adminDisplayName: "Other Admin",
      },
    });

    expect(dup.status(), await dup.text()).toBe(409);

    const scope = {
      tenantId: provisioned.tenantId!,
      workspaceId: provisioned.defaultWorkspaceId!,
      projectId: provisioned.defaultProjectId!,
    };

    const createBody = {
      requestId: `TRIAL-FUNNEL-${suffix}`,
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

    const { runId } = await createRun(request, createBody, scope);

    await executeRun(request, runId, scope);
    await waitForReadyForCommit(request, runId, 120_000, scope);
    await commitRun(request, runId, scope);
    await waitForRunDetailCommitted(request, runId, 90_000, scope);

    const checkout = await request.post(`${liveApiBase}/v1/tenant/billing/checkout`, {
      headers: { ...liveJsonHeaders(), ...liveTenantScopeHeaders(scope) },
      data: {
        targetTier: "Team",
        returnUrl: "https://example.com/billing/return",
        cancelUrl: "https://example.com/billing/cancel",
      },
    });

    expect(checkout.status(), await checkout.text()).toBe(200);

    const convert = await request.post(`${liveApiBase}/v1/tenant/convert`, {
      headers: { ...liveJsonHeaders(), ...liveTenantScopeHeaders(scope) },
      data: { targetTier: "Team" },
    });

    expect(convert.status(), await convert.text()).toBe(204);

    const metricsRes = await request.get(`${liveApiBase}/metrics`, { timeout: 30_000 });

    if (!metricsRes.ok()) {
      test.skip(true, `GET /metrics returned ${metricsRes.status()} (enable Observability:Prometheus:Enabled).`);
    }

    const metricsText = await metricsRes.text();

    expectPrometheusTextContainsTrialFunnelMetrics(metricsText);
  });
});

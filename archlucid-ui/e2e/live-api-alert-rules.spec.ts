/**
 * Live API + SQL: alert rule create + list (ExecuteAuthority via DevelopmentBypass admin).
 */
import { expect, test } from "@playwright/test";

import { runAxe } from "./helpers/axe-helper";

import {
  commitRun,
  createRun,
  executeRun,
  getAlertRulesRaw,
  liveApiBase,
  postAlertRuleRaw,
  waitForReadyForCommit,
  waitForRunDetailCommitted,
} from "./helpers/live-api-client";

test.describe("live-api-alert-rules", () => {
  test.beforeAll(async ({ request }) => {
    const health = await request.get(`${liveApiBase}/health/ready`, { timeout: 60_000 });

    if (!health.ok()) {
      throw new Error(`Live API not ready at ${liveApiBase}/health/ready (status ${health.status()}).`);
    }
  });

  test("create alert rule then list includes it; alerts page renders", async ({ request, page }) => {
    test.setTimeout(180_000);

    const createBody = {
      requestId: `E2E-ALERT-${Date.now()}`,
      description: "Seed run for alert rule E2E.",
      systemName: "E2EAlertRules",
      environment: "prod",
      cloudProvider: 1,
      constraints: [] as string[],
      requiredCapabilities: [] as string[],
      assumptions: [] as string[],
      priorManifestVersion: null as string | null,
    };

    const { runId } = await createRun(request, createBody);
    await executeRun(request, runId);
    await waitForReadyForCommit(request, runId, 90_000);
    await commitRun(request, runId);
    await waitForRunDetailCommitted(request, runId, 60_000);

    const ruleName = `e2e-metric-${Date.now()}`;
    const createRuleRes = await postAlertRuleRaw(request, {
      name: ruleName,
      ruleType: "CriticalRecommendationCount",
      severity: "Warning",
      thresholdValue: 1,
      isEnabled: true,
      targetChannelType: "DigestOnly",
      metadataJson: "{}",
    });

    expect(createRuleRes.ok(), `POST alert-rules expected 200, got ${createRuleRes.status()}`).toBeTruthy();
    const created = (await createRuleRes.json()) as { ruleId?: string; name?: string };

    expect(created.ruleId).toBeTruthy();
    expect(created.name).toBe(ruleName);

    const listRes = await getAlertRulesRaw(request);

    expect(listRes.ok()).toBeTruthy();
    const list = (await listRes.json()) as Array<{ name?: string; ruleId?: string }>;
    const found = list.some((r) => r.ruleId === created.ruleId || r.name === ruleName);

    expect(found).toBeTruthy();

    await page.goto("/alerts?projectId=default", { waitUntil: "load" });
    await page.locator("main").first().waitFor({ state: "visible", timeout: 60_000 });
    const axe = await runAxe(page);
    const critical = axe.violations.filter((v) => v.impact === "critical" || v.impact === "serious");
    expect(critical, "alerts page axe").toHaveLength(0);
  });
});

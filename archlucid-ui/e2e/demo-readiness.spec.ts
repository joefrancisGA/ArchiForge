import { expect, test } from "@playwright/test";

import {
  SCREENSHOT_RUN_ID,
  SHOWCASE_DEMO_RUN_ID,
  SHOWCASE_STATIC_DEMO_MANIFEST_ID,
} from "./fixtures";

const claimsShowcasePath = "/showcase/claims-intake-modernization";

/**
 * Validates the mock-backed “proof chain”: runs list → run detail → manifest detail.
 * Run in isolation: `npx playwright test -c playwright.mock.config.ts e2e/demo-readiness.spec.ts`
 * or `npx playwright test --grep @demo-readiness`.
 */
test.describe.parallel("demo-readiness — mock proof chain @demo-readiness", () => {
  test("policy pack rejects literal undefined token route @demo-readiness", async ({ page }) => {
    await page.goto("/governance/policy-packs/undefined");
    await expect(page.getByTestId("branded-not-found")).toBeVisible();
  });

  test("runs list shows Claims Intake example without mock-provider leakage", async ({ page }) => {
    await page.goto("/runs?projectId=default");
    await expect(page.getByRole("heading", { name: /architecture runs/i })).toBeVisible();
    await expect(page.getByText(/Claims Intake Modernization/i).first()).toBeVisible();
    await expect(page.getByText(/mock API/i)).toHaveCount(0);
  });

  test("run detail avoids not-found shells, bogus pipeline progress, and invalid dates", async ({ page }) => {
    await page.goto(`/runs/${encodeURIComponent(SHOWCASE_DEMO_RUN_ID)}`);
    const primaryMain = page.getByRole("main").first();
    await expect(primaryMain).not.toContainText(/run not found/i);
    await expect(primaryMain).not.toContainText(/request failed/i);
    await expect(primaryMain).not.toContainText(/Invalid Date/i);
    await expect(primaryMain.getByText(/\b0 of 4 run pipeline stages complete\b/i)).toHaveCount(0);

    await page.goto(`/runs/${encodeURIComponent(SCREENSHOT_RUN_ID)}`);
    await expect(page.getByRole("main").first()).not.toContainText(/run not found/i);
  });

  test("showcase-aligned manifest UUID loads manifest chrome (not indefinite skeleton)", async ({ page }) => {
    await page.goto(`/manifests/${encodeURIComponent(SHOWCASE_STATIC_DEMO_MANIFEST_ID)}`);
    await expect(page.getByRole("heading", { name: /Finalized architecture manifest/i })).toBeVisible();
    const primaryMain = page.getByRole("main");
    await expect(primaryMain).toHaveCount(1);
    await expect(primaryMain).not.toContainText(/manifest summary could not be loaded/i);
    await expect(primaryMain).not.toContainText(/request failed/i);
  });

  test("marketing showcase exposes working deep links into operator proof pages", async ({ page }) => {
    await page.goto(claimsShowcasePath);
    await expect(page.getByRole("heading", { level: 1 }).first()).toContainText(
      /completed architecture output|Completed example/i,
    );

    await page.getByRole("link", { name: /Run detail/i }).first().click();
    await expect(page).toHaveURL(new RegExp(`/runs/${SHOWCASE_DEMO_RUN_ID.replace(/-/g, "\\-")}`));
    await expect(page.getByRole("main").first()).not.toContainText(/Invalid Date/i);

    await page.goto(claimsShowcasePath);
    await page.getByRole("link", { name: /Finalized manifest/i }).first().click();
    await expect(page).toHaveURL(
      new RegExp(`/manifests/${SHOWCASE_STATIC_DEMO_MANIFEST_ID.replace(/-/g, "\\-")}`),
    );
    await expect(page.getByRole("heading", { name: /Finalized architecture manifest/i })).toBeVisible();
  });

  test("demo pages do not leak internal tokens in main content @demo-readiness", async ({ page }) => {
    const paths: string[] = [
      "/",
      "/runs?projectId=default",
      `/runs/${encodeURIComponent(SHOWCASE_DEMO_RUN_ID)}`,
      `/manifests/${encodeURIComponent(SHOWCASE_STATIC_DEMO_MANIFEST_ID)}`,
      "/governance",
      "/help",
      `/runs/${encodeURIComponent(SHOWCASE_DEMO_RUN_ID)}/findings/${encodeURIComponent("phi-minimization-risk")}`,
    ];

    const banned = [
      /undefined/,
      /\bfixture\b/i,
      /\bmock\b/i,
      /localhost/i,
      /Invalid Date/i,
      /\boperator access\b/i,
      /\bAPI-gated\b/i,
    ];

    for (const path of paths) {
      await page.goto(path);
      const mainText = await page.getByRole("main").first().innerText();

      for (const pattern of banned) {
        expect(mainText, `Unexpected token on ${path}`).not.toMatch(pattern);
      }
    }
  });

  test("core demo smoke — home, new request, runs, run detail, manifest, finding, showcase @demo-readiness", async ({
    page,
  }) => {
    await page.goto("/");
    await expect(page.locator('a[href^="/runs/new"]').first()).toBeVisible();

    await page.goto("/runs/new");
    await expect(page).toHaveURL(/\/runs\/new/);

    await page.goto("/runs?projectId=default");
    await expect(page.getByRole("heading", { name: /architecture runs/i })).toBeVisible();
    await page.getByRole("link", { name: /Claims Intake Modernization/i }).first().click();
    await expect(page).toHaveURL(new RegExp(`/runs/${SHOWCASE_DEMO_RUN_ID.replace(/-/g, "\\-")}`));
    await expect(page.getByRole("main").first()).not.toContainText(/request failed/i);

    await page.goto(`/manifests/${encodeURIComponent(SHOWCASE_STATIC_DEMO_MANIFEST_ID)}`);
    await expect(page.getByRole("heading", { name: /Finalized architecture manifest/i })).toBeVisible();

    await page.goto(
      `/runs/${encodeURIComponent(SHOWCASE_DEMO_RUN_ID)}/findings/${encodeURIComponent("phi-minimization-risk")}`,
    );
    await expect(page.getByRole("main").first()).not.toContainText(/request failed/i);

    await page.goto("/showcase/claims-intake-modernization");
    await expect(page.getByRole("heading", { level: 1 }).first()).toBeVisible();
    await expect(page.getByRole("main").first()).not.toContainText(/request failed/i);

    await page.goto("/ask");
    await expect(page.getByRole("main").first()).not.toContainText(/request failed/i);

    await page.goto("/help");
    await expect(page.getByRole("main").first()).not.toContainText(/request failed/i);
  });

  test("advanced-route smoke — ask graph compare governance advisory replay search policy packs load @demo-readiness", async ({
    page,
  }) => {
    const routes = [
      "/ask",
      "/graph",
      "/compare",
      "/governance",
      "/advisory-scheduling",
      "/replay",
      "/search",
      "/policy-packs",
    ] as const;

    for (const path of routes) {
      await page.goto(path);
      await expect(page.getByRole("main").first()).not.toContainText(/request failed/i);
    }
  });

  test("policy pack scoped route renders pack shell (not governance workflow page heading) @demo-readiness", async ({
    page,
  }) => {
    await page.goto("/governance/policy-packs/e2e-policy-pack-001");
    await expect(page.getByText("Policy pack detail")).toBeVisible();
    await expect(page.getByRole("heading", { level: 1, name: /^Governance workflow$/i })).toHaveCount(0);
  });

  test("invalid manifest and run route tokens surface branded not-found @demo-readiness", async ({ page }) => {
    await page.goto("/manifests/undefined");
    await expect(page.getByTestId("branded-not-found")).toBeVisible();

    await page.goto("/runs/undefined");
    await expect(page.getByTestId("branded-not-found")).toBeVisible();
  });
});

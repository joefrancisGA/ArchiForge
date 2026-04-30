import { expect, test } from "@playwright/test";

import { SHOWCASE_DEMO_RUN_ID, SCREENSHOT_FINDING_ID, SHOWCASE_STATIC_DEMO_MANIFEST_ID } from "./fixtures";
test.describe("operator shell smoke", () => {
  test("home renders shell headings", async ({ page }) => {
    await page.goto("/");

    await expect(page.getByRole("heading", { name: "ArchLucid", level: 1 })).toBeVisible();
    await expect(page.getByText("Create Request")).toBeVisible();
  });

  test("runs list with default project shows a run row without generic error boundary @smoke", async ({ page }) => {
    await page.goto("/runs?projectId=default");

    await expect(page.getByRole("heading", { name: /^Architecture runs$/i })).toBeVisible();
    await expect(page.getByRole("main").getByText(/Something went wrong/i)).toHaveCount(0);
    await expect(page.locator('[data-testid^="runs-row-"]').first()).toBeVisible();
  });

  test("runs list renders without generic error boundary", async ({ page }) => {
    await page.goto("/runs");

    await expect(page.getByRole("heading", { name: /^Architecture runs$/i })).toBeVisible();
    await expect(page.getByRole("main").getByText(/Something went wrong/i)).toHaveCount(0);
  });

  test("Ask page renders without generic error boundary", async ({ page }) => {
    await page.goto("/ask");

    await expect(page.getByRole("heading", { name: /^Ask ArchLucid$/i })).toBeVisible();
    await expect(page.getByRole("main").getByText(/Something went wrong/i)).toHaveCount(0);
  });

  test("Help page renders primary heading", async ({ page }) => {
    await page.goto("/help");

    await expect(page.getByRole("heading", { name: /^Help$/i, level: 1 })).toBeVisible();
    await expect(page.getByRole("main").getByText(/Something went wrong/i)).toHaveCount(0);
  });

  test("new request page renders without generic error boundary", async ({ page }) => {
    await page.goto("/runs/new");

    await expect(page.getByRole("heading", { name: /new architecture request/i })).toBeVisible();
    await expect(page.getByRole("main").getByText(/Something went wrong/i)).toHaveCount(0);
  });
});

test.describe("operator shell smoke — core proof path", () => {
  test("home through help without generic error boundary @smoke-core-path", async ({ page }) => {
    await page.goto("/");
    await expect(page.getByRole("main").getByText(/Something went wrong/i)).toHaveCount(0);

    await page.goto("/runs?projectId=default");
    await expect(page.getByRole("heading", { name: /^Architecture runs$/i })).toBeVisible();
    await expect(page.getByRole("main").getByText(/Something went wrong/i)).toHaveCount(0);

    await page.goto(`/runs/${encodeURIComponent(SHOWCASE_DEMO_RUN_ID)}`);
    await expect(page.getByRole("main").first()).not.toContainText(/Something went wrong/i);

    await page.goto(`/manifests/${encodeURIComponent(SHOWCASE_STATIC_DEMO_MANIFEST_ID)}`);
    await expect(page.getByRole("heading", { name: /Finalized architecture manifest/i })).toBeVisible();

    await page.goto(
      `/runs/${encodeURIComponent(SHOWCASE_DEMO_RUN_ID)}/findings/${encodeURIComponent(SCREENSHOT_FINDING_ID)}`,
    );
    await expect(page.getByRole("main").first()).not.toContainText(/Something went wrong/i);

    await page.goto("/showcase/claims-intake-modernization");
    await expect(page.getByRole("heading", { level: 1 }).first()).toBeVisible();

    await page.goto("/ask");
    await expect(page.getByRole("heading", { name: /^Ask ArchLucid$/i })).toBeVisible();
    await expect(page.getByRole("main").getByText(/Something went wrong/i)).toHaveCount(0);

    await page.goto("/help");
    await expect(page.getByRole("heading", { name: /^Help$/i, level: 1 })).toBeVisible();
    await expect(page.getByRole("main").getByText(/Something went wrong/i)).toHaveCount(0);
  });
});

test.describe("operator shell smoke — advanced surface path", () => {
  test("analysis and controls routes render primary headings @smoke-advanced-path", async ({ page }) => {
    await page.goto("/ask");
    await expect(page.getByRole("heading", { name: /^Ask ArchLucid$/i })).toBeVisible();
    await expect(page.getByRole("main").first().getByText(/Something went wrong/i)).toHaveCount(0);

    await page.goto("/graph");
    await expect(page.getByRole("heading", { name: /Architecture graph/i })).toBeVisible();
    await expect(page.getByRole("main").first().getByText(/Something went wrong/i)).toHaveCount(0);

    await page.goto("/compare");
    await expect(page.getByRole("heading", { name: /Compare runs/i })).toBeVisible();
    await expect(page.getByRole("main").first().getByText(/Something went wrong/i)).toHaveCount(0);

    await page.goto("/governance");
    await expect(page.getByRole("heading", { name: /Governance workflow/i })).toBeVisible();
    await expect(page.getByRole("main").first().getByText(/Something went wrong/i)).toHaveCount(0);

    await page.goto("/advisory");
    await expect(page.getByTestId("advisory-hub")).toBeVisible();
    await expect(page.getByRole("main").first().getByText(/Something went wrong/i)).toHaveCount(0);

    await page.goto("/replay");
    await expect(page.getByRole("heading", { name: /^Replay$/i })).toBeVisible();
    await expect(page.getByRole("main").first().getByText(/Something went wrong/i)).toHaveCount(0);

    await page.goto("/search");
    await expect(page.getByRole("heading", { name: /Semantic Search/i })).toBeVisible();
    await expect(page.getByRole("main").first().getByText(/Something went wrong/i)).toHaveCount(0);

    await page.goto("/policy-packs");
    await expect(page.getByRole("heading", { name: /Policy packs/i })).toBeVisible();
    await expect(page.getByRole("main").first().getByText(/Something went wrong/i)).toHaveCount(0);
  });
});

import { expect, test } from "@playwright/test";

import {
  FIXTURE_MANIFEST_ID,
  FIXTURE_PROJECT_ID,
  FIXTURE_RUN_ID,
} from "./fixtures";
import { gotoRunDetailForMockFixtureRun } from "./helpers/operator-journey";

test.describe("operator journey — run detail to manifest and back", () => {
  test("reviews fixture run, opens manifest, returns to run (mock API only)", async ({ page }) => {
    await gotoRunDetailForMockFixtureRun(page);

    await expect(page.getByRole("heading", { name: "Run detail", level: 2 })).toBeVisible();

    const runSummarySection = page
      .locator("section")
      .filter({ has: page.getByRole("heading", { name: "Run", level: 3 }) });
    await expect(runSummarySection.getByText("Run ID:", { exact: true })).toBeVisible();
    await expect(runSummarySection.getByText(FIXTURE_RUN_ID)).toBeVisible();
    await expect(runSummarySection.getByText("Project:", { exact: true })).toBeVisible();
    await expect(runSummarySection.getByText(FIXTURE_PROJECT_ID)).toBeVisible();
    await expect(page.getByText(/E2E fixture run \(no live API\)/)).toBeVisible();

    await expect(page.getByRole("heading", { name: "Authority chain", level: 3 })).toBeVisible();
    await expect(page.getByText(/ctx-snap-fixture/)).toBeVisible();
    await expect(page.getByText(/graph-snap-fixture/)).toBeVisible();

    const manifestLink = page.getByRole("link", { name: FIXTURE_MANIFEST_ID });
    await expect(manifestLink).toBeVisible();
    await manifestLink.click();

    await expect(page.getByRole("heading", { name: "Manifest", level: 2 })).toBeVisible();
    await expect(page.getByText("Manifest ID:", { exact: false })).toBeVisible();
    await expect(page.getByText(FIXTURE_MANIFEST_ID)).toBeVisible();
    await expect(page.getByText("Status:", { exact: false })).toBeVisible();
    await expect(page.locator("main").getByText("Accepted").first()).toBeVisible();
    await expect(page.getByText(/E2E fixture manifest:/)).toBeVisible();

    await expect(page.getByRole("heading", { name: "Artifacts", level: 3 })).toBeVisible();
    await expect(page.getByRole("table")).toBeVisible();
    await expect(page.getByRole("columnheader", { name: "Artifact" })).toBeVisible();
    await expect(page.getByText("architecture-overview.md", { exact: true })).toBeVisible();
    await expect(page.getByText("topology.mmd", { exact: true })).toBeVisible();

    await expect(page.getByRole("link", { name: "Download bundle (ZIP)" })).toBeVisible();

    await page.getByRole("link", { name: "Run detail" }).click();

    await expect(page.getByRole("heading", { name: "Run detail", level: 2 })).toBeVisible();
    await expect(page.getByText(FIXTURE_RUN_ID)).toBeVisible();
  });
});

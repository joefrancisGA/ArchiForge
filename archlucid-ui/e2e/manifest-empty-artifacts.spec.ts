/**
 * Operator semantics (manifest detail):
 * - A **200 + `[]`** artifact list is a **valid empty catalog**: summary and bundle affordance still apply.
 * - That is **not** the same as an **artifact-list request failure** (warning callout + “could not be loaded”).
 * - Bundle download is a **separate** step: the empty-state copy notes the ZIP may still 404 at download time.
 */
import { expect, test } from "@playwright/test";

import { FIXTURE_MANIFEST_EMPTY_ARTIFACTS_ID } from "./fixtures";
import { gotoManifestEmptyArtifactsOperatorCase } from "./helpers/operator-journey";

test.describe("operator journey — manifest empty artifact list", () => {
  test("shows valid-empty state, operator copy, and bundle link (mock API only)", async ({ page }) => {
    await gotoManifestEmptyArtifactsOperatorCase(page);

    await expect(page.getByRole("heading", { name: "Manifest", level: 2 })).toBeVisible();
    await expect(page.getByText(FIXTURE_MANIFEST_EMPTY_ARTIFACTS_ID)).toBeVisible();

    await expect(page.getByText("Artifact list could not be loaded.", { exact: true })).toHaveCount(0);
    await expect(page.getByText("Artifact list response was not usable.", { exact: true })).toHaveCount(0);

    const emptyRegion = page.getByRole("status").filter({
      has: page.getByText("No artifacts listed for this manifest", { exact: true }),
    });
    await expect(emptyRegion).toBeVisible();
    await expect(emptyRegion.getByText(/valid empty result/)).toBeVisible();
    await expect(emptyRegion.getByText(/Bundle ZIP may return 404/)).toBeVisible();

    const bundleLink = page.getByRole("link", { name: "Download bundle (ZIP)" });
    await expect(bundleLink).toBeVisible();
    await expect(bundleLink).toHaveAttribute("href", new RegExp(encodeURIComponent(FIXTURE_MANIFEST_EMPTY_ARTIFACTS_ID)));
    await expect(bundleLink).toHaveAttribute("href", /bundle/);

    await expect(page.getByRole("columnheader", { name: "Artifact" })).toHaveCount(0);
  });
});

import { expect, type Page } from "@playwright/test";

import {
  FIXTURE_LEFT_RUN_ID,
  FIXTURE_MANIFEST_EMPTY_ARTIFACTS_ID,
  FIXTURE_RIGHT_RUN_ID,
  FIXTURE_RUN_ID,
} from "../fixtures";

// --- Navigation (deterministic operator paths; defaults match shared fixtures) ---

/** Query string for `/compare` using the standard E2E run pair. */
export function comparePairSearchParams(
  leftRunId: string = FIXTURE_LEFT_RUN_ID,
  rightRunId: string = FIXTURE_RIGHT_RUN_ID,
): string {
  return new URLSearchParams({ leftRunId, rightRunId }).toString();
}

/** Opens compare with prefilled left/right (URL params). Does not register routes. */
export async function gotoComparePageWithFixturePair(
  page: Page,
  leftRunId: string = FIXTURE_LEFT_RUN_ID,
  rightRunId: string = FIXTURE_RIGHT_RUN_ID,
): Promise<void> {
  await page.goto(`/compare?${comparePairSearchParams(leftRunId, rightRunId)}`);
}

/** Run detail for the standard mock-api run fixture (`e2e/mock-archiforge-api-server`). */
export async function gotoRunDetailForMockFixtureRun(page: Page): Promise<void> {
  await page.goto(`/runs/${encodeURIComponent(FIXTURE_RUN_ID)}`);
}

/** Manifest detail for a known id (encode-safe). */
export async function gotoManifestDetail(page: Page, manifestId: string): Promise<void> {
  await page.goto(`/manifests/${encodeURIComponent(manifestId)}`);
}

/** Manifest page wired in the mock server for empty artifact list semantics. */
export async function gotoManifestEmptyArtifactsOperatorCase(page: Page): Promise<void> {
  await gotoManifestDetail(page, FIXTURE_MANIFEST_EMPTY_ARTIFACTS_ID);
}

// --- Assertions (only where duplicated across specs) ---

/** After Compare succeeds, the summary strip for structured + legacy outcomes. */
export async function expectComparisonRequestOutcomeVisible(page: Page): Promise<void> {
  await expect(page.getByRole("region", { name: "Comparison request outcome" })).toBeVisible();
}

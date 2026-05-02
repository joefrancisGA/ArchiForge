import { canonicalizeDemoRunId } from "@/lib/demo-run-canonical";
import { isBuyerSafeDemoMarketingChromeEnv } from "@/lib/demo-ui-env";
import { isDemoRunIdEligibleForStaticFallback } from "@/lib/operator-static-demo";
import {
  SHOWCASE_STATIC_DEMO_MANIFEST_ID,
  SHOWCASE_STATIC_DEMO_RUN_ID,
} from "@/lib/showcase-static-demo";

export type PrimaryReviewExploreLink = {
  readonly href: string;
  readonly label: string;
};

/** Curated buyer walkthrough — stable when authenticated review detail routes throw in client-only demos. */
export function getShowcaseWalkthroughHref(): string {
  return `/showcase/${encodeURIComponent(SHOWCASE_STATIC_DEMO_RUN_ID)}`;
}

/** Finalized manifest for the Claims Intake static spine. */
export function getShowcaseManifestHref(): string {
  return `/manifests/${encodeURIComponent(SHOWCASE_STATIC_DEMO_MANIFEST_ID)}`;
}

/**
 * In buyer-safe demo builds, prefer destinations that ship without `/reviews/[id]` (detail can hit the segment error
 * boundary when hydration throws). Canonical workspace detail remains available behind an explicit tertiary link.
 */
export function isBuyerSafePrimaryReviewNavigationPreferred(runId: string): boolean {
  if (!isBuyerSafeDemoMarketingChromeEnv()) {
    return false;
  }

  const id = canonicalizeDemoRunId(runId.trim());

  return isDemoRunIdEligibleForStaticFallback(id);
}

/** Primary authenticated “next step” column on the reviews table and similar hero CTAs. */
export function getBuyerSafeReviewsTableLink(runId: string): PrimaryReviewExploreLink {
  const id = canonicalizeDemoRunId(runId.trim());

  /** Manifest-first for the curated Claims Intake spine — avoids `/reviews/[id]` when detail hydration is brittle. */
  if (isDemoRunIdEligibleForStaticFallback(id)) {
    return { href: getShowcaseManifestHref(), label: "Review package" };
  }

  return {
    href: `/reviews/${encodeURIComponent(id)}`,
    label: "Open review",
  };
}

/** Full workspace detail (`/reviews/...`) for operators and staging — still used as a tertiary action in buyer-safe demos. */
export function getCanonicalReviewWorkspaceHref(runId: string): string {
  return `/reviews/${encodeURIComponent(canonicalizeDemoRunId(runId))}`;
}

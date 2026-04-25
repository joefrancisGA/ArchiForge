/**
 * Single source of truth for the buyer-facing vertical-picker labels rendered on
 * `/get-started`. The CI guard `scripts/ci/assert_buyer_first_30_minutes_in_sync.py`
 * asserts this list matches the on-disk slugs under `templates/briefs/` exactly.
 *
 * Owner decision Q2 (Resolved 2026-04-23 sixth pass — see `docs/PENDING_QUESTIONS.md`):
 * "Use existing `templates/briefs/*` folder slugs as the visible labels (defaults today)."
 */
export const BUYER_GET_STARTED_VERTICAL_SLUGS = [
  "financial-services",
  "healthcare",
  "public-sector",
  "public-sector-us",
  "retail",
  "saas",
] as const;

export type BuyerGetStartedVerticalSlug = (typeof BUYER_GET_STARTED_VERTICAL_SLUGS)[number];

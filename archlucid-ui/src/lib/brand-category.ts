/**
 * Brand-neutral content seam for the buyer-facing product category.
 *
 * This module is the **single point of truth** for the buyer-facing category
 * label that historically read "AI Architecture Intelligence" and is being
 * repositioned to "AI Architecture Review Board" per owner decisions Q6 / Q7
 * (Resolved 2026-04-23 sixth pass — see `docs/PENDING_QUESTIONS.md`).
 *
 * To rebrand again in the future:
 *   1. Change `BRAND_CATEGORY` here.
 *   2. Run `npm run rebrand-check` (or `python scripts/ci/assert_brand_category_seam.py`)
 *      from the repo root to find any surface that still hardcodes the legacy
 *      string. The script ships in WARN mode through the V1 rebrand workstream
 *      (PR-1 through PR-6 in `docs/architecture/REBRAND_WORKSTREAM_2026_04_23.md`)
 *      and flips to FAIL mode in PR-7.
 *   3. Each follow-on PR replaces a hardcoded legacy occurrence with an import
 *      of `BRAND_CATEGORY` from this file.
 *
 * Why both exports stay live:
 *   - `BRAND_CATEGORY` is what every UI surface should render going forward.
 *   - `BRAND_CATEGORY_LEGACY` stays exported deliberately so SEO redirect
 *     handlers and analytics tag mappers can still resolve the historical
 *     phrase for ~30 days after the workstream completes. Removing it would
 *     break any external inbound link / search-result snippet that still
 *     references the legacy phrase.
 *
 * The CI guard `scripts/ci/assert_brand_category_seam.py` treats this file as
 * the small allow-list — it is the only file under `archlucid-ui/src/` that
 * is permitted to mention the legacy string without also importing
 * `BRAND_CATEGORY_LEGACY` (it IS the export site).
 */

export const BRAND_CATEGORY = "AI Architecture Review Board";

export const BRAND_CATEGORY_LEGACY = "AI Architecture Intelligence";

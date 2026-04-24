> **Scope:** "AI Architecture Review Board" rebrand workstream — execution tracker. Created 2026-04-23 from the V1-scheduled rebrand decision (owner Q6 / Q7, PENDING_QUESTIONS sixth pass). Removed when PR-7 (closing) merges on `main`.

# "AI Architecture Review Board" rebrand workstream

## Why this exists

[`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) Q6 / Q7 (Resolved 2026-04-23, sixth pass) scheduled the rebrand from **"AI Architecture Intelligence"** to **"AI Architecture Review Board"** for V1, with the explicit stipulation that the workstream consumes **separate session(s)** and is sequenced **after the brand-neutral content seam ships** so each surface flip is a one-line change.

This file is the running checklist for the seven PRs that complete the workstream. It is the working surface — the normative inputs are owner Q6 / Q7 in [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md), Improvement 4 in [`QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md`](../QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md) §3, and Prompt 4 in [`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_23_73_20.md`](../CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_23_73_20.md).

## Architecture (decomposition)

- **Interface (single point of truth):** [`archlucid-ui/src/lib/brand-category.ts`](../../archlucid-ui/src/lib/brand-category.ts) — exports `BRAND_CATEGORY` (new) and `BRAND_CATEGORY_LEGACY` (kept for SEO redirect handlers and analytics tag mappers, ~30-day window).
- **Service:** the seam is import-only — no runtime read, no env var, no orchestration layer. Each surface that needs the buyer-facing category label imports `BRAND_CATEGORY` and renders it inline.
- **Data model:** two `string` constants. Compile-time checked via TypeScript; SEO meta tag is set via Next.js `Metadata.other`.
- **Orchestration layer:** [`scripts/ci/assert_brand_category_seam.py`](../../scripts/ci/assert_brand_category_seam.py) — WARN through PR-1..PR-6, FAIL in PR-7. The guard exists because the rebrand is **multi-PR**: without it, a new occurrence of the legacy string could land mid-workstream and silently bypass the seam.

## Security / Scalability / Reliability / Cost

- **Security:** N/A — buyer-facing copy only; no auth, no PII, no policy.
- **Scalability:** N/A — static const exports, statically rendered.
- **Reliability:** the legacy string stays exported for ~30 days post-PR-7 so external inbound links (search-result snippets, social cards, partner redirects) keep resolving via the SEO redirect handler. Removing `BRAND_CATEGORY_LEGACY` before that window expires is explicitly out of scope until a separate ADR.
- **Cost:** zero infra cost; engineering cost is the seven PRs below.

## PR sequence

| PR | Surfaces | Status | Owner of execution |
|----|----------|--------|--------------------|
| **PR-1** (this PR) | `archlucid-ui/src/lib/brand-category.ts` (new seam) + `archlucid-ui/src/app/(marketing)/why/page.tsx` flip + WARN-mode CI guard `scripts/ci/assert_brand_category_seam.py` + `npm run rebrand-check` script + Vitest spec asserting `/why` renders `BRAND_CATEGORY` not the legacy string | **In flight** (this PR) | assistant |
| **PR-2** | `archlucid-ui/src/app/(marketing)/welcome/page.tsx` (page-metadata `description`) + `archlucid-ui/src/app/(marketing)/get-started/page.tsx` (any landing prose) + `archlucid-ui/src/app/(marketing)/pricing/page.tsx` (any landing prose) | **DONE** (2026-04-24) | assistant |
| **PR-3** | `docs/EXECUTIVE_SPONSOR_BRIEF.md` + `docs/go-to-market/COMPETITIVE_LANDSCAPE.md` (incl. comparison-table caption / row label rewrites) | **Pending** — separate session | assistant |
| **PR-4** | Per-vertical brief docs under `templates/briefs/**/brief.md` (financial-services, healthcare, public-sector, public-sector-us, retail, saas) | **Pending** — separate session | assistant |
| **PR-5** | `docs/trust-center.md` + `docs/library/PRODUCT_PACKAGING.md` (procurement-pack-facing surfaces) | **Pending** — separate session | assistant |
| **PR-6** | In-product copy: operator-shell governance pages (`archlucid-ui/src/app/(operator)/**`), navigation labels (`archlucid-ui/src/lib/nav-config.ts`), `LayerHeader` / layer guidance copy if it carries the legacy phrase. Stop-and-ask boundary in Prompt 4 explicitly carves operator-shell copy out of PR-1; this is the only PR allowed to touch it. | **Pending** — separate session | assistant |
| **PR-7** (closing) | Flip `scripts/ci/assert_brand_category_seam.py` invocation in `.github/workflows/ci.yml` from default WARN to `--fail`. Confirm zero offenders. Optionally delete this tracker file (replaced by the CI guard). | **Pending** — separate session | assistant |

## How each follow-on PR works

For every surface in PR-2 through PR-6:

1. Replace each occurrence of the literal string `AI Architecture Intelligence` with one of:
   - For TSX: `{BRAND_CATEGORY}` after `import { BRAND_CATEGORY } from "@/lib/brand-category";`.
   - For Markdown: the literal **AI Architecture Review Board** (Markdown is not executable; no import).
2. If the legacy phrase is needed for SEO / redirect / analytics purposes inside the same file, keep it but ensure the file ALSO references `BRAND_CATEGORY_LEGACY` (escape hatch — the CI guard's allow-list rule).
3. Run `npm run rebrand-check` (or `python scripts/ci/assert_brand_category_seam.py` from repo root). The WARN list should shrink by exactly one file.
4. Add an entry to [`docs/CHANGELOG.md`](../CHANGELOG.md) noting the surface flipped and which PR in this tracker it satisfies.

## Stop-and-ask boundaries (carried over from Prompt 4)

- **Do NOT** touch the in-product operator-shell copy in PR-1. That is **PR-6**.
- **Do NOT** remove `BRAND_CATEGORY_LEGACY` from the seam file in any PR. SEO redirect handlers depend on it for ~30 days post-PR-7.
- **Do NOT** flip the CI guard to `--fail` mode before PR-6 has merged. The guard exists to prevent **new** occurrences mid-workstream, not to block **existing** occurrences that the workstream will resolve in order.

## Verification at each step

- `npm run rebrand-check` — runs the seam guard from the UI workspace.
- `python scripts/ci/assert_brand_category_seam.py` — runs the seam guard from repo root.
- `python -m unittest discover -s scripts/ci/tests -p "test_assert_brand_category_seam.py"` — self-test asserts the guard catches both directions (legitimate-with-escape-hatch passes, hardcoded legacy fails in `--fail` mode).
- `npx vitest run src/app/'(marketing)'/why/WhyArchlucidMarketingView.test.tsx` — confirms `/why` continues rendering `BRAND_CATEGORY` (not the legacy string).

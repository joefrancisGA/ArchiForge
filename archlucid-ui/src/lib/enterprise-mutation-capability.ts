import { AUTHORITY_RANK } from "@/lib/nav-authority";

/**
 * Whether the caller’s numeric rank should **soft-enable** Execute-class POST/toggle controls on Enterprise-heavy
 * operator pages (governance workflow, policy lifecycle, alert tooling writes, inbox triage, etc.).
 *
 * **UI shaping only — API authoritative:** `true` does **not** guarantee the HTTP call succeeds; **ArchLucid.Api**
 * `[Authorize(Policy = …)]` still returns **401/403**. This hook exists so buttons and shortcuts match the same story as nav.
 *
 * **Single threshold with nav:** `rank >= AUTHORITY_RANK.ExecuteAuthority` — the same numeric floor used for
 * **`requiredAuthority: "ExecuteAuthority"`** link visibility after **`filterNavLinksByAuthority`** (**`nav-config.ts`** rows
 * + **`nav-authority.ts`**; see **docs/PRODUCT_PACKAGING.md** §3 *Read vs Execute* and **docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md** §4). Matches **`CurrentPrincipal.hasEnterpriseOperatorSurfaces`**
 * (**`normalizeAuthMeResponse`**); **`maxAuthority`** there tracks **`requiredAuthorityFromRank(authorityRank)`**
 * (**`current-principal.test.ts`**). Rank comes from **`current-principal.ts`** / **`useNavCallerAuthorityRank()`**
 * (conservative **Read** while JWT **`/me`** refetches — **`OperatorNavAuthorityProvider`**). **Not** progressive disclosure
 * (**`nav-shell-visibility.ts`**) and **not** buyer “which layer” copy (**`layer-guidance.ts`** / **`LayerHeader`** — same rank
 * threshold for Enterprise **cue** text only).
 *
 * @see `authority-seam-regression.test.ts` (rank vs **`navLinkVisibleForCallerRank`** for **`ExecuteAuthority`** links;
 *   Enterprise monotonicity / tier gates are **nav-only** — this function must stay the **Execute+ mutation** floor only),
 *   **`authority-execute-floor-regression.test.ts`** (minimal loop: synthetic **`ExecuteAuthority`** nav row visibility **≡** this function per rank),
 *   `use-enterprise-mutation-capability.test.tsx`, `OperatorNavAuthorityProvider.test.tsx`, `enterprise-mutation-capability.test.ts`,
 *   `enterprise-authority-ui-shaping.test.tsx` (pages still wire this hook to **`disabled`** controls).
 */
export function enterpriseMutationCapabilityFromRank(rank: number): boolean {
  return rank >= AUTHORITY_RANK.ExecuteAuthority;
}

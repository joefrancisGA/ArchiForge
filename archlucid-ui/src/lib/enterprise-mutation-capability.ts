import { AUTHORITY_RANK } from "@/lib/nav-authority";

/**
 * True when the numeric authority rank is expected to satisfy **Execute**-class mutations in the operator UI
 * for **Enterprise Controls** and operator-heavy **alerts-governance** pages (governance workflow, dashboard writes,
 * policy pack lifecycle, alert rule/routing/composite creates, alerts inbox acknowledge/resolve/suppress, governance
 * dashboard empty-state copy, policy packs empty-scope copy, alert rules inspect-first layout + empty list copy, etc.). Matches
 * the Reader vs Operator framing of `EnterpriseControlsExecutePageHint` /
 * `EnterpriseExecutePlusPageCue` in `EnterpriseControlsContextHints.tsx`—not a second authZ engine; the API still
 * returns 401/403. See **docs/PRODUCT_PACKAGING.md** (§3, code seams + contributor drift guard) and
 * **docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md** Stage 1.
 *
 * **Same rank source as nav:** uses the numeric rank from **`current-principal.ts`** / **`useNavCallerAuthorityRank()`**;
 * Reader-tier users should not see Execute-only **nav links** *and* should see **soft-disabled** mutation controls where this hook is wired.
 *
 * @see `authority-seam-regression.test.ts`, `use-enterprise-mutation-capability.test.tsx`, `enterprise-mutation-capability.test.ts`
 */
export function enterpriseMutationCapabilityFromRank(rank: number): boolean {
  return rank >= AUTHORITY_RANK.ExecuteAuthority;
}

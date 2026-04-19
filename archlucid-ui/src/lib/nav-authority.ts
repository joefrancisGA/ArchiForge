/**
 * ## Purpose
 *
 * UI-side authority hints for operator navigation (sidebar, mobile drawer, command palette). Policy **names** mirror
 * **`ArchLucidPolicies`** on **`ArchLucid.Api`** (`ReadAuthority` / `ExecuteAuthority` / `AdminAuthority` in **`ArchLucid.Core`**)
 * so the shell stays aligned with RBAC vocabulary — **not** a second authorization engine.
 *
 * ## API vs UI (hard boundary)
 *
 * **`[Authorize(Policy = …)]`** on controllers is **authoritative** (**401/403**). This module answers *whether a nav
 * destination should appear for rank R* and supplies **`maxAuthorityRankFromMeClaims`** for **`GET /api/auth/me`**
 * parsing. **Deep links** still hit the route; omission here never implies a POST or toggle is allowed.
 *
 * ## Read-tier vs Execute+ (same numerics, different surfaces)
 *
 * - **Nav / palette:** **`navLinkVisibleForCallerRank`** / **`filterNavLinksByAuthority`** — link visible iff
 *   `callerRank >= requiredAuthorityRank(link.requiredAuthority)` (missing `requiredAuthority` ⇒ visible at every rank).
 * - **Enterprise write affordances:** **`enterpriseMutationCapabilityFromRank`** / **`useEnterpriseMutationCapability()`**
 *   — **`callerRank >= AUTHORITY_RANK.ExecuteAuthority`**. **`CurrentPrincipal.hasEnterpriseOperatorSurfaces`**
 *   (**`current-principal.ts`**) must use that **same numeric floor** (see **`current-principal.test.ts`**).
 *   Shell composition is **tier → authority** in **`nav-shell-visibility.ts`**; empty groups are dropped there.
 *
 * **`ReadAuthority`** on a **`NavLinkItem`** marks read-mostly destinations; **`ExecuteAuthority`** marks Execute-class
 * primary workflows (replay, governance workflow, selected alert configuration). **`AdminAuthority`** is rare on nav;
 * many admin-only POSTs stay server-gated while list pages stay **`ReadAuthority`** — see **`nav-config.ts`** header.
 *
 * ## Packaging references
 *
 * **`nav-config.ts`** owns link metadata. Contributor order: **docs/PRODUCT_PACKAGING.md** §3 *Contributor drift guard*;
 * Stage 1 framing: **docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md** §4.
 *
 * @see `authority-seam-regression.test.ts` — `/me` claims → rank vs Enterprise nav vs mutation capability; config-wide Execute rows vs Read; Auditor Enterprise parity.
 * @see `nav-authority.test.ts` — `navLinkVisibleForCallerRank` Execute floor; `nav-config.structure.test.ts` — packaging invariants on **`NAV_GROUPS`**.
 * @see `EnterpriseControlsContextHints.authority.test.tsx` — rank-gated Enterprise cue components (same **`ExecuteAuthority`** threshold).
 * @see `current-principal.test.ts` — **`normalizeAuthMeResponse`**: **`maxAuthority`** must track **`requiredAuthorityFromRank(authorityRank)`**.
 * @see `OperatorNavAuthorityProvider.test.tsx` — rank fed into consumers of this module during JWT `/me` refetch.
 * @see `enterprise-authority-ui-shaping.test.tsx` — **`useEnterpriseMutationCapability`** still gates **`disabled`** on representative Enterprise pages (UI only).
 */

/** Same strings as server `ArchLucidPolicies` — smallest durable contract for nav links. */
export type RequiredAuthority = "ReadAuthority" | "ExecuteAuthority" | "AdminAuthority";

/** Monotonic rank for comparisons: higher means the caller may access more `requiredAuthority` labels. */
export const AUTHORITY_RANK: Readonly<Record<RequiredAuthority, number>> = {
  ReadAuthority: 1,
  ExecuteAuthority: 2,
  AdminAuthority: 3,
};

export function requiredAuthorityRank(required: RequiredAuthority): number {
  return AUTHORITY_RANK[required];
}

/**
 * Returns true when the caller's **maximum** satisfied policy rank is at least the link's optional requirement.
 * Missing `requiredAuthority` stays visible for all ranks (Core Pilot breadth, backward compatibility).
 */
export function navLinkVisibleForCallerRank<T extends { requiredAuthority?: RequiredAuthority }>(
  link: T,
  callerRank: number,
): boolean {
  if (link.requiredAuthority === undefined || link.requiredAuthority === null) {
    return true;
  }

  return callerRank >= requiredAuthorityRank(link.requiredAuthority);
}

export function filterNavLinksByAuthority<T extends { requiredAuthority?: RequiredAuthority }>(
  links: ReadonlyArray<T>,
  callerRank: number,
): T[] {
  return links.filter((link) => navLinkVisibleForCallerRank(link, callerRank));
}

/** Role claim values aligned with `ArchLucidRoles` on the API (case-insensitive). */
const ROLE_ADMIN = "Admin";
const ROLE_OPERATOR = "Operator";
const ROLE_READER = "Reader";
const ROLE_AUDITOR = "Auditor";

const ROLE_URI_SUFFIX = "/claims/role";

/**
 * Derives the caller's maximum authority rank from `GET /api/auth/me` claim rows.
 * Uses `ClaimTypes.Role` and Entra-style `roles` claims; ignores unknown role strings.
 */
export function maxAuthorityRankFromMeClaims(claims: ReadonlyArray<{ type: string; value: string }>): number {
  let rank = 0;

  for (const claim of claims) {
    if (!isArchLucidRoleClaimType(claim.type)) {
      continue;
    }

    const value = claim.value.trim();

    if (value.length === 0) {
      continue;
    }

    rank = Math.max(rank, rankForRoleValue(value));
  }

  if (rank === 0) {
    return AUTHORITY_RANK.ReadAuthority;
  }

  return rank;
}

export function isArchLucidRoleClaimType(type: string): boolean {
  if (type === "roles") {
    return true;
  }

  if (type === "http://schemas.microsoft.com/ws/2008/06/identity/claims/role") {
    return true;
  }

  return type.endsWith(ROLE_URI_SUFFIX);
}

/**
 * Raw role claim values from `/api/auth/me` (deduped case-insensitively, order preserved).
 * Used by {@link loadCurrentPrincipal}; not a security boundary — server still enforces policies.
 */
export function collectArchLucidRoleClaimValues(
  claims: ReadonlyArray<{ type: string; value: string }>,
): string[] {
  const seen = new Set<string>();
  const out: string[] = [];

  for (const claim of claims) {
    if (!isArchLucidRoleClaimType(claim.type)) {
      continue;
    }

    const value = claim.value.trim();

    if (value.length === 0) {
      continue;
    }

    const key = value.toLowerCase();

    if (seen.has(key)) {
      continue;
    }

    seen.add(key);
    out.push(value);
  }

  return out;
}

/** Maps a resolved rank (from {@link maxAuthorityRankFromMeClaims}) back to a policy label. */
export function requiredAuthorityFromRank(rank: number): RequiredAuthority {
  if (rank >= AUTHORITY_RANK.AdminAuthority) {
    return "AdminAuthority";
  }

  if (rank >= AUTHORITY_RANK.ExecuteAuthority) {
    return "ExecuteAuthority";
  }

  return "ReadAuthority";
}

function rankForRoleValue(value: string): number {
  const normalized = value.trim();

  if (normalized.length === 0) {
    return 0;
  }

  const lower = normalized.toLowerCase();

  if (lower === ROLE_ADMIN.toLowerCase()) {
    return AUTHORITY_RANK.AdminAuthority;
  }

  if (lower === ROLE_OPERATOR.toLowerCase()) {
    return AUTHORITY_RANK.ExecuteAuthority;
  }

  if (lower === ROLE_READER.toLowerCase() || lower === ROLE_AUDITOR.toLowerCase()) {
    return AUTHORITY_RANK.ReadAuthority;
  }

  return 0;
}

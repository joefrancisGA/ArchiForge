/**
 * UI-side authority hints for operator navigation (sidebar, mobile drawer, command palette).
 * Mirrors server `ArchLucidPolicies` (`ReadAuthority` / `ExecuteAuthority` / `AdminAuthority` in `ArchLucid.Core`)
 * so nav shaping can align with API RBAC without duplicating a full authZ engine.
 *
 * **Packaging alignment:** `ReadAuthority` on a `NavLinkItem` marks **read-mostly** destinations (typical Advanced
 * Analysis inspection and many **Enterprise Controls** evidence or inbox views). `ExecuteAuthority` marks workflows
 * whose primary API verbs are Execute-class (replay, governance workflow, selected alert configuration). **`AdminAuthority`**
 * is reserved for nav only when a whole area is admin-scoped; many admin-only POSTs stay gated on the server while
 * the list page stays `ReadAuthority`—see comments in `nav-config.ts`.
 *
 * This is **not** enforcement: routes still 401/403 from the API. It is structural metadata for progressive disclosure + role-aware nav.
 * Enterprise **in-page** mutation affordances reuse the same **Execute+** rank threshold as `enterpriseMutationCapabilityFromRank` in `enterprise-mutation-capability.ts`.
 *
 * **Read-tier vs Execute+ in the UI:** **`navLinkVisibleForCallerRank`** / **`filterNavLinksByAuthority`** only decide
 * whether a **destination appears** in nav (and empty groups are dropped in `nav-shell-visibility.ts`). **`rank >= Execute`**
 * is additionally used for **soft-enabled POST/toggle controls** via `useEnterpriseMutationCapability()` — same numeric
 * scale, different surface. Packaging narrative: **docs/PRODUCT_PACKAGING.md** §3; Stage 1 framing:
 * **docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md** §4.
 *
 * @see `authority-seam-regression.test.ts` — `/me` claims → rank vs Enterprise nav vs mutation capability.
 * @see `nav-authority.test.ts` — `navLinkVisibleForCallerRank` Execute floor; `EnterpriseControlsContextHints.authority.test.tsx` — cue components.
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

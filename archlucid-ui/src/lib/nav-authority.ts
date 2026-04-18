/**
 * UI-side authority hints for operator navigation (sidebar, mobile drawer, command palette).
 * Mirrors server `ArchLucidPolicies` (`ReadAuthority` / `ExecuteAuthority` / `AdminAuthority` in `ArchLucid.Core`)
 * so nav shaping can align with API RBAC without duplicating a full authZ engine.
 *
 * This is **not** enforcement: routes still 401/403 from the API. It is structural metadata for progressive disclosure + role-aware nav.
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
    if (!isRoleClaimType(claim.type)) {
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

function isRoleClaimType(type: string): boolean {
  if (type === "roles") {
    return true;
  }

  if (type === "http://schemas.microsoft.com/ws/2008/06/identity/claims/role") {
    return true;
  }

  return type.endsWith(ROLE_URI_SUFFIX);
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

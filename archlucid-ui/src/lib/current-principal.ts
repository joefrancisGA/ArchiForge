/**
 * UI read-model for the authenticated operator principal.
 *
 * ## Endpoint and JSON shape
 *
 * - **Upstream API:** `GET /api/auth/me` (`ArchLucid.Api.Controllers.Admin.AuthDebugController`), same body as
 *   **`CallerIdentityResponse`**: `{ "name": string | null, "claims": [ { "type", "value" }, ... ] }`.
 * - **Browser (same-origin):** `GET /api/proxy/api/auth/me` — forwards bearer / API key + scope headers; see
 *   `src/app/api/proxy/[...path]/route.ts`.
 *
 * ## Public API (use these; do not add parallel `/me` clients)
 *
 * - **`loadCurrentPrincipal` / `getCurrentPrincipal`** — full **`CurrentPrincipal`** (name, roles, rank, flags).
 * - **`getCurrentAuthority`** — `ReadAuthority` | `ExecuteAuthority` | `AdminAuthority` only.
 * - **`getCurrentAuthorityRank`** — numeric rank (see **`AUTHORITY_RANK`** in `nav-authority.ts`).
 * - **`normalizeAuthMeResponse`** — pure parse for tests or callers that already have JSON.
 * - **`buildAuthMeProxyRequestInit`** — shared `RequestInit` (bearer + registration scope) for `/me` or diagnostics.
 *
 * ## Where `authorityRank` flows (keep in sync)
 *
 * **`OperatorNavAuthorityProvider`** exposes the same rank to **`useNavCallerAuthorityRank()`**, which feeds
 * **`filterNavLinksForOperatorShell`** / **`listNavGroupsVisibleInOperatorShell`** (`nav-shell-visibility.ts`) and
 * **`useEnterpriseMutationCapability()`** (Execute+ floor in **`enterprise-mutation-capability.ts`**). Page-level **layout**
 * (inspect-first columns when mutation is off) also keys off that hook on some routes — see **`authority-shaped-layout-regression.test.tsx`**.
 * **`LayerHeader`**
 * Enterprise rank cue uses the **same numeric Execute boundary** for in-strip copy (**not** tier disclosure — that stays in
 * **`nav-shell-visibility.ts`**). Some routes also read **`useEnterpriseMutationCapability()`** for paragraphs that are not
 * the rank cue (e.g. governance resolution **Change related controls** supplement — same policies story, **second hook**;
 * **`enterprise-authority-ui-shaping.test.tsx`**). Packaging enumeration: **docs/PRODUCT_PACKAGING.md** §3 *Four UI shaping surfaces*.
 * **`hasEnterpriseOperatorSurfaces`**
 * uses that **same Execute floor** as **`enterpriseMutationCapabilityFromRank(authorityRank)`** — do not diverge (guarded in
 * **`current-principal.test.ts`**). **Progressive disclosure** (`nav-tier`) is **not** applied here; it runs only in
 * **`nav-shell-visibility.ts`** before **`filterNavLinksByAuthority`**. Drift between consumers breaks the story in
 * **docs/PRODUCT_PACKAGING.md** §3 (*Code seams* read-model + *Contributor drift guard*).
 * **Stage 1 framing (not entitlements):** **docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md** §4 maps buyer “role clarity” to these modules — same read-model, no licensing.
 *
 * **Route-local exception (still API-authoritative):** audit **CSV export** enablement on **`/audit`** uses raw
 * **`roleClaimValues`** (**Auditor** / **Admin**) to mirror **`RequireAuditor`** — not **`useEnterpriseMutationCapability()`**
 * and not **`LayerHeader`** rank lines. Do not “fix” export gating by moving it to **`authorityRank`** without an explicit product decision.
 *
 * ## Role → policy normalization (UX only; server enforces policies)
 *
 * App roles are read from Entra-style **`roles`** and **`ClaimTypes.Role`** claims (`nav-authority.ts`).
 * Highest role wins; unknown roles contribute nothing (then Reader default rank applies in **`maxAuthorityRankFromMeClaims`**).
 *
 * | App role (claim) | `maxAuthority`        | `authorityRank` | `hasEnterpriseOperatorSurfaces` |
 * |------------------|----------------------|-----------------|-----------------------------------|
 * | Reader, Auditor  | `ReadAuthority`      | 1               | false                             |
 * | Operator         | `ExecuteAuthority`   | 2               | true                              |
 * | Admin            | `AdminAuthority`     | 3               | true                              |
 *
 * ## Resilience (no full auth client)
 *
 * Synthetic **`CurrentPrincipal`** (conservative **Read**) when: non-browser SSR, JWT unsigned session, `/me` non-OK,
 * or network error — see **`CurrentPrincipalSyntheticReason`**. **`OperatorNavAuthorityProvider`** mirrors this for nav;
 * prefer **`useOperatorNavAuthority().currentPrincipal`** in the shell so shaping stays aligned.
 *
 * **Do not duplicate backend authorization** — use this only for UX shaping. The API remains authoritative for 401/403.
 *
 * **Alignment:** `authorityRank` / `maxAuthority` must stay consistent with **`nav-config.ts`** `requiredAuthority` on
 * each link (same policy names as `ArchLucidPolicies`). In **`normalizeAuthMeResponse`**, **`maxAuthority`** is always
 * **`requiredAuthorityFromRank(authorityRank)`** from **`nav-authority.ts`** — regression in **`current-principal.test.ts`**.
 * Contributor checklist: **docs/PRODUCT_PACKAGING.md** §3 *Contributor drift guard*.
 *
 * **Cross-module tests:** `authority-seam-regression.test.ts` exercises `normalizeAuthMeResponse` together with
 * nav visibility and mutation rank; **`authority-shaped-ui-regression.test.ts`** guards real **`NAV_GROUPS`**
 * **`ExecuteAuthority`** rows vs Read/Execute ranks and synthetic shell principals; **`authority-execute-floor-regression.test.ts`** locks **`navLinkVisibleForCallerRank`**
 * for a synthetic **`ExecuteAuthority`** link to the **same boolean** as **`enterpriseMutationCapabilityFromRank`** (prevents
 * nav/mutation threshold drift without re-reading full cross-module suite); **`OperatorNavAuthorityProvider.test.tsx`** locks conservative **`useNavCallerAuthorityRank`**
 * during JWT **`/me`** refetch; unit coverage remains in `current-principal.test.ts`. Rank-gated Enterprise copy
 * components: **`EnterpriseControlsContextHints.authority.test.tsx`** (same rank as nav / mutation). Page-level mutation
 * affordances: **`src/app/(operator)/enterprise-authority-ui-shaping.test.tsx`** (hook → **`disabled`** on representative routes).
 */

import {
  AUTHORITY_RANK,
  collectArchLucidRoleClaimValues,
  maxAuthorityRankFromMeClaims,
  requiredAuthorityFromRank,
  type RequiredAuthority,
} from "@/lib/nav-authority";
import { isJwtAuthMode } from "@/lib/oidc/config";
import { ensureAccessTokenFresh, getAccessTokenForApi, isLikelySignedIn } from "@/lib/oidc/session";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";

/** JSON body shape for `GET /api/auth/me` — mirrors `CallerIdentityResponse`. */
export type AuthMeResponse = {
  name?: string | null;
  claims?: ReadonlyArray<{ type: string; value: string }>;
};

/** ArchLucid app roles carried in JWT / dev-bypass claims (`ArchLucidRoles` on the server). */
export type ArchLucidAppRole = "Admin" | "Operator" | "Reader" | "Auditor";

export type CurrentPrincipalSyntheticReason = "jwt-unsigned" | "me-http" | "me-network" | "non-browser";

/**
 * Compact principal read-model for UI code paths (nav, feature hints, enterprise surfacing).
 * Prefer `loadCurrentPrincipal()` in client components; never assume this matches server enforcement.
 */
export type CurrentPrincipal = {
  /** `auth-me` when parsed from a successful `/me` response; `synthetic` when we did not call or could not trust `/me`. */
  provenance: "auth-me" | "synthetic";
  /** Populated only when `provenance === "synthetic"` — explains why we fell back. */
  syntheticReason?: CurrentPrincipalSyntheticReason;
  /** `User.Identity.Name` from the API when present */
  name: string | null;
  /** Distinct role claim values from `/me` (see `collectArchLucidRoleClaimValues` in `nav-authority.ts`) */
  roleClaimValues: readonly string[];
  /** Best-effort highest app role for labels; null when unknown (synthetic unsigned). */
  primaryAppRole: ArchLucidAppRole | null;
  /** Highest policy tier this principal is expected to satisfy — same strings as `ArchLucidPolicies` */
  maxAuthority: RequiredAuthority;
  /** Numeric rank for comparisons (1=Read, 2=Execute, 3=Admin) */
  authorityRank: number;
  /** True when the principal should see operator/admin-oriented Enterprise Controls hints (Execute+) */
  hasEnterpriseOperatorSurfaces: boolean;
};

const ME_PATH = "/api/proxy/api/auth/me";

/**
 * Builds default `RequestInit` for `/api/proxy/api/auth/me` in the browser (bearer + registration scope merge).
 * Reusable by any client code that needs the same headers as the operator shell.
 */
export async function buildAuthMeProxyRequestInit(): Promise<RequestInit> {
  await ensureAccessTokenFresh();

  const headers = new Headers({ Accept: "application/json" });
  const bearer = getAccessTokenForApi();

  if (bearer !== undefined && bearer !== null && bearer.trim().length > 0) {
    headers.set("Authorization", `Bearer ${bearer}`);
  }

  return mergeRegistrationScopeForProxy({
    cache: "no-store",
    credentials: "same-origin",
    headers,
  });
}

function createSyntheticPrincipal(reason: CurrentPrincipalSyntheticReason): CurrentPrincipal {
  return {
    provenance: "synthetic",
    syntheticReason: reason,
    name: null,
    roleClaimValues: [],
    primaryAppRole: null,
    maxAuthority: "ReadAuthority",
    authorityRank: AUTHORITY_RANK.ReadAuthority,
    hasEnterpriseOperatorSurfaces: false,
  };
}

/**
 * Read-tier placeholder before the operator shell’s first `loadCurrentPrincipal` settles.
 * Matches initial `callerAuthorityRank` in `OperatorNavAuthorityProvider` (conservative, Core Pilot–safe).
 */
export const shellBootstrapReadPrincipal: Readonly<CurrentPrincipal> = Object.freeze({
  provenance: "synthetic",
  syntheticReason: undefined,
  name: null,
  roleClaimValues: [],
  primaryAppRole: null,
  maxAuthority: "ReadAuthority",
  authorityRank: AUTHORITY_RANK.ReadAuthority,
  hasEnterpriseOperatorSurfaces: false,
});

/**
 * Principal returned when nav hooks run outside `OperatorNavAuthorityProvider` (e.g. isolated Vitest).
 * **Admin** rank keeps links visible; not a real session.
 */
export const operatorNavOutsideProviderPrincipal: Readonly<CurrentPrincipal> = Object.freeze({
  provenance: "synthetic",
  syntheticReason: undefined,
  name: null,
  roleClaimValues: ["Admin"],
  primaryAppRole: "Admin",
  maxAuthority: "AdminAuthority",
  authorityRank: AUTHORITY_RANK.AdminAuthority,
  hasEnterpriseOperatorSurfaces: true,
});

function primaryAppRoleFromRank(rank: number, roleClaimValues: readonly string[]): ArchLucidAppRole | null {
  if (rank >= AUTHORITY_RANK.AdminAuthority) {
    return "Admin";
  }

  if (rank >= AUTHORITY_RANK.ExecuteAuthority) {
    return "Operator";
  }

  if (rank >= AUTHORITY_RANK.ReadAuthority) {
    const lower = roleClaimValues.map((v) => v.toLowerCase());

    if (lower.includes("auditor")) {
      return "Auditor";
    }

    return "Reader";
  }

  return null;
}

/**
 * Normalizes a successful `AuthMeResponse` body into `CurrentPrincipal`.
 * Exported for tests and for callers that already obtained JSON (e.g. diagnostics).
 */
export function normalizeAuthMeResponse(payload: AuthMeResponse): CurrentPrincipal {
  const claims = payload.claims ?? [];
  const authorityRank = maxAuthorityRankFromMeClaims(claims);
  const roleClaimValues = collectArchLucidRoleClaimValues(claims);
  const maxAuthority = requiredAuthorityFromRank(authorityRank);

  return {
    provenance: "auth-me",
    name: payload.name ?? null,
    roleClaimValues,
    primaryAppRole: primaryAppRoleFromRank(authorityRank, roleClaimValues),
    maxAuthority,
    authorityRank,
    hasEnterpriseOperatorSurfaces: authorityRank >= AUTHORITY_RANK.ExecuteAuthority,
  };
}

/**
 * Loads the current principal from `/api/proxy/api/auth/me`.
 *
 * - **Non-browser:** returns a synthetic Read principal (`non-browser`) — do not call from RSC for real auth state.
 * - **JWT, not signed in:** synthetic Read (`jwt-unsigned`) without calling `/me`.
 * - **development-bypass:** calls `/me` using the proxy’s server API key so the dev role still shapes the UI.
 * - **`/me` failure:** conservative synthetic Read (`me-http` / `me-network`).
 */
export async function loadCurrentPrincipal(options?: { init?: RequestInit }): Promise<CurrentPrincipal> {
  if (typeof window === "undefined") {
    return createSyntheticPrincipal("non-browser");
  }

  if (isJwtAuthMode() && !isLikelySignedIn()) {
    return createSyntheticPrincipal("jwt-unsigned");
  }

  try {
    const init = options?.init ?? (await buildAuthMeProxyRequestInit());
    const response = await fetch(ME_PATH, init);

    if (!response.ok) {
      return createSyntheticPrincipal("me-http");
    }

    const body = (await response.json()) as AuthMeResponse;

    return normalizeAuthMeResponse(body);
  } catch {
    return createSyntheticPrincipal("me-network");
  }
}

/** Alias for `loadCurrentPrincipal` — same contract; pick whichever reads clearer at the call site. */
export const getCurrentPrincipal = loadCurrentPrincipal;

/**
 * Returns only the normalized max policy tier (Reader→ReadAuthority, Operator→ExecuteAuthority, Admin→AdminAuthority).
 * Prefer `loadCurrentPrincipal()` when you also need name, roles, or enterprise surfacing flags.
 */
export async function getCurrentAuthority(options?: { init?: RequestInit }): Promise<RequiredAuthority> {
  const principal = await loadCurrentPrincipal(options);

  return principal.maxAuthority;
}

/**
 * Returns the numeric authority rank (see `AUTHORITY_RANK` in `nav-authority.ts`).
 */
export async function getCurrentAuthorityRank(options?: { init?: RequestInit }): Promise<number> {
  const principal = await loadCurrentPrincipal(options);

  return principal.authorityRank;
}

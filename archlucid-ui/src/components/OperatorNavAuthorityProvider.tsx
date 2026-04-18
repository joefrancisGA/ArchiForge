"use client";

import { usePathname } from "next/navigation";
import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";

import { AUTHORITY_RANK, maxAuthorityRankFromMeClaims } from "@/lib/nav-authority";
import { isJwtAuthMode } from "@/lib/oidc/config";
import { ensureAccessTokenFresh, getAccessTokenForApi, isLikelySignedIn } from "@/lib/oidc/session";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";

type MeClaimsPayload = {
  claims?: ReadonlyArray<{ type: string; value: string }>;
};

export type OperatorNavAuthorityContextValue = {
  /** Monotonic 1=Read, 2=Execute, 3=Admin — use with `@/lib/nav-authority` helpers. */
  callerAuthorityRank: number;
  /** True while the first in-flight `/api/auth/me` attempt has not settled for this refresh cycle. */
  isAuthorityLoading: boolean;
};

const OperatorNavAuthorityContext = createContext<OperatorNavAuthorityContextValue | undefined>(undefined);

const DEFAULT_RANK_FULL_ACCESS = AUTHORITY_RANK.AdminAuthority;

/**
 * Resolves the operator principal via `GET /api/proxy/api/auth/me` (and on route changes + window focus)
 * so sidebar, mobile nav, and command palette share one structural authority rank.
 *
 * - **development-bypass:** uses the proxy’s server-side API key; `/me` reflects `DevelopmentBypass` dev role.
 * - **JWT + not signed in:** conservative **Read** rank without calling `/me`.
 * - **JWT + signed in:** bearer + `/me` for role claims.
 */
export function OperatorNavAuthorityProvider({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const [callerAuthorityRank, setCallerAuthorityRank] = useState(AUTHORITY_RANK.ReadAuthority);
  const [isAuthorityLoading, setIsAuthorityLoading] = useState(true);

  const refreshCallerAuthority = useCallback(async (): Promise<void> => {
    if (isJwtAuthMode() && !isLikelySignedIn()) {
      setCallerAuthorityRank(AUTHORITY_RANK.ReadAuthority);
      setIsAuthorityLoading(false);

      return;
    }

    setIsAuthorityLoading(true);

    try {
      await ensureAccessTokenFresh();

      const headers = new Headers({ Accept: "application/json" });
      const bearer = getAccessTokenForApi();

      if (bearer !== undefined && bearer !== null && bearer.trim().length > 0) {
        headers.set("Authorization", `Bearer ${bearer}`);
      }

      const init = mergeRegistrationScopeForProxy({
        cache: "no-store",
        credentials: "same-origin",
        headers,
      });
      const response = await fetch("/api/proxy/api/auth/me", init);

      if (!response.ok) {
        setCallerAuthorityRank(AUTHORITY_RANK.ReadAuthority);

        return;
      }

      const body = (await response.json()) as MeClaimsPayload;
      const rank = maxAuthorityRankFromMeClaims(body.claims ?? []);

      setCallerAuthorityRank(rank);
    } catch {
      setCallerAuthorityRank(AUTHORITY_RANK.ReadAuthority);
    } finally {
      setIsAuthorityLoading(false);
    }
  }, []);

  useEffect(() => {
    void refreshCallerAuthority();
  }, [pathname, refreshCallerAuthority]);

  useEffect(() => {
    const onFocus = (): void => {
      void refreshCallerAuthority();
    };

    window.addEventListener("focus", onFocus);

    return () => {
      window.removeEventListener("focus", onFocus);
    };
  }, [refreshCallerAuthority]);

  const value = useMemo<OperatorNavAuthorityContextValue>(
    () => ({ callerAuthorityRank, isAuthorityLoading }),
    [callerAuthorityRank, isAuthorityLoading],
  );

  return <OperatorNavAuthorityContext.Provider value={value}>{children}</OperatorNavAuthorityContext.Provider>;
}

/**
 * Returns the shared caller authority rank for nav filtering.
 * When used outside `OperatorNavAuthorityProvider` (e.g. unit tests), defaults to **Admin** rank so links stay visible.
 */
export function useOperatorNavAuthority(): OperatorNavAuthorityContextValue {
  const ctx = useContext(OperatorNavAuthorityContext);

  if (ctx === undefined) {
    return {
      callerAuthorityRank: DEFAULT_RANK_FULL_ACCESS,
      isAuthorityLoading: false,
    };
  }

  return ctx;
}

/**
 * Rank used for **filtering** nav links: while JWT `/me` is in flight for a signed-in session, stay conservative (Read)
 * so Operator-only destinations do not flash for Reader before claims resolve.
 */
export function useNavCallerAuthorityRank(): number {
  const { callerAuthorityRank, isAuthorityLoading } = useOperatorNavAuthority();

  if (isAuthorityLoading && isJwtAuthMode() && isLikelySignedIn()) {
    return AUTHORITY_RANK.ReadAuthority;
  }

  return callerAuthorityRank;
}

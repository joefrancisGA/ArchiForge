"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";

import { AUTH_MODE } from "@/lib/auth-config";
import { isJwtAuthMode } from "@/lib/oidc/config";
import { isLikelySignedIn } from "@/lib/oidc/session";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";

/** Session guard: after SaaS trial pre-seed, first visit to operator home lands on the welcome run detail. */
const SESSION_KEY = "archlucid_trial_welcome_home_redirect_v1";

type TrialStatusPayload = {
  trialWelcomeRunId?: string | null;
};

/**
 * One-time redirect from operator home (`/`) to `/reviews/{trialWelcomeRunId}` when the API exposes a pre-seeded
 * welcome run (self-service trial). Uses sessionStorage so returning to home does not loop.
 */
export function TrialWelcomeRunDeepLink() {
  const router = useRouter();

  useEffect(() => {
    if (AUTH_MODE !== "development-bypass" && isJwtAuthMode() && !isLikelySignedIn()) {
      return;
    }

    let cancelled = false;

    void (async () => {
      try {
        if (typeof window === "undefined") {
          return;
        }

        const res = await fetch(
          "/api/proxy/v1/tenant/trial-status",
          mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" } }),
        );

        if (!res.ok || cancelled) {
          return;
        }

        const json = (await res.json()) as TrialStatusPayload;
        const welcomeId = json.trialWelcomeRunId?.trim() ?? "";

        if (!welcomeId) {
          return;
        }

        const already = window.sessionStorage.getItem(SESSION_KEY);

        if (already === welcomeId) {
          return;
        }

        window.sessionStorage.setItem(SESSION_KEY, welcomeId);
        router.replace(`/reviews/${encodeURIComponent(welcomeId)}`);
      } catch {
        /* ignore */
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [router]);

  return null;
}

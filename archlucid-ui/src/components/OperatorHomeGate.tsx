"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState, type ReactNode } from "react";

import { AUTH_MODE } from "@/lib/auth-config";
import { isJwtAuthMode } from "@/lib/oidc/config";
import { isLikelySignedIn } from "@/lib/oidc/session";

function gateAllowsInitialPaint(): boolean {
  if (AUTH_MODE === "development-bypass" || !isJwtAuthMode())
    return true;

  if (typeof window === "undefined")
    return false;

  return isLikelySignedIn();
}

/**
 * When OIDC JWT mode is enabled and the browser has no session, the operator home (`/`)
 * redirects buyers to the public marketing welcome page.
 */
export function OperatorHomeGate({ children }: { children: ReactNode }) {
  const router = useRouter();
  const [allow, setAllow] = useState(gateAllowsInitialPaint);

  useEffect(() => {
    if (AUTH_MODE === "development-bypass" || !isJwtAuthMode()) {
      setAllow(true);

      return;
    }

    if (!isLikelySignedIn()) {
      router.replace("/welcome");

      return;
    }

    setAllow(true);
  }, [router]);

  if (!allow) {
    return (
      <main aria-busy="true">
        <p className="text-sm text-neutral-600 dark:text-neutral-400">Loading…</p>
      </main>
    );
  }

  return <>{children}</>;
}

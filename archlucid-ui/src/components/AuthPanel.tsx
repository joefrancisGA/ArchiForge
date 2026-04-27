"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

import { AUTH_MODE } from "@/lib/auth-config";
import { isJwtAuthMode } from "@/lib/oidc/config";
import {
  isLikelySignedIn,
  readSignedInDisplayName,
  signOutAndRedirectHome,
} from "@/lib/oidc/session";

/** Inline shell chrome (top bar): dev bypass notice, or OIDC sign-in / sign-out + display name. */
export function AuthPanel() {
  const [displayName, setDisplayName] = useState<string | null>(null);
  const [signedIn, setSignedIn] = useState(false);

  const refresh = useCallback(() => {
    if (!isJwtAuthMode()) {
      return;
    }

    setSignedIn(isLikelySignedIn());
    setDisplayName(readSignedInDisplayName());
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  useEffect(() => {
    const onFocus = (): void => {
      refresh();
    };

    window.addEventListener("focus", onFocus);

    return () => window.removeEventListener("focus", onFocus);
  }, [refresh]);

  // Amber strip is dev-local only — hide in production screenshots and omit from customer builds.
  if (AUTH_MODE === "development-bypass" || !isJwtAuthMode()) {
    if (process.env.NODE_ENV !== "development" || process.env.NEXT_PUBLIC_HIDE_ENV_BADGE === "true") {
      return null;
    }

    return (
      <div
        role="status"
        aria-label="Environment mode"
        className="inline-flex h-6 shrink-0 items-center gap-1.5 rounded-full border border-amber-300 bg-amber-50 px-2 py-0 text-[11px] font-semibold text-amber-800 dark:border-amber-700 dark:bg-amber-950/40 dark:text-amber-300"
      >
        <span className="h-1.5 w-1.5 shrink-0 rounded-full bg-amber-500" aria-hidden />
        Development workspace
      </div>
    );
  }

  return (
    <div
      role="region"
      aria-label="Authentication status"
      className="flex shrink-0 flex-wrap items-center gap-2 text-[11px] text-neutral-600 dark:text-neutral-400"
    >
      <span className="inline-flex h-6 items-center gap-1.5 rounded-full border border-neutral-200 bg-white px-2 py-0 font-medium text-neutral-700 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300">
        <span className={`h-1.5 w-1.5 shrink-0 rounded-full ${signedIn ? "bg-emerald-500" : "bg-neutral-400"}`} aria-hidden />
        {signedIn && displayName ? displayName : signedIn ? "Signed in" : "Not signed in"}
      </span>
      {!signedIn ? (
        <Link
          className="auth-panel-focus inline-flex h-6 items-center rounded-md bg-slate-900 px-2.5 py-0 text-[11px] font-medium text-white no-underline dark:bg-slate-800"
          href="/auth/signin"
          aria-label="Sign in with your organization account"
        >
          Sign in
        </Link>
      ) : (
        <button
          type="button"
          className="auth-panel-focus inline-flex h-6 items-center rounded-md border border-neutral-300 bg-white px-2 py-0 text-[11px] font-medium text-neutral-700 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-200"
          aria-label="Sign out and return to the operator home page"
          onClick={() => void signOutAndRedirectHome()}
        >
          Sign out
        </button>
      )}
    </div>
  );
}

"use client";

import { ChevronDown, ClipboardCheck, FileCheck2, Package, Target, X } from "lucide-react";
import Link from "next/link";
import { useEffect, useId, useState } from "react";
import type { CSSProperties } from "react";

import { OptInTourLauncher } from "@/components/tour/OptInTourLauncher";
import { Button } from "@/components/ui/button";
import { AUTH_MODE } from "@/lib/auth-config";
import { isJwtAuthMode } from "@/lib/oidc/config";
import { isLikelySignedIn } from "@/lib/oidc/session";
import { listRunsByProjectPaged } from "@/lib/api";
import { readHasExistingRunsCache, writeHasExistingRunsCache } from "@/lib/operator-run-presence";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";
import { cn } from "@/lib/utils";

const SESSION_DISMISS_KEY = "archlucid_welcome_dismissed_session";

type TrialStatusPayload = {
  status?: string;
  daysRemaining?: number | null;
};

/**
 * Operator-home welcome: trial badge from `GET /v1/tenant/trial-status` (defers until load); first-run vs returning
 * copy from a cached `archlucid_has_existing_runs` (instant) and `listRunsByProjectPaged` revalidation. Dismissal uses
 * sessionStorage so a new browser session can show the banner again.
 */
const DEFAULT_PROJECT_ID = "default";

const dotMaskStyle: CSSProperties = {
  WebkitMaskImage:
    "linear-gradient(to right, transparent 0%, transparent 35%, rgba(0,0,0,0.2) 55%, rgba(0,0,0,0.7) 100%)",
  maskImage: "linear-gradient(to right, transparent 0%, transparent 35%, rgba(0,0,0,0.2) 55%, rgba(0,0,0,0.7) 100%)",
};

export function WelcomeBanner() {
  const patternId = useId().replaceAll(":", "");
  const [dismissed, setDismissed] = useState(true);
  const [compact, setCompact] = useState(false);
  const [hydrated, setHydrated] = useState(false);
  const [trial, setTrial] = useState<TrialStatusPayload | null>(null);
  const [hasExistingRuns, setHasExistingRuns] = useState(false);

  useEffect(() => {
    try {
      if (typeof window !== "undefined" && window.sessionStorage.getItem(SESSION_DISMISS_KEY) === "1") {
        setDismissed(true);
        setCompact(false);
      } else {
        setDismissed(false);
        setCompact(false);
        setHasExistingRuns(readHasExistingRunsCache());
      }
    } catch {
      setDismissed(false);
      setHasExistingRuns(false);
    }

    setHydrated(true);
  }, []);

  useEffect(() => {
    if (!hydrated || dismissed) {
      return;
    }

    if (AUTH_MODE !== "development-bypass" && isJwtAuthMode() && !isLikelySignedIn()) {
      return;
    }

    let cancelled = false;

    void (async () => {
      try {
        const res = await fetch(
          "/api/proxy/v1/tenant/trial-status",
          mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" } }),
        );

        if (!cancelled && res.ok) {
          const json = (await res.json()) as TrialStatusPayload;
          setTrial(json);
        }
      } catch {
        /* ignore */
      }

      try {
        const page = await listRunsByProjectPaged(DEFAULT_PROJECT_ID, 1, 1);
        const next = (page.items?.length ?? 0) > 0;

        if (cancelled) {
          return;
        }

        setHasExistingRuns(next);
        writeHasExistingRunsCache(next);
      } catch {
        if (!cancelled) {
          setHasExistingRuns(false);
          writeHasExistingRunsCache(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [hydrated, dismissed]);

  if (!hydrated) {
    return null;
  }

  if (dismissed) {
    return null;
  }

  const trialActive = trial?.status === "Active";
  const days = trial?.daysRemaining;
  const returningUser = hasExistingRuns;
  const headingText = returningUser
    ? "Architecture manifest workspace"
    : "Turn architecture proposals into governed, evidence-backed review packages.";
  const subheadingText = returningUser
    ? "Monitor active runs, finalize manifests, and review governance findings."
    : "Turn architecture intent into a governed, reviewable manifest with supporting artifacts and findings.";

  const setWelcomeDismissed = (nextCompact: boolean) => {
    if (nextCompact) {
      setCompact(true);

      return;
    }

    setDismissed(true);

    try {
      window.sessionStorage.setItem(SESSION_DISMISS_KEY, "1");
    } catch {
      /* private mode */
    }
  };

  if (compact) {
    return (
      <div
        role="banner"
        aria-label={trialActive ? "Trial welcome (compact)" : "Welcome (compact)"}
        className="mb-2 flex flex-wrap items-center gap-2 rounded-lg border border-neutral-200 bg-white/95 px-3 py-2 text-sm shadow-sm dark:border-neutral-700 dark:bg-neutral-900/95"
      >
        {trialActive && typeof days === "number" ? (
          <span className="inline-block rounded-full border border-amber-300 bg-amber-100 px-2 py-0.5 text-[10px] font-semibold text-amber-800 dark:border-amber-700 dark:bg-amber-900/50 dark:text-amber-300">
            {days} day{days === 1 ? "" : "s"} left on trial
          </span>
        ) : null}
        <Button asChild size="sm" className="h-8" variant="primary">
          <Link href="/reviews/new">New review</Link>
        </Button>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="h-8 px-1 text-neutral-500"
          aria-label="Expand welcome banner"
          onClick={() => {
            setCompact(false);
          }}
        >
          <ChevronDown className="h-4 w-4 rotate-180" aria-hidden />
        </Button>
      </div>
    );
  }

  return (
    <div
      role="banner"
      aria-label={trialActive ? "Trial welcome" : "Welcome"}
      className={cn(
        "isolate relative mb-4 overflow-hidden rounded-xl border border-l-4 bg-gradient-to-br px-5 py-4 shadow-sm",
        trialActive
          ? "border-amber-200 border-l-amber-500 from-amber-50/80 to-white dark:border-amber-900 dark:border-l-amber-500 dark:from-amber-950/30 dark:to-neutral-900"
          : "border-teal-200 border-l-teal-600 from-teal-50/80 to-white dark:border-teal-900 dark:border-l-teal-500 dark:from-teal-950/30 dark:to-neutral-900",
      )}
    >
      <div className="absolute right-10 top-3 z-10 flex items-center gap-0.5">
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="h-7 text-xs text-neutral-500"
          onClick={() => {
            setWelcomeDismissed(true);
          }}
        >
          Minimize
        </Button>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="h-7 w-7 text-neutral-400 hover:text-neutral-700 dark:text-neutral-500 dark:hover:text-neutral-200"
          aria-label="Dismiss welcome for this session"
          onClick={() => {
            setWelcomeDismissed(false);
          }}
        >
          <X className="h-4 w-4" aria-hidden />
        </Button>
      </div>

      <div
        className="absolute inset-0 z-0 overflow-hidden rounded-xl opacity-20 mix-blend-multiply dark:opacity-15 dark:mix-blend-screen"
        style={dotMaskStyle}
        aria-hidden
      >
        <svg className="absolute left-0 top-0 h-full w-full" width="100%" height="100%" aria-hidden>
          <defs>
            <pattern id={patternId} x="0" y="0" width="24" height="24" patternUnits="userSpaceOnUse">
              <circle cx="2" cy="2" r="1" className="fill-teal-800 dark:fill-teal-100" />
            </pattern>
          </defs>
          <rect width="100%" height="100%" fill={`url(#${patternId})`} />
        </svg>
      </div>

      <div className="relative z-10 flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between lg:gap-6">
        <div className="min-w-0 flex-1 pr-8">
          {trialActive && typeof days === "number" ? (
            <span className="mb-2 inline-block rounded-full border border-amber-300 bg-amber-100 px-2.5 py-0.5 text-[11px] font-semibold text-amber-800 dark:border-amber-700 dark:bg-amber-900/50 dark:text-amber-300">
              {days} day{days === 1 ? "" : "s"} left on trial
            </span>
          ) : null}
          <h2 className="mb-1 pr-10 text-3xl font-bold leading-tight tracking-tight text-neutral-900 dark:text-neutral-100">
            {headingText}
          </h2>
          <p className="mt-0 max-w-lg text-sm text-neutral-600 dark:text-neutral-400">{subheadingText}</p>

          <div className="mt-4 flex flex-wrap items-center gap-2.5">
            <Button asChild variant="primary" className="h-10 px-6 text-base font-semibold shadow-sm">
              <Link href="/reviews/new">New review</Link>
            </Button>
            <Button
              asChild
              variant="outline"
              className="h-10 border-teal-300 px-5 text-sm font-semibold text-teal-800 hover:bg-teal-50 dark:border-teal-700 dark:text-teal-300 dark:hover:bg-teal-900/40"
            >
              <Link href="/showcase/claims-intake-modernization">See completed example</Link>
            </Button>
            <OptInTourLauncher className="h-10 px-4 text-sm" />
          </div>
        </div>

        {!returningUser ? (
          <div
            className="w-full shrink-0 rounded-lg border border-neutral-200/80 bg-white/80 px-4 py-3 text-sm shadow-sm backdrop-blur-sm dark:border-neutral-800/60 dark:bg-neutral-900/80 dark:backdrop-blur-sm lg:max-w-[17rem]"
            aria-label="What you will receive from a completed run"
          >
            <p className="m-0 mb-1.5 text-xs font-semibold text-neutral-800 dark:text-neutral-200">What you&apos;ll get</p>
            <ul className="m-0 mb-2 list-none space-y-1.5 p-0">
              {(
                [
                  { label: "Governed manifest" as const, Icon: FileCheck2 },
                  { label: "Actionable findings" as const, Icon: Target },
                  { label: "Exportable artifact bundle" as const, Icon: Package },
                  { label: "Review trail" as const, Icon: ClipboardCheck },
                ] as const
              ).map(({ label, Icon }) => (
                <li key={label} className="flex items-center gap-1.5 text-xs text-neutral-600 dark:text-neutral-400">
                  <Icon className="h-3.5 w-3.5 shrink-0 text-teal-600 dark:text-teal-400" aria-hidden />
                  {label}
                </li>
              ))}
            </ul>
            <p className="m-0 text-xs leading-relaxed text-neutral-600 dark:text-neutral-400">
              One request produces everything needed for review.
            </p>
          </div>
        ) : null}
      </div>
    </div>
  );
}

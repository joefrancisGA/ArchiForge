"use client";

import { AlertCircle, Archive, FileText, History, X } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";

import { OptInTourLauncher } from "@/components/tour/OptInTourLauncher";
import { Button } from "@/components/ui/button";
import { AUTH_MODE } from "@/lib/auth-config";
import { isJwtAuthMode } from "@/lib/oidc/config";
import { isLikelySignedIn } from "@/lib/oidc/session";
import { listRunsByProjectPaged } from "@/lib/api";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";
import { cn } from "@/lib/utils";

const STORAGE_KEY = "archlucid_welcome_dismissed";

type TrialStatusPayload = {
  status?: string;
  daysRemaining?: number | null;
};

/**
 * Operator-home welcome: trial-aware copy when `GET /v1/tenant/trial-status` reports an active self-service trial;
 * first-run vs returning-user hero copy from a lightweight runs page (`listRunsByProjectPaged`, page size 1).
 * Dismissal persists in localStorage.
 */
const DEFAULT_PROJECT_ID = "default";

export function WelcomeBanner() {
  const [dismissed, setDismissed] = useState(true);
  const [hydrated, setHydrated] = useState(false);
  const [trial, setTrial] = useState<TrialStatusPayload | null>(null);
  const [hasExistingRuns, setHasExistingRuns] = useState(false);

  useEffect(() => {
    try {
      const raw = typeof window !== "undefined" ? window.localStorage.getItem(STORAGE_KEY) : null;
      setDismissed(raw === "1");
    } catch {
      setDismissed(false);
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

        if (!cancelled) {
          setHasExistingRuns((page.items?.length ?? 0) > 0);
        }
      } catch {
        if (!cancelled) {
          setHasExistingRuns(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [hydrated, dismissed]);

  if (!hydrated || dismissed) {
    return null;
  }

  const trialActive = trial?.status === "Active";
  const days = trial?.daysRemaining;
  const returningUser = hasExistingRuns;
  const headingText = returningUser
    ? "Architecture manifest workspace"
    : "Generate your first architecture manifest";
  const subheadingText = returningUser
    ? "Monitor active runs, finalize manifests, and review governance findings."
    : "Turn architecture intent into a governed, reviewable manifest with supporting artifacts and findings.";
  const secondaryCtaLabel = returningUser ? "View runs" : "See completed example";

  return (
    <div
      role="banner"
      aria-label={trialActive ? "Trial welcome" : "Welcome"}
      className={cn(
        "relative mb-4 rounded-xl border bg-gradient-to-br px-5 py-4 shadow-sm",
        trialActive
          ? "isolate relative mb-4 rounded-xl border border-amber-200 border-l-4 border-l-amber-500 bg-gradient-to-br from-amber-50/80 to-white px-5 py-4 shadow-sm dark:border-amber-900 dark:border-l-amber-500 dark:from-amber-950/30 dark:to-neutral-900"
          : "isolate relative mb-4 rounded-xl border border-teal-200 border-l-4 border-l-teal-600 bg-gradient-to-br from-teal-50/80 to-white px-5 py-4 shadow-sm dark:border-teal-900 dark:border-l-teal-500 dark:from-teal-950/30 dark:to-neutral-900",
      )}
    >
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className="absolute right-3 top-3 z-10 h-7 w-7 text-neutral-400 hover:text-neutral-700 dark:text-neutral-500 dark:hover:text-neutral-200"
        aria-label="Dismiss welcome banner"
        onClick={() => {
          try {
            window.localStorage.setItem(STORAGE_KEY, "1");
          } catch {
            /* private mode */
          }

          setDismissed(true);
        }}
      >
        <X className="h-4 w-4" aria-hidden />
      </Button>

      <div className="absolute inset-0 z-0 overflow-hidden rounded-xl opacity-40 mix-blend-multiply dark:opacity-20 dark:mix-blend-screen" aria-hidden>
        <svg className="absolute left-0 top-0 h-full w-full" width="100%" height="100%">
          <defs>
            <pattern id="hero-dots" x="0" y="0" width="24" height="24" patternUnits="userSpaceOnUse">
              <circle cx="2" cy="2" r="1" className="fill-teal-800 dark:fill-teal-100" />
            </pattern>
          </defs>
          <rect width="100%" height="100%" fill="url(#hero-dots)" />
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
              <Link href="/runs/new">Create Request</Link>
            </Button>
            <Button
              asChild
              variant="outline"
              className="h-10 border-teal-300 px-5 text-sm font-semibold text-teal-800 hover:bg-teal-50 dark:border-teal-700 dark:text-teal-300 dark:hover:bg-teal-900/40"
            >
              <Link href="/runs?projectId=default">{secondaryCtaLabel}</Link>
            </Button>
            {trialActive ? (
              <Button asChild variant="outline" size="sm" className="h-8">
                <Link href="/getting-started?source=registration">Onboarding checklist</Link>
              </Button>
            ) : null}
            <OptInTourLauncher buttonVariant="ghost" className="h-8" />
          </div>
        </div>

        {!returningUser ? (
          <div
            className="w-full shrink-0 rounded-lg border border-neutral-200/80 bg-white/40 px-4 py-3 text-sm shadow-sm dark:border-neutral-800/60 dark:bg-neutral-900/30 lg:max-w-[17rem]"
            aria-label="Sample completed run output"
          >
            <p className="m-0 mb-2 text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
              Sample output includes
            </p>
            <ul className="m-0 list-none space-y-1.5 p-0 text-xs text-neutral-700 dark:text-neutral-300">
              <li className="flex items-center gap-2">
                <FileText className="h-4 w-4 shrink-0 text-teal-700 dark:text-teal-400" aria-hidden />
                <span>Architecture manifest</span>
              </li>
              <li className="flex items-center gap-2">
                <AlertCircle className="h-4 w-4 shrink-0 text-teal-700 dark:text-teal-400" aria-hidden />
                <span>Findings</span>
              </li>
              <li className="flex items-center gap-2">
                <Archive className="h-4 w-4 shrink-0 text-teal-700 dark:text-teal-400" aria-hidden />
                <span>Artifact bundle</span>
              </li>
              <li className="flex items-center gap-2">
                <History className="h-4 w-4 shrink-0 text-teal-700 dark:text-teal-400" aria-hidden />
                <span>Review trail</span>
              </li>
            </ul>
          </div>
        ) : null}
      </div>
    </div>
  );
}

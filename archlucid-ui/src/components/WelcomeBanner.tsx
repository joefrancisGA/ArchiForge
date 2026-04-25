"use client";

import { X } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { AUTH_MODE } from "@/lib/auth-config";
import { isJwtAuthMode } from "@/lib/oidc/config";
import { isLikelySignedIn } from "@/lib/oidc/session";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";
import { cn } from "@/lib/utils";

const STORAGE_KEY = "archlucid_welcome_dismissed";

type TrialStatusPayload = {
  status?: string;
  daysRemaining?: number | null;
};

/**
 * Operator-home welcome: trial-aware copy when `GET /v1/tenant/trial-status` reports an active self-service trial;
 * otherwise the original first-run guidance. Dismissal persists in localStorage.
 */
export function WelcomeBanner() {
  const [dismissed, setDismissed] = useState(true);
  const [hydrated, setHydrated] = useState(false);
  const [trial, setTrial] = useState<TrialStatusPayload | null>(null);

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

        if (!res.ok || cancelled) {
          return;
        }

        const json = (await res.json()) as TrialStatusPayload;

        if (!cancelled) {
          setTrial(json);
        }
      } catch {
        /* ignore */
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

  return (
    <div
      role="banner"
      aria-label={trialActive ? "Trial welcome" : "Welcome"}
      className={cn(
        "relative mb-4 max-w-3xl rounded-lg border border-neutral-200 bg-white p-4 pl-5 shadow-sm dark:border-neutral-700 dark:bg-neutral-900",
        trialActive ? "border-l-4 border-l-amber-600 dark:border-l-amber-500" : "border-l-4 border-l-teal-700 dark:border-l-teal-500",
      )}
    >
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className="absolute right-2 top-2 h-8 w-8 text-neutral-500 hover:text-neutral-900 dark:text-neutral-400 dark:hover:text-neutral-100"
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
      <h2 className="pr-10 text-lg font-semibold text-neutral-900 dark:text-neutral-100">
        {trialActive ? "Your trial workspace is ready" : "Welcome to ArchLucid"}
      </h2>
      <p className="mt-2 max-w-2xl text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
        {trialActive ? (
          <>
            {typeof days === "number" ? (
              <>
                You have <strong>{days}</strong> calendar day{days === 1 ? "" : "s"} left on the self-service trial.
                Explore the seeded sample run from{" "}
                <Link href="/getting-started?source=registration" className="text-teal-800 underline dark:text-teal-300">
                  onboarding
                </Link>{" "}
                or start a fresh architecture run.
              </>
            ) : (
              <>
                Explore the guided{" "}
                <Link href="/getting-started?source=registration" className="text-teal-800 underline dark:text-teal-300">
                  onboarding checklist
                </Link>{" "}
                or jump into the wizard when you are ready.
              </>
            )}
          </>
        ) : (
          <>
            Start by creating a run with the guided wizard. The pipeline will produce manifests and artifacts you can
            review on the run detail page. When you are ready, compare runs or explore governance from the sidebar.
          </>
        )}
      </p>
      <div className="mt-4 flex flex-wrap gap-3">
        <Button
          asChild
          className="bg-teal-700 text-white hover:bg-teal-800 dark:bg-teal-800 dark:text-white dark:hover:bg-teal-700"
        >
          <Link href="/runs/new">Create your first run</Link>
        </Button>
        <Button asChild variant="outline">
          <Link href="/runs?projectId=default">Explore demo data</Link>
        </Button>
        {trialActive ? (
          <Button asChild variant="outline">
            <Link href="/getting-started?source=registration">Open onboarding</Link>
          </Button>
        ) : null}
      </div>
    </div>
  );
}

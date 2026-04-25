"use client";

import { useEffect, useRef, useState } from "react";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
import { downloadFirstValueReportPdf } from "@/lib/api";
import type { ApiProblemDetails } from "@/lib/api-problem";
import { AUTH_MODE } from "@/lib/auth-config";
import { isApiRequestError } from "@/lib/api-request-error";
import { isJwtAuthMode } from "@/lib/oidc/config";
import { isLikelySignedIn } from "@/lib/oidc/session";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";
import { recordSponsorBannerFirstCommitBadge } from "@/lib/sponsor-banner-telemetry";

export type EmailRunToSponsorBannerProps = {
  runId: string;
};

type TrialStatusPayload = {
  firstCommitUtc?: string | null;
};

function computeUtcDayN(firstCommitIso: string, nowMs: number): number | null {
  const commitMs = new Date(firstCommitIso).getTime();

  if (Number.isNaN(commitMs)) {
    return null;
  }

  const msPerDay = 24 * 60 * 60 * 1000;

  return Math.max(0, Math.floor((nowMs - commitMs) / msPerDay));
}

/**
 * Non-modal post-commit CTA: downloads a sponsor-shareable PDF projection of the canonical first-value-report
 * Markdown for this run, and prompts the operator to email it. Only rendered when the run has a golden manifest.
 *
 * The primary action calls `POST /v1/pilots/runs/{runId}/first-value-report.pdf` (Read access on the API; mirrors
 * the auth surface of the existing Markdown sibling so click-to-download is one-shot).
 */
export function EmailRunToSponsorBanner({ runId }: EmailRunToSponsorBannerProps) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<{
    message: string;
    problem: ApiProblemDetails | null;
    correlationId: string | null;
  } | null>(null);
  const [badgeDayN, setBadgeDayN] = useState<number | null>(null);
  const telemetrySentRef = useRef(false);

  useEffect(() => {
    let cancelled = false;

    async function loadTrialStatus(): Promise<void> {
      if (AUTH_MODE !== "development-bypass" && isJwtAuthMode() && !isLikelySignedIn()) {
        return;
      }

      try {
        const res = await fetch(
          "/api/proxy/v1/tenant/trial-status",
          mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" } }),
        );

        if (!res.ok) {
          return;
        }

        const json = (await res.json()) as TrialStatusPayload;

        if (cancelled) {
          return;
        }

        const iso = json.firstCommitUtc;

        if (typeof iso !== "string" || iso.length === 0) {
          setBadgeDayN(null);

          return;
        }

        const n = computeUtcDayN(iso, Date.now());

        if (n === null) {
          setBadgeDayN(null);

          return;
        }

        setBadgeDayN(n);
      } catch {
        /* graceful: banner without badge */
      }
    }

    void loadTrialStatus();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (badgeDayN === null) {
      return;
    }

    if (telemetrySentRef.current) {
      return;
    }

    telemetrySentRef.current = true;
    recordSponsorBannerFirstCommitBadge(badgeDayN);
  }, [badgeDayN]);

  async function onDownload(): Promise<void> {
    setBusy(true);
    setError(null);

    try {
      await downloadFirstValueReportPdf(runId);
    } catch (e: unknown) {
      if (isApiRequestError(e)) {
        setError({
          message: e.message,
          problem: e.problem,
          correlationId: e.correlationId,
        });
      } else {
        setError({
          message: e instanceof Error ? e.message : "Could not generate sponsor PDF.",
          problem: null,
          correlationId: null,
        });
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <aside
      data-testid="email-run-to-sponsor-banner"
      role="region"
      aria-label="Email this run to your sponsor"
      className="mb-6 max-w-3xl rounded-md border border-teal-300 bg-teal-50 px-4 py-3 dark:border-teal-700 dark:bg-teal-950/40"
    >
      <p className="m-0 flex flex-wrap items-center text-[11px] font-semibold uppercase tracking-wide text-teal-800 dark:text-teal-300">
        <span>Time to value</span>
        {badgeDayN !== null ? (
          <span
            data-testid="email-run-to-sponsor-first-commit-badge"
            title="UTC days since your tenant's first committed golden manifest"
            aria-label={`Day ${badgeDayN} since your tenant's first committed golden manifest`}
            className="ml-2 inline-flex items-center rounded-full bg-teal-100 px-2 py-0.5 text-[11px] font-medium text-teal-900 dark:bg-teal-900 dark:text-teal-100"
          >
            Day {badgeDayN} since first commit
          </span>
        ) : null}
      </p>
      <p className="m-0 mt-1 text-sm text-neutral-800 dark:text-neutral-100">
        This run is committed. Send your sponsor a one-page PDF derived from the first-value-report Markdown so they
        can see the headline timing, findings counts, and decision-trace summary without opening the operator shell.
      </p>
      <div className="mt-3 flex flex-wrap items-center gap-3">
        <Button
          type="button"
          variant="default"
          className="bg-teal-700 text-white hover:bg-teal-800 dark:bg-teal-600 dark:hover:bg-teal-500"
          disabled={busy}
          onClick={() => void onDownload()}
          data-testid="email-run-to-sponsor-primary-action"
        >
          {busy ? "Preparing PDF…" : "Email this run to your sponsor"}
        </Button>
        <span className="text-xs text-neutral-600 dark:text-neutral-400">
          Downloads <code>first-value-report-{runId}.pdf</code> — attach to your email of choice.
        </span>
      </div>
      {error !== null ? (
        <div className="mt-2">
          <OperatorApiProblem
            problem={error.problem}
            fallbackMessage={error.message}
            correlationId={error.correlationId}
            variant="warning"
          />
        </div>
      ) : null}
    </aside>
  );
}

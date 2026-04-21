"use client";

import { useState } from "react";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
import { downloadFirstValueReportPdf } from "@/lib/api";
import type { ApiProblemDetails } from "@/lib/api-problem";
import { isApiRequestError } from "@/lib/api-request-error";

export type EmailRunToSponsorBannerProps = {
  runId: string;
};

/**
 * Non-modal post-commit CTA: downloads a sponsor-shareable PDF projection of the canonical first-value-report
 * Markdown for this run, and prompts the operator to email it. Only rendered when the run has a golden manifest.
 *
 * The primary action calls `POST /v1/pilots/runs/{runId}/first-value-report.pdf` (ReadAuthority on API; mirrors
 * the auth surface of the existing Markdown sibling so click-to-download is one-shot).
 */
export function EmailRunToSponsorBanner({ runId }: EmailRunToSponsorBannerProps) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<{
    message: string;
    problem: ApiProblemDetails | null;
    correlationId: string | null;
  } | null>(null);

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
      <p className="m-0 text-[11px] font-semibold uppercase tracking-wide text-teal-800 dark:text-teal-300">
        Time to value
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

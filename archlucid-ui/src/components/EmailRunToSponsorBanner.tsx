"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
import {
  downloadFirstValueReportPdf,
  getArchitecturePackageDocxUrl,
  getBundleDownloadUrl,
  getRunExportDownloadUrl,
} from "@/lib/api";
import type { ApiProblemDetails } from "@/lib/api-problem";
import { isApiRequestError } from "@/lib/api-request-error";
import { AUTH_MODE } from "@/lib/auth-config";
import { DEFAULT_GITHUB_BLOB_BASE } from "@/lib/docs-public-base";
import { isJwtAuthMode } from "@/lib/oidc/config";
import { isLikelySignedIn } from "@/lib/oidc/session";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";
import { recordSponsorBannerFirstCommitBadge } from "@/lib/sponsor-banner-telemetry";

export type EmailRunToSponsorBannerProps = {
  runId: string;
  manifestId: string;
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
 * Post-commit pilot ROI hub: primary PDF download (canonical sponsor projection) plus links to existing Markdown,
 * architecture DOCX, ZIP exports, and the in-product scorecard — no duplicate generation logic on the client.
 *
 * Render only when the server has confirmed a **Committed** manifest summary (see `runs/[runId]/page.tsx`).
 */
export function EmailRunToSponsorBanner({ runId, manifestId }: EmailRunToSponsorBannerProps) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<{
    message: string;
    problem: ApiProblemDetails | null;
    correlationId: string | null;
  } | null>(null);
  const [badgeDayN, setBadgeDayN] = useState<number | null>(null);
  const telemetrySentRef = useRef(false);

  const markdownHref = `/api/proxy/v1/pilots/runs/${encodeURIComponent(runId)}/first-value-report`;
  const executiveBriefHref = `${DEFAULT_GITHUB_BLOB_BASE}/docs/EXECUTIVE_SPONSOR_BRIEF.md`;
  const pilotRoiModelHref = `${DEFAULT_GITHUB_BLOB_BASE}/docs/library/PILOT_ROI_MODEL.md`;

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

  async function onDownloadPdf(): Promise<void> {
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
      id="pilot-scorecard-package"
      data-testid="email-run-to-sponsor-banner"
      role="region"
      aria-label="Pilot scorecard package"
      className="mb-6 max-w-3xl rounded-md border border-teal-300 bg-teal-50 px-4 py-3 dark:border-teal-700 dark:bg-teal-950/40"
    >
      <p className="m-0 flex flex-wrap items-center text-[11px] font-semibold uppercase tracking-wide text-teal-800 dark:text-teal-300">
        <span>Time to value</span>
        {badgeDayN !== null ? (
          <span
            data-testid="email-run-to-sponsor-first-commit-badge"
            title="UTC days since your tenant's first finalized manifest"
            aria-label={`Day ${badgeDayN} since your tenant's first finalized manifest`}
            className="ml-2 inline-flex items-center rounded-full bg-teal-100 px-2 py-0.5 text-[11px] font-medium text-teal-900 dark:bg-teal-900 dark:text-teal-100"
          >
            Day {badgeDayN} since first finalization
          </span>
        ) : null}
      </p>

      <h2 className="m-0 mt-2 text-base font-semibold text-neutral-900 dark:text-neutral-50">
        Generate pilot scorecard package
      </h2>

      <p className="m-0 mt-2 text-sm leading-relaxed text-neutral-800 dark:text-neutral-100">
        Sponsor narrative aligns with{" "}
        <a
          className="font-medium text-teal-800 underline underline-offset-2 dark:text-teal-300"
          href={executiveBriefHref}
          rel="noopener noreferrer"
          target="_blank"
        >
          EXECUTIVE_SPONSOR_BRIEF.md
        </a>
        ; headline timing and conservative ROI framing follow{" "}
        <a
          className="font-medium text-teal-800 underline underline-offset-2 dark:text-teal-300"
          href={pilotRoiModelHref}
          rel="noopener noreferrer"
          target="_blank"
        >
          PILOT_ROI_MODEL.md
        </a>
        . Exports below reuse existing API routes — the hosted API remains authoritative for entitlements.
      </p>

      <div className="mt-3 flex flex-wrap items-center gap-3">
        <Button
          type="button"
          variant="primary"
          disabled={busy}
          onClick={() => void onDownloadPdf()}
          data-testid="email-run-to-sponsor-primary-action"
        >
          {busy ? "Preparing PDF…" : "Generate pilot scorecard package"}
        </Button>
        <span className="text-xs text-neutral-600 dark:text-neutral-400">
          Step 1: sponsor one‑pager PDF (<code className="text-[0.7rem]">POST …/first-value-report.pdf</code>) — same
          projection as the Markdown report.
        </span>
      </div>

      <ul className="m-0 mt-3 list-none space-y-1.5 p-0 text-xs text-neutral-700 dark:text-neutral-300">
        <li>
          <a
            className="font-medium text-teal-800 underline underline-offset-2 dark:text-teal-300"
            href={markdownHref}
            download={`archlucid-first-value-report-${runId}.md`}
          >
            First-value report (Markdown)
          </a>{" "}
          — <code className="text-[0.7rem]">GET …/first-value-report</code>
        </li>
        <li>
          <a
            className="font-medium text-teal-800 underline underline-offset-2 dark:text-teal-300"
            href={getArchitecturePackageDocxUrl(runId)}
          >
            Architecture package (DOCX)
          </a>{" "}
          — <code className="text-[0.7rem]">GET …/docx/runs/{runId}/architecture-package</code>
        </li>
        <li>
          <a
            className="font-medium text-teal-800 underline underline-offset-2 dark:text-teal-300"
            href={getBundleDownloadUrl(manifestId)}
          >
            Manifest bundle (ZIP)
          </a>
          {" · "}
          <a
            className="font-medium text-teal-800 underline underline-offset-2 dark:text-teal-300"
            href={getRunExportDownloadUrl(runId)}
          >
            Run export (ZIP)
          </a>
          {" · "}
          <Link className="font-medium text-teal-800 underline underline-offset-2 dark:text-teal-300" href="/scorecard">
            In-product pilot scorecard
          </Link>
          {" · "}
          <a className="font-medium text-teal-800 underline underline-offset-2 dark:text-teal-300" href="#artifacts-exports">
            Artifacts &amp; exports section
          </a>
        </li>
      </ul>

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

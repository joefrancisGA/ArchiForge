"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

import { LayerHeader } from "@/components/LayerHeader";

const DOCS_REPO_BASE =
  process.env.NEXT_PUBLIC_ARCHLUCID_DOCS_REPO_BASE ??
  "https://github.com/joefrancisGA/ArchLucid/blob/main";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import type { AuditEvent } from "@/lib/api";
import { searchAuditEvents } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";

/** Matches <see cref="ArchLucid.Core.Audit.AuditEventTypes.SecurityAssessmentPublished" /> (server catalog). */
const SECURITY_ASSESSMENT_PUBLISHED = "SecurityAssessmentPublished";

type PublicationPayload = {
  assessmentCode?: string;
  summaryReference?: string;
  assessorDisplayName?: string;
};

function parsePayload(dataJson: string): PublicationPayload {
  try {
    return JSON.parse(dataJson) as PublicationPayload;
  } catch {
    return {};
  }
}

/**
 * Trust and security home: procurement-oriented strip plus link to the latest published assessment audit signal.
 */
export default function SecurityTrustPage() {
  const [latest, setLatest] = useState<AuditEvent | null>(null);
  const [loadError, setLoadError] = useState<ApiLoadFailureState | null>(null);

  const refresh = useCallback(async () => {
    setLoadError(null);

    try {
      const rows = await searchAuditEvents({
        eventType: SECURITY_ASSESSMENT_PUBLISHED,
        take: 1,
      });

      setLatest(rows[0] ?? null);
    } catch (err) {
      setLoadError(toApiLoadFailure(err));
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const payload = latest ? parsePayload(latest.dataJson) : {};
  const summaryHref =
    payload.summaryReference && payload.summaryReference.startsWith("http")
      ? payload.summaryReference
      : `${DOCS_REPO_BASE}/docs/security/pen-test-summaries/2026-Q2-REDACTED-SUMMARY.md`;

  return (
    <div className="space-y-6">
      <LayerHeader pageKey="security-trust" />

      <section
        aria-label="Published security assessment"
        className="rounded-lg border border-emerald-200 bg-emerald-50/80 px-4 py-3 dark:border-emerald-900 dark:bg-emerald-950/40"
      >
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <p className="m-0 text-sm font-semibold text-emerald-900 dark:text-emerald-100">
              Third-party assessment publication
            </p>
            <p className="m-0 mt-1 text-sm text-emerald-900/90 dark:text-emerald-100/90">
              When security publishes a redacted summary, a durable audit event drives this badge and deep link.
            </p>
          </div>
          {latest ? (
            <Link
              href={summaryHref}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center rounded-full bg-emerald-700 px-3 py-1 text-xs font-semibold text-white hover:bg-emerald-800 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-600"
            >
              Latest: {payload.assessmentCode ?? "published"} — open summary
            </Link>
          ) : (
            <span className="inline-flex items-center rounded-full bg-neutral-200 px-3 py-1 text-xs font-semibold text-neutral-700 dark:bg-neutral-800 dark:text-neutral-200">
              No publication audit yet
            </span>
          )}
        </div>
        {latest ? (
          <p className="mt-2 mb-0 text-xs text-emerald-900/80 dark:text-emerald-200/80">
            Recorded {new Date(latest.occurredUtc).toLocaleString()} — assessor:{" "}
            {payload.assessorDisplayName ?? "see summary"}.
          </p>
        ) : null}
      </section>

      {loadError ? <OperatorApiProblem failure={loadError} /> : null}

      <section className="space-y-2">
        <h2 className="text-lg font-semibold">Repository trust center</h2>
        <p className="m-0 text-sm text-neutral-700 dark:text-neutral-300">
          Buyer-facing index (policies, DPA template, subprocessors, SOC 2 self-assessment, CAIQ / SIG): open the{" "}
          <Link
            className="text-sky-700 underline underline-offset-2 hover:text-sky-900 dark:text-sky-400 dark:hover:text-sky-200"
            href={`${DOCS_REPO_BASE}/docs/go-to-market/TRUST_CENTER.md`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Trust Center (markdown)
          </Link>
          .
        </p>
      </section>
    </div>
  );
}

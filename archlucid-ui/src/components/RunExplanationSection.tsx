"use client";

import { useMemo } from "react";

import { CitationChips } from "@/components/explanation/CitationChips";
import { DocumentLayout, type DocumentTocItem } from "@/components/DocumentLayout";
import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";
import { Progress } from "@/components/ui/progress";
import type { ExplanationResult, RunExplanationSummary } from "@/types/explanation";

export type RunExplanationSectionProps = {
  summary: RunExplanationSummary | null;
  loading: boolean;
  error: string | null;
  runId: string;
};

const badgeShell = "inline-block rounded-md border px-2.5 py-1 text-[13px] font-semibold";

/** Tailwind mapping for faithfulness support ratio (0–100). */
export function faithfulnessBadgeClass(pct: number): string {
  if (pct >= 80 - 1e-9) {
    return `${badgeShell} border-emerald-300 bg-emerald-100 text-emerald-950 dark:border-emerald-800 dark:bg-emerald-950/50 dark:text-emerald-100`;
  }

  if (pct >= 50 - 1e-9) {
    return `${badgeShell} border-amber-300 bg-amber-100 text-amber-950 dark:border-amber-800 dark:bg-amber-950/50 dark:text-amber-50`;
  }

  return `${badgeShell} border-rose-300 bg-rose-100 text-rose-950 dark:border-rose-800 dark:bg-rose-950/50 dark:text-rose-50`;
}

/** Tailwind mapping for risk posture label (case-insensitive). */
export function riskPostureBadgeClass(posture: string): string {
  const key = posture.trim().toLowerCase();

  if (key === "critical") {
    return `${badgeShell} border-rose-300 bg-rose-100 text-rose-950 dark:border-rose-800 dark:bg-rose-950/60 dark:text-rose-100`;
  }

  if (key === "high") {
    return `${badgeShell} border-orange-300 bg-orange-100 text-orange-950 dark:border-orange-800 dark:bg-orange-950/50 dark:text-orange-50`;
  }

  if (key === "medium") {
    return `${badgeShell} border-amber-300 bg-amber-100 text-amber-950 dark:border-amber-800 dark:bg-amber-950/50 dark:text-amber-50`;
  }

  return `${badgeShell} border-emerald-300 bg-emerald-100 text-emerald-950 dark:border-emerald-800 dark:bg-emerald-950/50 dark:text-emerald-50`;
}

/**
 * @deprecated Use {@link riskPostureBadgeClass} for Tailwind; kept for tests that assert legacy hex palette.
 */
export function riskPostureBadgeColors(posture: string): { background: string; color: string; borderColor: string } {
  const key = posture.trim().toLowerCase();

  if (key === "critical") {
    return { background: "#fee2e2", color: "#991b1b", borderColor: "#fecaca" };
  }

  if (key === "high") {
    return { background: "#ffedd5", color: "#c2410c", borderColor: "#fed7aa" };
  }

  if (key === "medium") {
    return { background: "#fef3c7", color: "#92400e", borderColor: "#fde68a" };
  }

  return { background: "#dcfce7", color: "#166534", borderColor: "#bbf7d0" };
}

function confidencePercent(confidence: number): number {
  if (!Number.isFinite(confidence)) {
    return 0;
  }

  const pct = confidence <= 1 ? Math.round(confidence * 100) : Math.round(confidence);

  return Math.min(100, Math.max(0, pct));
}

/** API payloads sometimes omit `explanation`; avoid crashing the review detail client subtree. */
function explanationBody(summary: RunExplanationSummary): ExplanationResult {
  const raw = summary.explanation;

  if (raw === null || raw === undefined) {
    return {
      rawText: "",
      structured: null,
      confidence: null,
      provenance: null,
      summary: "",
      keyDrivers: [],
      riskImplications: [],
      costImplications: [],
      complianceImplications: [],
      detailedNarrative: "",
    };
  }

  return raw;
}

/**
 * Run-level aggregate explanation: assessment, posture, confidence, themes, drivers/risks, provenance.
 */
export function RunExplanationSection({ summary, loading, error, runId }: RunExplanationSectionProps) {
  const tocItems = useMemo((): DocumentTocItem[] => {
    if (summary === null) {
      return [];
    }

    const expl = explanationBody(summary);
    const items: DocumentTocItem[] = [{ id: "doc-explanation-assessment", label: "Assessment" }];
    const traces = summary.findingTraceConfidences;

    if (traces !== null && traces !== undefined && traces.length > 0) {
      items.push({ id: "doc-explanation-traces", label: "Finding trace confidence" });
    }

    items.push(
      { id: "doc-explanation-confidence", label: "Model confidence" },
      { id: "doc-explanation-themes", label: "Themes" },
      { id: "doc-explanation-drivers", label: "Key drivers" },
      { id: "doc-explanation-risks", label: "Risk implications" },
    );

    if (expl.provenance !== null && expl.provenance !== undefined) {
      items.push({ id: "doc-explanation-provenance", label: "LLM provenance" });
    }

    return items;
  }, [summary]);

  if (loading) {
    return (
      <div aria-busy="true">
        <OperatorLoadingNotice>Loading explanation…</OperatorLoadingNotice>
      </div>
    );
  }

  if (error) {
    return <p role="alert" className="m-0 text-sm text-red-700 dark:text-red-300">{error}</p>;
  }

  if (!summary) {
    return null;
  }

  const expl = explanationBody(summary);
  const themeSummaries = summary.themeSummaries ?? [];
  const overallAssessment = summary.overallAssessment?.trim() ?? "Assessment details are not available for this review.";
  const riskPostureLabel = summary.riskPosture?.trim().length > 0 ? summary.riskPosture : "Not rated";
  const postureClass = riskPostureBadgeClass(riskPostureLabel);
  const conf = expl.confidence;
  const pct = conf !== null && conf !== undefined ? confidencePercent(conf) : null;
  const prov = expl.provenance;
  const faith = summary.faithfulnessSupportRatio;
  let faithPct: number | null = null;
  let faithClass: string | null = null;

  if (faith !== null && faith !== undefined && Number.isFinite(faith)) {
    faithPct = Math.round(faith * 100);
    faithClass = faithfulnessBadgeClass(faithPct);
  }

  return (
    <DocumentLayout tocItems={tocItems}>
      <p id="doc-explanation-assessment" className="m-0 text-xl font-bold leading-snug text-neutral-900 dark:text-neutral-50">
        {overallAssessment}
      </p>

      <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
        <span className="sr-only">Risk posture:</span>
        <span
          role="status"
          aria-label={`Risk posture ${riskPostureLabel}`}
          data-risk-posture={riskPostureLabel.trim().toLowerCase()}
          className={postureClass}
        >
          {riskPostureLabel}
        </span>
        <span className="ml-3 text-[13px] text-neutral-500 dark:text-neutral-400">
          {summary.decisionCount} decisions · {summary.findingCount} findings · {summary.unresolvedIssueCount}{" "}
          unresolved · {summary.complianceGapCount} compliance gaps
        </span>
      </p>

      {faithPct !== null && faithClass !== null ? (
        <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
          <span className="sr-only">Faithfulness vs findings:</span>
          <span role="status" aria-label={`Faithfulness support ratio ${faithPct} percent`} className={faithClass}>
            Faithfulness {faithPct}%
          </span>
          <span className="ml-2.5 text-xs text-neutral-500 dark:text-neutral-400">
            (token overlap vs finding traces — see docs)
          </span>
        </p>
      ) : null}

      {summary.usedDeterministicFallback === true ? (
        <p
          role="status"
          className="m-0 rounded-md border border-yellow-300 bg-yellow-50 p-3 text-sm leading-relaxed text-yellow-950 dark:border-yellow-800 dark:bg-yellow-950/40 dark:text-yellow-50"
        >
          This explanation was generated from manifest structure because AI-generated text did not sufficiently match
          the underlying findings.
        </p>
      ) : null}

      {summary.faithfulnessWarning && summary.usedDeterministicFallback !== true ? (
        <p
          role="status"
          className="m-0 rounded-md border border-orange-300 bg-orange-50 p-3 text-sm leading-relaxed text-orange-950 dark:border-orange-800 dark:bg-orange-950/40 dark:text-orange-50"
        >
          {summary.faithfulnessWarning}
        </p>
      ) : null}

      <CitationChips citations={summary.citations ?? []} runId={runId} />

      {summary.findingTraceConfidences && summary.findingTraceConfidences.length > 0 ? (
        <div className="mb-4">
          <h3 id="doc-explanation-traces" className="m-0 mb-2 text-lg font-semibold text-neutral-900 dark:text-neutral-100">
            Finding trace confidence
          </h3>
          <ul className="m-0 list-disc space-y-1 pl-5 text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
            {summary.findingTraceConfidences.map((row) => (
              <li key={row.findingId}>
                <code className="rounded bg-neutral-100 px-1 text-xs dark:bg-neutral-800">{row.findingId}</code> —{" "}
                {row.traceConfidenceLabel} ({Math.round(row.traceCompletenessRatio * 100)}% trace fields)
                {row.ruleId && row.ruleId.trim().length > 0 ? `; rule ${row.ruleId}` : ""}
                {typeof row.evidenceRefCount === "number" && Number.isFinite(row.evidenceRefCount)
                  ? `; ${row.evidenceRefCount} evidence ref(s)`
                  : ""}
                {row.missingTraceFields !== null &&
                row.missingTraceFields !== undefined &&
                row.missingTraceFields.length > 0 ? (
                  <span className="text-neutral-500 dark:text-neutral-400">
                    {" "}
                    — missing: {row.missingTraceFields.join(", ")}
                  </span>
                ) : null}
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      <div id="doc-explanation-confidence" className="mb-4">
        <p id="doc-explanation-confidence-label" className="m-0 mb-1.5 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
          Model confidence
        </p>
        {pct === null ? (
          <p role="status" className="m-0 text-sm text-neutral-500 dark:text-neutral-400">
            Not available
          </p>
        ) : (
          <>
            <Progress
              value={pct}
              aria-valuemin={0}
              aria-valuemax={100}
              aria-valuenow={pct}
              aria-labelledby="doc-explanation-confidence-label"
            />
            <p className="m-0 mt-1.5 text-[13px] text-neutral-500 dark:text-neutral-400">{pct}%</p>
          </>
        )}
      </div>

      <div className="mb-4">
        <h3 id="doc-explanation-themes" className="m-0 mb-2 text-lg font-semibold text-neutral-900 dark:text-neutral-100">
          Themes
        </h3>
        <ul className="m-0 list-disc space-y-1 pl-5 text-base leading-relaxed">
          {themeSummaries.map((t) => (
            <li key={t}>{t}</li>
          ))}
        </ul>
      </div>

      <div className="mb-4">
        <h3 id="doc-explanation-drivers" className="m-0 mb-2 text-lg font-semibold text-neutral-900 dark:text-neutral-100">
          Key drivers
        </h3>
        <ul className="m-0 list-disc space-y-1 pl-5 text-base leading-relaxed">
          {(expl.keyDrivers ?? []).map((d) => (
            <li key={d}>{d}</li>
          ))}
        </ul>
      </div>

      <div className="mb-4">
        <h3 id="doc-explanation-risks" className="m-0 mb-2 text-lg font-semibold text-neutral-900 dark:text-neutral-100">
          Risk implications
        </h3>
        <ul className="m-0 list-disc space-y-1 pl-5 text-base leading-relaxed">
          {(expl.riskImplications ?? []).map((r) => (
            <li key={r}>{r}</li>
          ))}
        </ul>
      </div>

      {prov ? (
        <details id="doc-explanation-provenance" className="text-sm text-neutral-700 dark:text-neutral-300">
          <summary className="cursor-pointer font-semibold text-neutral-900 dark:text-neutral-100">Provenance metadata</summary>
          <dl className="m-0 mt-3 grid grid-cols-[auto_1fr] gap-x-4 gap-y-1.5">
            <dt>Agent type</dt>
            <dd className="m-0">{prov.agentType}</dd>
            <dt>Model ID</dt>
            <dd className="m-0">{prov.modelId}</dd>
            <dt>Prompt template</dt>
            <dd className="m-0">{prov.promptTemplateId ?? "—"}</dd>
            <dt>Prompt version</dt>
            <dd className="m-0">{prov.promptTemplateVersion ?? "—"}</dd>
            <dt>Content hash</dt>
            <dd className="m-0">{prov.promptContentHash ?? "—"}</dd>
          </dl>
        </details>
      ) : null}
    </DocumentLayout>
  );
}

import type { ReactNode } from "react";

import { OperatorEmptyState } from "@/components/OperatorShellMessage";
import { getArchitecturePackageDocxUrl } from "@/lib/api";
import { compareRunHeadingLabel } from "@/lib/compare-run-display";
import { sortGoldenManifestComparison } from "@/lib/compare-display-sort";
import type { GoldenManifestComparison } from "@/types/comparison";

const cellCls = "border border-neutral-200 px-2.5 py-2 text-left align-top dark:border-neutral-700";
const sectionBoxCls = "mt-5 rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-700 dark:bg-neutral-950";

/** Inline empty-state note for a comparison section with zero deltas. */
function EmptySectionNote({ label }: { label: string }) {
  return (
    <OperatorEmptyState title={label}>
      <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">No changes in this section for this pair.</p>
    </OperatorEmptyState>
  );
}

/**
 * Prefer dollar + monthly framing when the payload is numeric (demo-friendly “100 vs 120” deltas).
 */
function formatCostEstimateCell(value: unknown): string {
  if (value === null || value === undefined) {
    return "—";
  }

  const s = String(value).trim();

  if (s.length === 0) {
    return "—";
  }

  if (/^[$€£]/.test(s)) {
    return `${s}/mo (est.)`;
  }

  if (/^\d+([\.,]\d+)?$/.test(s.replace(/,/g, ""))) {
    return `~$${s.replace(/,/g, "")}/mo (est.)`;
  }

  return s;
}

/** Card-style collapsible bucket for structured compare output. */
function ComparisonFoldSection(props: {
  title: string;
  countBadge: number;
  defaultOpen: boolean;
  children: ReactNode;
}) {
  return (
    <details className={sectionBoxCls} open={props.defaultOpen}>
      <summary className="cursor-pointer list-none text-[15px] font-semibold text-neutral-900 marker:content-none dark:text-neutral-100 [&::-webkit-details-marker]:hidden">
        <span className="mr-2 inline-flex items-center rounded-full bg-neutral-200 px-2 py-0 text-[11px] font-bold text-neutral-800 dark:bg-neutral-800 dark:text-neutral-200">
          {props.countBadge}
        </span>
        {props.title}
      </summary>
      <div className="mt-3">{props.children}</div>
    </details>
  );
}

/**
 * Golden-manifest structured comparison: tables and stable column order for operator review.
 */
export function StructuredComparisonView(props: { golden: GoldenManifestComparison }) {
  const golden = sortGoldenManifestComparison(props.golden);
  const total =
    golden.totalDeltaCount !== undefined
      ? golden.totalDeltaCount
      : golden.decisionChanges.length +
        golden.requirementChanges.length +
        golden.securityChanges.length +
        golden.topologyChanges.length +
        golden.costChanges.length;

  return (
    <section id="compare-structured" className="mt-7">
      <h3 className="mb-2">Manifest comparison</h3>
      <p className="mb-3 max-w-3xl text-sm font-medium leading-relaxed text-neutral-800 dark:text-neutral-100">
        Compare finalized manifests to understand what changed between runs — each card below summarizes one category.
        Prefer this narrative before supplementary diffs further down.
      </p>
      <div className="mb-3 flex flex-wrap items-baseline gap-3 text-sm text-neutral-700 dark:text-neutral-300">
        <span>
          <strong>Baseline run:</strong> {compareRunHeadingLabel(golden.baseRunId)}
        </span>
        <span aria-hidden="true" className="text-neutral-300 dark:text-neutral-600">
          →
        </span>
        <span>
          <strong>Updated run:</strong> {compareRunHeadingLabel(golden.targetRunId)}
        </span>
        <span className="text-neutral-500 dark:text-neutral-400">
          · <strong>Total deltas (reported):</strong> {total}
        </span>
      </div>
      <p className="mb-4 mt-0 text-sm">
        <a
          href={getArchitecturePackageDocxUrl(golden.baseRunId, golden.targetRunId, {
            includeComparisonExplanation: true,
          })}
          rel="noreferrer"
        >
          Download architecture package DOCX (includes comparison; AI narrative when configured)
        </a>
      </p>

      <ComparisonFoldSection title="Summary highlights" countBadge={golden.summaryHighlights.length} defaultOpen>
        {golden.summaryHighlights.length === 0 ? (
          <EmptySectionNote label="No summary highlights" />
        ) : (
          <ul className="m-0 pl-5 leading-normal">
            {golden.summaryHighlights.map((h, i) => (
              <li key={`highlight-${i}`}>{h}</li>
            ))}
          </ul>
        )}
      </ComparisonFoldSection>

      <ComparisonFoldSection title="Decision changes" countBadge={golden.decisionChanges.length} defaultOpen={golden.decisionChanges.length > 0}>
        {golden.decisionChanges.length === 0 ? (
          <EmptySectionNote label="No decision changes" />
        ) : (
          <table className="mt-2 w-full border-collapse text-sm">
            <thead>
              <tr className="bg-neutral-50/90 dark:bg-neutral-900/50">
                <th className={cellCls}>Decision</th>
                <th className={cellCls}>Base</th>
                <th className={cellCls}>Target</th>
                <th className={cellCls}>Change</th>
              </tr>
            </thead>
            <tbody>
              {golden.decisionChanges.map((d, i) => (
                <tr key={i}>
                  <td className={cellCls}>{d.decisionKey}</td>
                  <td className={cellCls}>{d.baseValue ?? "—"}</td>
                  <td className={cellCls}>{d.targetValue ?? "—"}</td>
                  <td className={cellCls}>{d.changeType}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </ComparisonFoldSection>

      <ComparisonFoldSection
        title="Requirement changes"
        countBadge={golden.requirementChanges.length}
        defaultOpen={golden.requirementChanges.length > 0}
      >
        {golden.requirementChanges.length === 0 ? (
          <EmptySectionNote label="No requirement changes" />
        ) : (
          <table className="mt-2 w-full border-collapse text-sm">
            <thead>
              <tr className="bg-neutral-50/90 dark:bg-neutral-900/50">
                <th className={cellCls}>Requirement</th>
                <th className={cellCls}>Change</th>
              </tr>
            </thead>
            <tbody>
              {golden.requirementChanges.map((r) => (
                <tr key={`${r.requirementName}:${r.changeType}`}>
                  <td className={cellCls}>{r.requirementName}</td>
                  <td className={cellCls}>{r.changeType}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </ComparisonFoldSection>

      <ComparisonFoldSection title="Finding / posture delta" countBadge={golden.securityChanges.length} defaultOpen={golden.securityChanges.length > 0}>
        {golden.securityChanges.length === 0 ? (
          <EmptySectionNote label="No security control changes" />
        ) : (
          <table className="mt-2 w-full border-collapse text-sm">
            <thead>
              <tr className="bg-neutral-50/90 dark:bg-neutral-900/50">
                <th className={cellCls}>Control</th>
                <th className={cellCls}>Base</th>
                <th className={cellCls}>Target</th>
              </tr>
            </thead>
            <tbody>
              {golden.securityChanges.map((s, i) => (
                <tr key={i}>
                  <td className={cellCls}>{s.controlName}</td>
                  <td className={cellCls}>{s.baseStatus ?? "—"}</td>
                  <td className={cellCls}>{s.targetStatus ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </ComparisonFoldSection>

      <ComparisonFoldSection title="Topology / footprint" countBadge={golden.topologyChanges.length} defaultOpen={golden.topologyChanges.length > 0}>
        {golden.topologyChanges.length === 0 ? (
          <EmptySectionNote label="No topology changes" />
        ) : (
          <table className="mt-2 w-full border-collapse text-sm">
            <thead>
              <tr className="bg-neutral-50/90 dark:bg-neutral-900/50">
                <th className={cellCls}>Resource</th>
                <th className={cellCls}>Change</th>
              </tr>
            </thead>
            <tbody>
              {golden.topologyChanges.map((t) => (
                <tr key={`${t.resource}:${t.changeType}`}>
                  <td className={cellCls}>{t.resource}</td>
                  <td className={cellCls}>{t.changeType}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </ComparisonFoldSection>

      <ComparisonFoldSection title="Estimated cost delta" countBadge={golden.costChanges.length} defaultOpen={golden.costChanges.length > 0}>
        {golden.costChanges.length === 0 ? (
          <OperatorEmptyState title="No modeled cost deltas">
            <p className="m-0 text-sm">Estimated max monthly cost unchanged or not surfaced as numeric delta rows.</p>
          </OperatorEmptyState>
        ) : (
          <table className="mt-2 w-full border-collapse text-sm">
            <thead>
              <tr className="bg-neutral-50/90 dark:bg-neutral-900/50">
                <th className={cellCls}>Baseline (est. max monthly)</th>
                <th className={cellCls}>Updated (est. max monthly)</th>
              </tr>
            </thead>
            <tbody>
              {golden.costChanges.map((c, i) => (
                <tr key={`${String(c.baseCost ?? "n")}-${String(c.targetCost ?? "n")}-${i}`}>
                  <td className={cellCls}>{formatCostEstimateCell(c.baseCost)}</td>
                  <td className={cellCls}>{formatCostEstimateCell(c.targetCost)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </ComparisonFoldSection>
    </section>
  );
}

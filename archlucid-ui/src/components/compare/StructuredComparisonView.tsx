import type { ReactNode } from "react";

import { decisionKeyDisplay } from "@/lib/compare-decision-key-display";
import { getArchitecturePackageDocxUrl } from "@/lib/api";
import { compareRunHeadingLabel } from "@/lib/compare-run-display";
import { sortGoldenManifestComparison } from "@/lib/compare-display-sort";
import type { GoldenManifestComparison } from "@/types/comparison";

const cellCls = "border border-neutral-200 px-2.5 py-2 text-left align-top dark:border-neutral-700";
const sectionBoxCls = "mt-5 rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-700 dark:bg-neutral-950";

/**
 * Prefer dollar + monthly framing when the payload is numeric (demo-friendly "100 vs 120" deltas).
 */
function formatCostEstimateCell(value: unknown): string {
  if (value === null || value === undefined) return "?";

  const s = String(value).trim();

  if (s.length === 0) return "?";

  if (/^[\$\u00a3\u20ac]/.test(s)) {
    return `${s}/mo ? projected monthly run rate (from manifest pipeline cost model)`;
  }

  if (/^\d+([\.,]\d+)?$/.test(s.replace(/,/g, ""))) {
    return `~$${s.replace(/,/g, "")}/mo ? projected monthly run rate`;
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

  const noMaterialDeltaSections =
    golden.decisionChanges.length === 0 &&
    golden.requirementChanges.length === 0 &&
    golden.securityChanges.length === 0 &&
    golden.topologyChanges.length === 0 &&
    golden.costChanges.length === 0;

  return (
    <section id="compare-structured" className="mt-7">
      <h3 className="mb-2">Manifest comparison</h3>
      <p className="mb-3 max-w-3xl text-sm font-medium leading-relaxed text-neutral-800 dark:text-neutral-100">
        Compare finalized manifests to understand what changed between reviews ? each card below summarizes one category.
        Prefer this narrative before supplementary diffs further down.
      </p>
      <div className="mb-3 flex flex-wrap items-baseline gap-3 text-sm text-neutral-700 dark:text-neutral-300">
        <span>
          <strong>Baseline review:</strong> {compareRunHeadingLabel(golden.baseRunId)}
        </span>
        <span aria-hidden="true" className="text-neutral-300 dark:text-neutral-600">
          ?
        </span>
        <span>
          <strong>Updated review:</strong> {compareRunHeadingLabel(golden.targetRunId)}
        </span>
        <span className="text-neutral-500 dark:text-neutral-400">
          · <strong>Total changes:</strong> {total}
        </span>
      </div>
      {golden.summaryHighlights.length > 0 ? (
        <p className="mb-3 max-w-3xl rounded-md border border-teal-200/80 bg-teal-50/50 p-3 text-sm text-neutral-900 dark:border-teal-900/50 dark:bg-teal-950/30 dark:text-neutral-100">
          <strong>Sponsor recommendation:</strong> {golden.summaryHighlights[0]}
          {golden.summaryHighlights.length > 1 ? (
            <span className="text-neutral-600 dark:text-neutral-400">
              {" "}
              (+{golden.summaryHighlights.length - 1} more in summary highlights below)
            </span>
          ) : null}
        </p>
      ) : null}
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

      {golden.summaryHighlights.length > 0 ? (
        <ComparisonFoldSection title="Summary highlights" countBadge={golden.summaryHighlights.length} defaultOpen>
          <ul className="m-0 pl-5 leading-normal">
            {golden.summaryHighlights.map((h, i) => (
              <li key={`highlight-${i}`}>{h}</li>
            ))}
          </ul>
        </ComparisonFoldSection>
      ) : null}

      {noMaterialDeltaSections ? (
        <div
          className="mt-4 rounded-md border border-neutral-200 bg-neutral-50/80 px-3 py-2 text-sm text-neutral-800 dark:border-neutral-700 dark:bg-neutral-900/40 dark:text-neutral-200"
          role="status"
          data-testid="compare-no-material-deltas"
        >
          <strong className="font-semibold">No other material changes</strong>
          <span className="text-neutral-600 dark:text-neutral-400">
            {" "}
            ? no decision, requirement, security posture, topology, or modeled cost changes in this comparison payload.
          </span>
        </div>
      ) : (
        <>
          {golden.decisionChanges.length > 0 ? (
            <ComparisonFoldSection title="Decision changes" countBadge={golden.decisionChanges.length} defaultOpen>
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
                      <td className={cellCls}>
                        <div className="font-medium text-neutral-900 dark:text-neutral-100">
                          {d.displayLabel?.trim() ? d.displayLabel.trim() : decisionKeyDisplay(d.decisionKey)}
                        </div>
                        {d.displayLabel?.trim() ? (
                          <details className="mt-1 text-[11px] text-neutral-500 dark:text-neutral-400">
                            <summary className="cursor-pointer select-none">Technical key</summary>
                            <code className="mt-0.5 block font-mono text-[11px]">{d.decisionKey}</code>
                          </details>
                        ) : (
                          <div className="mt-0.5 font-mono text-[11px] text-neutral-500 dark:text-neutral-400">
                            {d.decisionKey}
                          </div>
                        )}
                      </td>
                      <td className={cellCls}>{d.baseValue ?? "?"}</td>
                      <td className={cellCls}>{d.targetValue ?? "?"}</td>
                      <td className={cellCls}>{d.changeType}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </ComparisonFoldSection>
          ) : null}

          {golden.requirementChanges.length > 0 ? (
            <ComparisonFoldSection
              title="Requirement changes"
              countBadge={golden.requirementChanges.length}
              defaultOpen
            >
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
            </ComparisonFoldSection>
          ) : null}

          {golden.securityChanges.length > 0 ? (
            <ComparisonFoldSection title="Finding / posture delta" countBadge={golden.securityChanges.length} defaultOpen>
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
                      <td className={cellCls}>{s.baseStatus ?? "?"}</td>
                      <td className={cellCls}>{s.targetStatus ?? "?"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </ComparisonFoldSection>
          ) : null}

          {golden.topologyChanges.length > 0 ? (
            <ComparisonFoldSection title="Topology / footprint" countBadge={golden.topologyChanges.length} defaultOpen>
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
            </ComparisonFoldSection>
          ) : null}

          {golden.costChanges.length > 0 ? (
            <ComparisonFoldSection title="Projected cost impact" countBadge={golden.costChanges.length} defaultOpen>
              <table className="mt-2 w-full border-collapse text-sm">
                <thead>
                  <tr className="bg-neutral-50/90 dark:bg-neutral-900/50">
                    <th className={cellCls}>Baseline ? projected monthly run rate</th>
                    <th className={cellCls}>Updated ? projected monthly run rate</th>
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
              <p className="mt-2 max-w-prose text-xs text-neutral-600 dark:text-neutral-400">
                Projected monthly run rates are derived from the manifest pipeline cost model. Figures reflect the
                architecture as described ? validate against your FinOps baseline before using in budget planning.
                Use &ldquo;Summarize for sponsor&rdquo; to include this delta in an executive narrative.
              </p>
            </ComparisonFoldSection>
          ) : null}
        </>
      )}
    </section>
  );
}

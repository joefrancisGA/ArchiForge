import Link from "next/link";

import type { ReactElement } from "react";

import { FindingInspectJsonPayload } from "@/components/FindingInspectJsonPayload";
import { CollapsibleSection } from "@/components/CollapsibleSection";
import { CopyIdButton } from "@/components/CopyIdButton";
import { ProductLearningFeedbackControls } from "@/components/ProductLearningFeedbackControls";
import { isNextPublicDemoMode } from "@/lib/demo-ui-env";
import { getShowcaseManifestHref } from "@/lib/buyer-safe-review-navigation";
import { isDemoRunIdEligibleForStaticFallback } from "@/lib/operator-static-demo";
import type { FindingInspectPayload } from "@/types/finding-inspect";
import {
  findingInspectPrimaryLabels,
  findingWhyThisMattersText,
} from "@/lib/finding-display-from-inspect";
import type { FindingInspectEvidence } from "@/types/finding-inspect";

export type FindingInspectFindingBodyProps = {
  readonly runId: string;
  readonly decodedFindingId: string;
  readonly payload: FindingInspectPayload;
  readonly variant?: "detail" | "inspect";
};

function evidencePrimaryText(row: FindingInspectEvidence): string {
  const excerpt = row.excerpt?.trim() ?? "";
  if (excerpt.length > 0) {
    return excerpt;
  }

  const artifact = row.artifactId?.trim() ?? "";
  if (artifact.length > 0) {
    return `Artifact reference: ${artifact}`;
  }

  return "Evidence item (no excerpt on file).";
}

function EvidenceExcerptBody({ text }: { readonly text: string }): ReactElement {
  const proseLike = text.includes(" ") && text.length > 24;

  if (proseLike) {
    return <p className="m-0 text-sm leading-relaxed text-neutral-800 dark:text-neutral-200">{text}</p>;
  }

  return <span className="font-mono text-xs text-neutral-800 dark:text-neutral-200">{text}</span>;
}

/**
 * Shared deterministic finding body — product-facing headings first,
 * collapsible identifiers and typed payload only on Finding detail (`variant=detail`) by default.
 */
export function FindingInspectFindingBody({
  runId,
  decodedFindingId,
  payload,
  variant = "inspect",
}: FindingInspectFindingBodyProps): ReactElement {
  const demoFillGaps = isNextPublicDemoMode() || isDemoRunIdEligibleForStaticFallback(runId);
  const reviewContextHref = isDemoRunIdEligibleForStaticFallback(runId)
    ? getShowcaseManifestHref()
    : `/reviews/${encodeURIComponent(runId)}`;
  const reviewContextLabel = isDemoRunIdEligibleForStaticFallback(runId)
    ? "Review package (manifest & context)"
    : "Open review detail (artifacts & graph)";
  const labels = findingInspectPrimaryLabels(payload);
  const whyThisMattersNarrative = findingWhyThisMattersText(payload);

  // Use the full ordered list from the API when present; otherwise fall back to single derived label.
  const structuredActions: string[] = (payload.recommendedActions ?? []).filter((a) => a.trim().length > 0);
  const recommendedActionParagraph =
    labels.recommendedAction ??
    "Review evidence and rationale above. Consult the finding category and primary rule to determine the appropriate remediation path.";

  return (
    <>
      <section className="rounded-lg border border-neutral-200 bg-neutral-50/80 p-4 dark:border-neutral-700 dark:bg-neutral-900/40">
        <h2 className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Why this matters</h2>
        {whyThisMattersNarrative ? (
          <p className="m-0 mt-2 text-sm leading-relaxed text-neutral-800 dark:text-neutral-200">
            {whyThisMattersNarrative}
          </p>
        ) : demoFillGaps ? (
          <p className="m-0 mt-2 text-sm leading-relaxed text-neutral-800 dark:text-neutral-200">
            In the Claims Intake sample, sensitive fields (PHI) are carried through the request path into downstream
            services. Without explicit redaction and access boundaries, reviewers cannot show regulators a defensible
            boundary for where patient identifiers stop.
          </p>
        ) : (
          <p className="m-0 mt-2 text-sm text-neutral-600 dark:text-neutral-400">
            — (no dedicated rationale on file; see primary rule and evidence below.)
          </p>
        )}
        <dl className="mt-3 space-y-2 text-sm text-neutral-800 dark:text-neutral-200">
          <div>
            <dt className="font-medium text-neutral-600 dark:text-neutral-400">Primary rule</dt>
            <dd className="m-0 mt-1">{payload.decisionRuleName ?? payload.decisionRuleId ?? "—"}</dd>
          </div>
        </dl>
        {payload.decisionRuleId ? (
          <div className="mt-3">
            <CollapsibleSection title="Technical rule identifier" defaultOpen={variant === "inspect"}>
              <div className="flex flex-wrap items-center gap-2">
                <code className="rounded bg-neutral-100 px-1.5 py-0.5 font-mono text-xs dark:bg-neutral-800">
                  {payload.decisionRuleId}
                </code>
                <CopyIdButton value={payload.decisionRuleId} aria-label="Copy rule identifier" />
              </div>
            </CollapsibleSection>
          </div>
        ) : null}
      </section>

      {variant === "detail" ? (
        <>
          <section className="rounded-lg border border-teal-200/90 bg-teal-50/50 p-4 dark:border-teal-900 dark:bg-teal-950/30">
            <h2 className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Recommended action</h2>
            {structuredActions.length > 1 ? (
              <ol className="mb-0 mt-2 list-decimal space-y-1.5 pl-5 text-sm text-neutral-800 dark:text-neutral-200">
                {structuredActions.map((action, idx) => (
                  <li key={idx}>{action}</li>
                ))}
              </ol>
            ) : (
              <p className="m-0 mt-2 whitespace-pre-line text-sm text-neutral-800 dark:text-neutral-200">
                {recommendedActionParagraph.trim()}
              </p>
            )}
          </section>
          <ProductLearningFeedbackControls
            runId={runId}
            manifestVersion={payload.manifestVersion}
            subjectType="Finding"
            artifactHint={`finding:${decodedFindingId}`}
            patternKey={
              payload.decisionRuleId
                ? `finding-rule:${payload.decisionRuleId}`
                : "finding"
            }
            detail={{
              findingId: decodedFindingId,
              decisionRuleId: payload.decisionRuleId,
            }}
            title="Was this finding useful?"
          />
        </>
      ) : null}

      <section className="rounded-lg border border-neutral-200 bg-neutral-50/80 p-4 dark:border-neutral-700 dark:bg-neutral-900/40">
        <h2 className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Evidence</h2>
        {payload.evidence.length === 0 ? (
          demoFillGaps ? (
            <ul className="mt-2 list-none space-y-3 p-0 text-sm">
              <li className="rounded-md border border-teal-200/80 bg-white/80 p-3 dark:border-teal-900 dark:bg-neutral-900/60">
                <p className="m-0 leading-relaxed text-neutral-800 dark:text-neutral-200">
                  Graph citation: <strong>Claims intake API</strong> — request schema includes unredacted member ID and
                  date of birth fields referenced by the adjudication subgraph.
                </p>
                <p className="m-0 mt-2 text-xs text-neutral-600 dark:text-neutral-400">
                  Sample-only evidence for buyer walkthrough; production findings link live graph nodes and artifacts.
                </p>
                <div className="mt-2">
                  <Link
                    href={reviewContextHref}
                    className="text-xs font-medium text-sky-700 underline dark:text-sky-300"
                  >
                    {reviewContextLabel}
                  </Link>
                </div>
              </li>
            </ul>
          ) : (
            <p className="m-0 mt-2 text-sm text-neutral-600 dark:text-neutral-400">No related graph citations on file.</p>
          )
        ) : (
          <ul className="mt-2 list-none space-y-3 p-0 text-sm">
            {payload.evidence.map((row, idx) => (
              <li
                key={`${row.excerpt ?? "ev"}-${row.artifactId ?? ""}-${idx}`}
                className="rounded-md border border-neutral-200 bg-white/80 p-3 dark:border-neutral-700 dark:bg-neutral-900/60"
              >
                <EvidenceExcerptBody text={evidencePrimaryText(row)} />
                {row.lineRange ? (
                  <p className="m-0 mt-1 text-xs text-neutral-600 dark:text-neutral-400">Lines: {row.lineRange}</p>
                ) : null}
                {row.artifactId ? (
                  <p className="m-0 mt-1 font-mono text-xs text-neutral-600 dark:text-neutral-400">
                    Artifact id: {row.artifactId}
                  </p>
                ) : null}
                <div className="mt-2">
                  <Link
                    href={reviewContextHref}
                    className="text-xs font-medium text-sky-700 underline dark:text-sky-300"
                  >
                    {reviewContextLabel}
                  </Link>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      {variant === "inspect" ? (
        <section className="rounded-lg border border-violet-200 bg-violet-50/70 p-4 dark:border-violet-900 dark:bg-violet-950/30">
          <h2 className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Recommended action</h2>
          {structuredActions.length > 1 ? (
            <ol className="mb-0 mt-2 list-decimal space-y-1.5 pl-5 text-sm text-neutral-800 dark:text-neutral-200">
              {structuredActions.map((action, idx) => (
                <li key={idx}>{action}</li>
              ))}
            </ol>
          ) : (
            <p className="m-0 mt-2 whitespace-pre-line text-sm text-neutral-800 dark:text-neutral-200">
              {recommendedActionParagraph.trim()}
            </p>
          )}
        </section>
      ) : null}

      <details className="rounded-lg border border-neutral-200 bg-neutral-50/80 dark:border-neutral-700 dark:bg-neutral-900/40">
        <summary className="cursor-pointer select-none px-4 py-3 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
          View AI Reasoning
        </summary>
        <div className="border-t border-neutral-200 px-4 pb-4 pt-2 dark:border-neutral-700">
          {payload.reasoningTrace ? (
            <p className="m-0 text-sm leading-relaxed text-neutral-800 dark:text-neutral-200 whitespace-pre-wrap">
              {payload.reasoningTrace}
            </p>
          ) : (
            <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
              No reasoning trace available for this finding.
            </p>
          )}
        </div>
      </details>

      <details className="rounded-lg border border-neutral-200 bg-neutral-50/80 dark:border-neutral-700 dark:bg-neutral-900/40">
        <summary className="cursor-pointer select-none px-4 py-3 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
          AI Audit Inspection
        </summary>
        <div className="border-t border-neutral-200 px-4 pb-4 pt-2 dark:border-neutral-700">
          <FindingInspectJsonPayload value={payload.typedPayload ?? null} />
        </div>
      </details>

      <section className="rounded-lg border border-neutral-200 bg-neutral-50/80 p-4 dark:border-neutral-700 dark:bg-neutral-900/40">
        <h2 className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Audit</h2>
        {payload.auditRowId ? (
          <p className="m-0 mt-2 text-sm text-neutral-800 dark:text-neutral-200">
            Durable audit event id: <span className="font-mono text-xs">{payload.auditRowId}</span>
            <span className="ml-2">
              <Link href="/audit" className="text-sky-700 underline dark:text-sky-300">
                Search in audit log
              </Link>
            </span>
          </p>
        ) : demoFillGaps ? (
          <div className="mt-2 space-y-1 text-sm text-neutral-800 dark:text-neutral-200">
            <p className="m-0">
              <strong className="font-medium">Sample audit trail (demo)</strong> — Finding recorded with severity review
              and rule linkage. Actor: <span className="text-neutral-600 dark:text-neutral-400">Governance automation</span>
              {" · "}
              Outcome: <span className="text-neutral-600 dark:text-neutral-400">Escalated for architecture review</span>
            </p>
            <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
              Production tenants emit the same fields from tenant-scoped audit storage.
            </p>
          </div>
        ) : (
          <p className="m-0 mt-2 text-sm text-neutral-600 dark:text-neutral-400">
            Audit record not available in this environment (SQL-backed audit logging may be disabled).
          </p>
        )}
      </section>
    </>
  );
}

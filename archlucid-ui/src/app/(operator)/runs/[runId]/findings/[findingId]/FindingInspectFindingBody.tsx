import Link from "next/link";
import type { ReactElement } from "react";

import { FindingInspectJsonPayload } from "@/components/FindingInspectJsonPayload";
import { CollapsibleSection } from "@/components/CollapsibleSection";
import { CopyIdButton } from "@/components/CopyIdButton";
import type { FindingInspectPayload } from "@/types/finding-inspect";

import { findingInspectPrimaryLabels } from "@/lib/finding-display-from-inspect";

export type FindingInspectFindingBodyProps = {
  readonly runId: string;
  readonly decodedFindingId: string;
  readonly payload: FindingInspectPayload;
  readonly variant?: "detail" | "inspect";
};

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
  const labels = findingInspectPrimaryLabels(payload);

  const recommendedActionParagraph =
    labels.recommendedAction ??
    `Review PHI handling posture with intake and security owners — align retention, encryption in transit/at rest, and egress controls with organizational policy before production rollout (${decodedFindingId}).`;

  return (
    <>
      <section className="rounded-lg border border-neutral-200 bg-neutral-50/80 p-4 dark:border-neutral-700 dark:bg-neutral-900/40">
        <h2 className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
          Why this was flagged
        </h2>
        <dl className="mt-2 space-y-2 text-sm text-neutral-800 dark:text-neutral-200">
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

      <section className="rounded-lg border border-neutral-200 bg-neutral-50/80 p-4 dark:border-neutral-700 dark:bg-neutral-900/40">
        <h2 className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Evidence</h2>
        {payload.evidence.length === 0 ? (
          <p className="m-0 mt-2 text-sm text-neutral-600 dark:text-neutral-400">No related graph citations on file.</p>
        ) : (
          <ul className="mt-2 list-disc space-y-2 pl-5 text-sm text-neutral-800 dark:text-neutral-200">
            {payload.evidence.map((row, idx) => (
              <li key={`${row.excerpt ?? "ev"}-${idx}`}>
                <span className="font-mono text-xs">{row.excerpt ?? row.artifactId ?? "(empty)"}</span>
                {row.lineRange ? (
                  <span className="ml-2 text-neutral-600 dark:text-neutral-400">({row.lineRange})</span>
                ) : null}
                <div className="mt-1">
                  <Link
                    href={`/runs/${encodeURIComponent(runId)}`}
                    className="text-xs text-sky-700 underline dark:text-sky-300"
                  >
                    Open run (artifacts and graph context)
                  </Link>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      {variant === "detail" ? (
        <section className="rounded-lg border border-violet-200 bg-violet-50/70 p-4 dark:border-violet-900 dark:bg-violet-950/30">
          <h2 className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
            Recommended action
          </h2>
          <p className="m-0 mt-2 whitespace-pre-line text-sm text-neutral-800 dark:text-neutral-200">
            {recommendedActionParagraph.trim()}
          </p>
        </section>
      ) : null}

      {variant === "detail" ? (
        <CollapsibleSection title="Structured evidence (technical)" defaultOpen={false}>
          <FindingInspectJsonPayload value={payload.typedPayload ?? null} />
        </CollapsibleSection>
      ) : (
        <section className="rounded-lg border border-neutral-200 bg-neutral-50/80 p-4 dark:border-neutral-700 dark:bg-neutral-900/40">
          <h2 className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
            Structured evidence (technical)
          </h2>
          <div className="mt-2">
            <FindingInspectJsonPayload value={payload.typedPayload ?? null} />
          </div>
        </section>
      )}

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
        ) : (
          <p className="m-0 mt-2 text-sm text-neutral-600 dark:text-neutral-400">
            Audit record not available in this environment (SQL-backed audit logging may be disabled).
          </p>
        )}
      </section>
    </>
  );
}

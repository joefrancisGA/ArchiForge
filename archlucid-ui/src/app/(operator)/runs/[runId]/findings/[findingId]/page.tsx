import Link from "next/link";

import { CopyFindingAsWorkItemButton } from "@/components/CopyFindingAsWorkItemButton";
import { CollapsibleSection } from "@/components/CollapsibleSection";
import { CopyIdButton } from "@/components/CopyIdButton";
import { FindingExplainPanel } from "@/components/FindingExplainPanel";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Badge } from "@/components/ui/badge";
import { getFindingInspect } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import type { FindingInspectPayload } from "@/types/finding-inspect";

import {
  findingDetailHeadingTitle,
  findingDetailLeadSentence,
  findingInspectPrimaryLabels,
} from "@/lib/finding-display-from-inspect";

import { FindingInspectFindingBody } from "./FindingInspectFindingBody";

/**
 * Finding detail: deterministic inspect sections first (server-loaded), supplemental AI audit collapsed below.
 */
export default async function RunFindingExplainPage({
  params,
}: {
  params: Promise<{ runId: string; findingId: string }>;
}) {
  const { runId, findingId } = await params;
  const decodedFindingId = decodeURIComponent(findingId);

  let inspectPayload: FindingInspectPayload | null = null;

  let inspectFailure: ApiLoadFailureState | null = null;

  try {
    inspectPayload = await getFindingInspect(runId, decodedFindingId);
  } catch (e) {
    inspectFailure = toApiLoadFailure(e);
  }

  const labels = inspectPayload !== null ? findingInspectPrimaryLabels(inspectPayload) : null;

  const pageTitle =
    inspectPayload !== null ? findingDetailHeadingTitle(inspectPayload) : "Finding detail";

  return (
    <main className="mx-auto max-w-3xl space-y-6 p-6">
      <nav className="flex flex-wrap items-center gap-3 text-sm text-neutral-600 dark:text-neutral-400">
        <Link
          href={`/runs/${encodeURIComponent(runId)}`}
          className="text-teal-800 underline decoration-neutral-300 underline-offset-2 hover:text-teal-900 dark:text-teal-300 dark:decoration-neutral-600 dark:hover:text-teal-200"
        >
          ← Back to architecture run
        </Link>
        <span aria-hidden="true">·</span>
        <Link
          href={`/runs/${encodeURIComponent(runId)}/findings/${encodeURIComponent(decodedFindingId)}/inspect`}
          className="text-teal-800 underline decoration-neutral-300 underline-offset-2 hover:text-teal-900 dark:text-teal-300 dark:decoration-neutral-600 dark:hover:text-teal-200"
        >
          Technical inspection trail
        </Link>
      </nav>

      <header className="space-y-2">
        <h1 className="text-xl font-semibold text-neutral-900 dark:text-neutral-100">{pageTitle}</h1>

        <p className="m-0 flex flex-wrap items-center gap-2 text-sm text-neutral-600 dark:text-neutral-400">
          <span className="font-medium text-neutral-800 dark:text-neutral-200">Finding</span>
          <code className="max-w-full break-all rounded bg-neutral-100 px-1.5 py-0.5 text-xs font-mono dark:bg-neutral-800">
            {decodedFindingId}
          </code>
          <CopyIdButton value={decodedFindingId} aria-label="Copy finding ID" />
          {labels?.severityLabel ? (
            <Badge variant="secondary" className="font-normal">
              {labels.severityLabel}
            </Badge>
          ) : null}

          {labels?.categoryLabel ? (
            <span className="text-xs text-neutral-600 dark:text-neutral-400">{labels.categoryLabel}</span>
          ) : null}
        </p>

        {labels?.impactedAreaLabel ? (
          <p className="m-0 text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
            <span className="font-medium text-neutral-800 dark:text-neutral-100">Impacted area: </span>
            {labels.impactedAreaLabel}
          </p>
        ) : null}

        {inspectPayload !== null ? (
          <p className="m-0 text-sm leading-relaxed text-neutral-600 dark:text-neutral-400">
            {findingDetailLeadSentence(inspectPayload)}
          </p>
        ) : null}

        {inspectPayload !== null ? (
          <div className="pt-1">
            <CopyFindingAsWorkItemButton
              findingId={decodedFindingId}
              payload={inspectPayload}
              runId={runId}
            />
          </div>
        ) : null}
      </header>

      {inspectFailure !== null ? (
        <OperatorApiProblem
          problem={inspectFailure.problem}
          fallbackMessage={inspectFailure.message}
          correlationId={inspectFailure.correlationId}
        />
      ) : null}

      {inspectPayload !== null ? (
        <FindingInspectFindingBody
          runId={runId}
          decodedFindingId={decodedFindingId}
          payload={inspectPayload}
          variant="detail"
        />
      ) : null}

      <CollapsibleSection title="AI audit (technical details)" defaultOpen={false}>
        <FindingExplainPanel runId={runId} findingId={findingId} />
      </CollapsibleSection>
    </main>
  );
}

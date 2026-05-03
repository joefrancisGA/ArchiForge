import Link from "next/link";

import { notFound } from "next/navigation";

import { CopyFindingAsWorkItemButton } from "@/components/CopyFindingAsWorkItemButton";
import { CollapsibleSection } from "@/components/CollapsibleSection";
import { CopyIdButton } from "@/components/CopyIdButton";
import { FindingExplainPanel } from "@/components/FindingExplainPanel";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { OperatorEvidenceLimitsFooter } from "@/components/OperatorEvidenceLimitsFooter";
import { Badge } from "@/components/ui/badge";
import { getFindingInspect } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { isApiNotFoundFailure, toApiLoadFailure } from "@/lib/api-load-failure";
import { tryStaticDemoFindingInspect } from "@/lib/operator-static-demo";
import type { FindingInspectPayload } from "@/types/finding-inspect";

import {
  findingDetailHeadingTitle,
  findingDetailLeadSentence,
  findingInspectPrimaryLabels,
} from "@/lib/finding-display-from-inspect";

import { isInvalidDynamicRouteToken, isInvalidGuidOrSlugRouteToken } from "@/lib/route-dynamic-param";
import { tryLoadRunExecutionFootnote } from "@/lib/try-load-run-execution-footnote";

import { FindingInspectFindingBody } from "./FindingInspectFindingBody";

function authorityRunIdsAlignForDemo(urlRunId: string, payloadRunId: string): boolean {
  const norm = (s: string): string => s.replace(/-/g, "").toLowerCase();

  return norm(urlRunId) === norm(payloadRunId);
}

/**
 * Finding detail: severity and narrative first; technical identifiers and export tools collapsed.
 */
export default async function RunFindingExplainPage({
  params,
}: {
  params: Promise<{ runId: string; findingId: string }>;
}) {
  const { runId, findingId } = await params;

  if (isInvalidGuidOrSlugRouteToken(runId)) {
    notFound();
  }

  if (isInvalidDynamicRouteToken(findingId)) {
    notFound();
  }

  const decodedFindingId = decodeURIComponent(findingId);

  let inspectPayload: FindingInspectPayload | null = null;

  let inspectFailure: ApiLoadFailureState | null = null;

  try {
    inspectPayload = await getFindingInspect(runId, decodedFindingId);
  } catch (e) {
    inspectFailure = toApiLoadFailure(e);

    const staticInspect = tryStaticDemoFindingInspect(runId, decodedFindingId);

    if (staticInspect !== null) {
      inspectPayload = staticInspect;
      inspectFailure = null;
    } else if (isApiNotFoundFailure(inspectFailure)) {
      notFound();
    }
  }

  if (inspectPayload !== null) {
    const staticInspect = tryStaticDemoFindingInspect(runId, decodedFindingId);

    if (staticInspect !== null && !authorityRunIdsAlignForDemo(runId, inspectPayload.runId)) {
      inspectPayload = staticInspect;
      inspectFailure = null;
    }
  }

  const runExecutionFootnote = await tryLoadRunExecutionFootnote(runId);

  const labels = inspectPayload !== null ? findingInspectPrimaryLabels(inspectPayload) : null;

  const pageTitle =
    inspectPayload !== null ? findingDetailHeadingTitle(inspectPayload) : "Finding detail";

  return (
    <main className="mx-auto max-w-3xl space-y-6 p-6">
      <nav className="flex flex-wrap items-center gap-3 text-sm text-neutral-600 dark:text-neutral-400">
        <Link
          href={`/reviews/${encodeURIComponent(runId)}/findings/${encodeURIComponent(decodedFindingId)}/inspect`}
          className="text-teal-800 underline decoration-neutral-300 underline-offset-2 hover:text-teal-900 dark:text-teal-300 dark:decoration-neutral-600 dark:hover:text-teal-200"
        >
          Technical inspection trail
        </Link>
      </nav>

      <header className="space-y-3">
        <h1 className="text-xl font-semibold text-neutral-900 dark:text-neutral-100">{pageTitle}</h1>

        {labels !== null ? (
          <div className="flex flex-wrap items-center gap-2">
            {labels.severityLabel ? (
              <Badge variant="secondary" className="font-normal">
                {labels.severityLabel}
              </Badge>
            ) : null}
            {labels.categoryLabel ? (
              <Badge variant="outline" className="font-normal">
                {labels.categoryLabel}
              </Badge>
            ) : null}
            {labels.statusLabel ? (
              <Badge variant="outline" className="font-normal">
                {labels.statusLabel}
              </Badge>
            ) : null}
            {labels.impactedAreaLabel ? (
              <Badge variant="outline" className="max-w-full whitespace-normal text-left font-normal">
                Business impact: {labels.impactedAreaLabel}
              </Badge>
            ) : null}
          </div>
        ) : null}

        {inspectPayload !== null ? (
          <p className="m-0 text-sm leading-relaxed text-neutral-600 dark:text-neutral-400">
            {findingDetailLeadSentence(inspectPayload)}
          </p>
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

      {inspectPayload !== null ? (
        <CollapsibleSection title="Export for remediation ticket" defaultOpen={false}>
          <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
            Copy a structured summary formatted for your issue tracker (Markdown, GitHub Issues, Azure Boards, or Jira).
          </p>
          <div className="pt-3">
            <CopyFindingAsWorkItemButton findingId={decodedFindingId} payload={inspectPayload} runId={runId} />
          </div>
        </CollapsibleSection>
      ) : null}

      {inspectPayload !== null ? (
        <CollapsibleSection title="Technical identifiers" defaultOpen={false}>
          <dl className="m-0 grid gap-2 text-sm text-neutral-800 dark:text-neutral-200">
            <div>
              <dt className="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
                Finding id
              </dt>
              <dd className="m-0 mt-1 flex flex-wrap items-center gap-2">
                <code className="max-w-full break-all rounded bg-neutral-100 px-1.5 py-0.5 text-xs font-mono dark:bg-neutral-800">
                  {decodedFindingId}
                </code>
                <CopyIdButton value={decodedFindingId} aria-label="Copy finding ID" />
              </dd>
            </div>
            {inspectPayload.manifestVersion ? (
              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
                  Manifest version
                </dt>
                <dd className="m-0 mt-1 font-mono text-xs">{inspectPayload.manifestVersion}</dd>
              </div>
            ) : null}
          </dl>
        </CollapsibleSection>
      ) : null}

      <CollapsibleSection title="Technical audit trail" defaultOpen={false}>
        <FindingExplainPanel
          runId={runId}
          findingId={findingId}
          confidenceLevel={inspectPayload.confidenceLevel ?? null}
        />
      </CollapsibleSection>

      <OperatorEvidenceLimitsFooter
        runId={runId}
        findingIdForInspectLink={decodedFindingId}
        execution={runExecutionFootnote}
        inspectMetadata={
          inspectPayload !== null
            ? {
                modelDeploymentName: inspectPayload.modelDeploymentName ?? null,
                promptTemplateVersion: inspectPayload.promptTemplateVersion ?? null,
              }
            : null
        }
      />
    </main>
  );
}

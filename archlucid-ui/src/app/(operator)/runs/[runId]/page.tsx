import Link from "next/link";
import type { ReactElement } from "react";

import { OperatorDemoStaticBanner } from "@/components/OperatorDemoStaticBanner";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import {
  OperatorEmptyState,
  OperatorMalformedCallout,
} from "@/components/OperatorShellMessage";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import {
  coerceArtifactDescriptorList,
  coerceManifestSummary,
  coerceRunDetail,
} from "@/lib/operator-response-guards";
import { manifestStatusForDisplay } from "@/lib/manifest-status-display";
import { effectiveRunSummaryForPipeline } from "@/lib/run-summary-from-detail";
import { ArtifactListTable } from "@/components/ArtifactListTable";
import { AuthorityPipelineTimeline } from "@/components/AuthorityPipelineTimeline";
import { ContextualHelp } from "@/components/ContextualHelp";
import { BeforeAfterDeltaPanel } from "@/components/BeforeAfterDeltaPanel";
import { CollapsibleSection } from "@/components/CollapsibleSection";
import { CopyIdButton } from "@/components/CopyIdButton";
import { RunExplanationSection } from "@/components/RunExplanationSection";
import { RunFindingExplainabilityTable } from "@/components/RunFindingExplainabilityTable";
import { RunDetailSectionNav, type RunDetailSection } from "@/components/RunDetailSectionNav";
import { RunDetailOutcomeCards } from "@/components/RunDetailOutcomeCards";
import { RunDetailPageHeader } from "@/components/RunDetailPageHeader";
import { RunProgressTracker } from "@/components/RunProgressTracker";
import { RunAgentForensicsSection } from "@/components/RunAgentForensicsSection";
import { EmailRunToSponsorBanner } from "@/components/EmailRunToSponsorBanner";
import { GenerateSponsorValueReportButton } from "@/components/GenerateSponsorValueReportButton";
import { PostCommitAdvancedAnalysisHint } from "@/components/PostCommitAdvancedAnalysisHint";
import { OperatorSectionRetryButton } from "@/components/OperatorSectionRetryButton";
import { GlossaryTooltip } from "@/components/GlossaryTooltip";
import { RunTraceViewerLink } from "@/components/RunTraceViewerLink";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader } from "@/components/ui/card";
import {
  type ApiResponseWithTrace,
  getBundleDownloadUrl,
  getManifestSummary,
  getRunDetail,
  getRunExplanationSummary,
  getRunExportDownloadUrl,
  getTraceabilityBundleDownloadUrl,
  getRunPipelineTimeline,
  getRunSummary,
  listArtifacts,
} from "@/lib/api";
import {
  tryStaticDemoArtifacts,
  tryStaticDemoManifestSummary,
  tryStaticDemoPipelineTimeline,
  tryStaticDemoRunDetail,
} from "@/lib/operator-static-demo";
import type {
  ArtifactDescriptor,
  ManifestSummary,
  PipelineTimelineItem,
  RunDetail,
  RunSummary,
} from "@/types/authority";
import type { RunExplanationSummary } from "@/types/explanation";

const sectionHeadingClass =
  "m-0 text-lg font-semibold tracking-tight text-neutral-900 border-b border-neutral-200 pb-2 dark:border-neutral-700 dark:text-neutral-100";

function ManifestSummarySection({
  manifestSummary,
}: {
  readonly manifestSummary: ManifestSummary;
}): ReactElement {
  return (
    <section id="manifest-summary" className="scroll-mt-24">
      <Card>
        <CardHeader>
          <h3 className={sectionHeadingClass}>Manifest summary</h3>
        </CardHeader>
        <CardContent className="space-y-4">
          {manifestSummary.operatorSummary ? (
            <p className="m-0 text-sm leading-relaxed text-neutral-600 dark:text-neutral-400">
              {manifestSummary.operatorSummary}
            </p>
          ) : null}
          <dl className="m-0 grid gap-3 sm:grid-cols-[minmax(8rem,auto)_1fr] sm:gap-x-6">
            <dt className="text-sm font-medium text-neutral-700 dark:text-neutral-300">Status</dt>
            <dd className="m-0 text-sm text-neutral-900 dark:text-neutral-100">
              {manifestStatusForDisplay(manifestSummary.status)}
            </dd>
            <dt className="text-sm font-medium text-neutral-700 dark:text-neutral-300">Rule set</dt>
            <dd className="m-0 text-sm text-neutral-900 dark:text-neutral-100">
              {manifestSummary.ruleSetId} {manifestSummary.ruleSetVersion}
            </dd>
            <dt className="text-sm font-medium text-neutral-700 dark:text-neutral-300">Decisions</dt>
            <dd className="m-0 text-sm text-neutral-900 dark:text-neutral-100">{manifestSummary.decisionCount}</dd>
            <dt className="text-sm font-medium text-neutral-700 dark:text-neutral-300">Warnings</dt>
            <dd className="m-0 text-sm text-neutral-900 dark:text-neutral-100">{manifestSummary.warningCount}</dd>
            <dt className="text-sm font-medium text-neutral-700 dark:text-neutral-300">Unresolved issues</dt>
            <dd className="m-0 text-sm text-neutral-900 dark:text-neutral-100">
              {manifestSummary.unresolvedIssueCount}
            </dd>
          </dl>
        </CardContent>
      </Card>
    </section>
  );
}

/** Server-rendered run detail page. Shows run metadata, authority chain, manifest summary, aggregate explanation, artifacts, and downloads. */
export default async function RunDetailPage({
  params,
}: {
  params: Promise<{ runId: string }>;
}) {
  const { runId } = await params;

  let runDetailResponse: ApiResponseWithTrace<RunDetail> | null = null;
  let loadFailure: ApiLoadFailureState | null = null;
  let usedStaticDemoRun = false;

  try {
    runDetailResponse = await getRunDetail(runId);
  } catch (e) {
    loadFailure = toApiLoadFailure(e);
    const fallback = tryStaticDemoRunDetail(runId);

    if (fallback !== null) {
      runDetailResponse = { data: fallback, traceId: null };
      loadFailure = null;
      usedStaticDemoRun = true;
    }
  }

  if (loadFailure || !runDetailResponse) {
    const fallback =
      loadFailure?.message ?? "Run not found or could not be loaded.";

    return (
      <main className="mx-auto max-w-4xl space-y-4 px-1 py-2 sm:px-0">
        <h1 className="text-xl font-semibold text-neutral-900 dark:text-neutral-100">Run detail</h1>
        <OperatorApiProblem
          problem={loadFailure?.problem ?? null}
          fallbackMessage={fallback}
          correlationId={loadFailure?.correlationId ?? null}
        />
        <p>
          <Link className="text-teal-800 underline dark:text-teal-300" href="/runs?projectId=default">
            ← Back to runs
          </Link>
        </p>
      </main>
    );
  }

  const envelope = coerceRunDetail(runDetailResponse.data);

  if (!envelope.ok) {
    return (
      <main className="mx-auto max-w-4xl space-y-4 px-1 py-2 sm:px-0">
        <h1 className="text-xl font-semibold text-neutral-900 dark:text-neutral-100">Run detail</h1>
        <OperatorMalformedCallout>
          <strong>Run detail response was not usable.</strong>
          <p className="mt-2">{envelope.message}</p>
          <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
            The run data could not be displayed. Try reloading.
          </p>
        </OperatorMalformedCallout>
        <p>
          <Link className="text-teal-800 underline dark:text-teal-300" href="/runs?projectId=default">
            ← Back to runs
          </Link>
        </p>
      </main>
    );
  }

  const resolvedDetail = envelope.value;
  const manifestId = resolvedDetail.run.goldenManifestId;
  const runDetailTraceId = runDetailResponse.traceId;

  let progressInitialSummary: RunSummary | null = null;

  try {
    progressInitialSummary = await getRunSummary(runId);
  } catch {
    progressInitialSummary = null;
  }

  const pipelineCompleteOnSummary = (s: RunSummary | null): boolean =>
    s !== null &&
    s.hasContextSnapshot === true &&
    s.hasGraphSnapshot === true &&
    s.hasFindingsSnapshot === true &&
    s.hasGoldenManifest === true;

  const progressForPipelineUi = effectiveRunSummaryForPipeline(progressInitialSummary, resolvedDetail.run);

  const showProgressTracker =
    !manifestId || !pipelineCompleteOnSummary(progressForPipelineUi);

  let manifestSummary: ManifestSummary | null = null;
  let artifacts: ArtifactDescriptor[] = [];
  let manifestSummaryFailure: ApiLoadFailureState | null = null;
  let manifestSummaryMalformed: string | null = null;
  let artifactsFailure: ApiLoadFailureState | null = null;
  let artifactsMalformed: string | null = null;
  let explanationSummary: RunExplanationSummary | null = null;
  let explanationFailure: ApiLoadFailureState | null = null;
  let pipelineTimeline: PipelineTimelineItem[] | null = null;
  let pipelineTimelineFailure: ApiLoadFailureState | null = null;

  try {
    pipelineTimeline = await getRunPipelineTimeline(runId);
  } catch (e) {
    pipelineTimelineFailure = toApiLoadFailure(e);

    if (usedStaticDemoRun) {
      const staticTimeline = tryStaticDemoPipelineTimeline(runId);

      if (staticTimeline !== null && staticTimeline.length > 0) {
        pipelineTimeline = staticTimeline;
        pipelineTimelineFailure = null;
      }
    }
  }

  if (manifestId) {
    try {
      const rawSummary: unknown = await getManifestSummary(manifestId);
      const coercedSummary = coerceManifestSummary(rawSummary);

      if (!coercedSummary.ok) {
        manifestSummaryMalformed = coercedSummary.message;
      } else {
        manifestSummary = coercedSummary.value;
      }
    } catch (e) {
      manifestSummaryFailure = toApiLoadFailure(e);
      const staticSummary = tryStaticDemoManifestSummary(manifestId);

      if (staticSummary !== null) {
        manifestSummary = staticSummary;
        manifestSummaryFailure = null;
      }
    }

    try {
      const rawArtifacts: unknown = await listArtifacts(manifestId);
      const coercedArtifacts = coerceArtifactDescriptorList(rawArtifacts);

      if (!coercedArtifacts.ok) {
        artifacts = [];
        artifactsMalformed = coercedArtifacts.message;
      } else {
        artifacts = coercedArtifacts.items;
      }
    } catch (e) {
      artifactsFailure = toApiLoadFailure(e);
      const staticArtifacts = tryStaticDemoArtifacts(runId, manifestId);

      if (staticArtifacts !== null) {
        artifacts = staticArtifacts;
        artifactsFailure = null;
      }
    }

    try {
      explanationSummary = await getRunExplanationSummary(runId);
    } catch (e) {
      explanationFailure = toApiLoadFailure(e);
    }
  }

  const runDetailNavSections: RunDetailSection[] = [
    { id: "run-metadata", label: "Run", available: true },
    { id: "pipeline-timeline", label: "Timeline", available: true },
    { id: "authority-chain", label: "Review trail", available: true },
    { id: "agent-forensics", label: "Diagnostics", available: true },
    { id: "manifest-summary", label: "Manifest", available: Boolean(manifestSummary) },
    { id: "run-explanation", label: "Explanation", available: Boolean(manifestId) },
    { id: "artifacts-exports", label: "Artifacts", available: Boolean(manifestId) },
    { id: "run-actions", label: "Actions", available: true },
  ];

  const runSummaryForBadge = progressForPipelineUi;
  const descriptionTrimmed = resolvedDetail.run.description?.trim() ?? "";
  const headline =
    descriptionTrimmed.length > 0 ? descriptionTrimmed : `Run ${resolvedDetail.run.runId}`;
  const createdLabel = new Date(resolvedDetail.run.createdUtc).toLocaleString();

  return (
    <main className="mx-auto max-w-4xl space-y-6 px-1 py-2 sm:px-0">
      <nav aria-label="Breadcrumb" className="text-sm text-neutral-600 dark:text-neutral-400">
        <Link className="text-teal-800 underline dark:text-teal-300" href="/">
          Home
        </Link>
        {" · "}
        <Link className="text-teal-800 underline dark:text-teal-300" href="/runs?projectId=default">
          Runs
        </Link>
        {" · "}
        <span className="font-medium text-neutral-800 dark:text-neutral-200" aria-current="page">
          {headline}
        </span>
      </nav>

      {usedStaticDemoRun ? <OperatorDemoStaticBanner /> : null}

      <RunDetailPageHeader
        runSummary={runSummaryForBadge}
        runId={resolvedDetail.run.runId}
        projectId={resolvedDetail.run.projectId}
        createdLabel={createdLabel}
        headline={headline}
        hasGoldenManifest={Boolean(manifestId)}
      />

      <RunDetailOutcomeCards
        runId={resolvedDetail.run.runId}
        artifactCount={artifacts.length}
        findingCountDisplay={explanationSummary?.findingCount ?? null}
        warningCountDisplay={manifestSummary?.warningCount ?? null}
        hasGoldenManifest={Boolean(manifestId)}
        unresolvedIssueCountDisplay={manifestSummary?.unresolvedIssueCount ?? null}
      />

      {showProgressTracker ? (
        <RunProgressTracker runId={runId} initialSummary={progressForPipelineUi} />
      ) : null}

      {manifestId ? <EmailRunToSponsorBanner runId={runId} /> : null}

      <RunDetailSectionNav sections={runDetailNavSections} />

      {manifestId && manifestSummary ? <ManifestSummarySection manifestSummary={manifestSummary} /> : null}

      <section id="run-metadata" className="scroll-mt-24">
        <Card>
          <CardHeader>
            <h3 className={sectionHeadingClass}>Run</h3>
            <CardDescription>
              Manifest summary and artifacts appear below when <GlossaryTooltip termKey="run">this run</GlossaryTooltip>{" "}
              has a <GlossaryTooltip termKey="golden_manifest">reviewed manifest</GlossaryTooltip> (after finalization).
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-3 text-sm text-neutral-700 dark:text-neutral-300">
            <RunTraceViewerLink traceId={runDetailTraceId} />
            {resolvedDetail.run.otelTraceId ? (
              <p className="m-0">
                <span className="font-medium text-neutral-800 dark:text-neutral-200">Creation trace:</span>{" "}
                <RunTraceViewerLink traceId={resolvedDetail.run.otelTraceId} />
              </p>
            ) : null}
            <p className="m-0">
              <span className="font-medium text-neutral-800 dark:text-neutral-200">Description:</span>{" "}
              {resolvedDetail.run.description ?? ""}
            </p>
          </CardContent>
        </Card>
      </section>

      <section id="pipeline-timeline" className="scroll-mt-24" aria-labelledby="pipeline-timeline-title">
        <Card>
          <CardHeader>
            <div className="mb-1 flex flex-wrap items-center gap-2">
              <h3 id="pipeline-timeline-title" className={sectionHeadingClass}>
                Pipeline timeline
              </h3>
              <ContextualHelp helpKey="run-pipeline-status" placement="right" />
            </div>
            <CardDescription>
              Audit events for this run, oldest first.
            </CardDescription>
          </CardHeader>
          <CardContent>
            {pipelineTimelineFailure ? (
              <>
                <AuthorityPipelineTimeline
                  items={null}
                  loadErrorMessage={pipelineTimelineFailure.message}
                />
                <OperatorSectionRetryButton label="Retry loading timeline" />
              </>
            ) : (
              <AuthorityPipelineTimeline items={pipelineTimeline} />
            )}
          </CardContent>
        </Card>
      </section>

      <section id="authority-chain" className="scroll-mt-24">
        <Card>
          <CardHeader>
            <h3 className={sectionHeadingClass}>Review trail</h3>
            <CardDescription>
              The reviewed manifest links to the finalized architecture record. Expand audit identifiers for traceability
              reference.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="rounded-lg border border-neutral-200 p-4 dark:border-neutral-800">
              <p className="m-0 text-sm font-medium text-neutral-800 dark:text-neutral-200">Reviewed manifest</p>
              <div className="mt-2 flex min-w-0 flex-wrap items-center justify-end gap-2 sm:justify-start">
                {manifestId ? (
                  <>
                    <Link
                      className="truncate font-mono text-xs text-teal-800 underline dark:text-teal-300"
                      href={`/manifests/${manifestId}`}
                    >
                      {manifestId}
                    </Link>
                    <CopyIdButton value={manifestId} aria-label="Copy reviewed manifest ID" />
                  </>
                ) : (
                  <span className="font-mono text-xs">—</span>
                )}
              </div>
            </div>

            <CollapsibleSection title="Audit identifiers" defaultOpen={false}>
              <ol className="m-0 list-none space-y-0 divide-y divide-neutral-200 p-0 dark:divide-neutral-800">
                <li className="flex flex-col gap-2 py-4 first:pt-0 sm:flex-row sm:items-center sm:justify-between">
                  <span className="shrink-0 text-sm font-medium text-neutral-800 dark:text-neutral-200">
                    <GlossaryTooltip termKey="context_snapshot">Context snapshot</GlossaryTooltip>
                  </span>
                  <span className="flex min-w-0 flex-1 items-center justify-end gap-2 sm:justify-end">
                    <code className="truncate font-mono text-xs text-neutral-700 dark:text-neutral-300">
                      {resolvedDetail.run.contextSnapshotId ?? "—"}
                    </code>
                    {resolvedDetail.run.contextSnapshotId ? (
                      <CopyIdButton value={resolvedDetail.run.contextSnapshotId} aria-label="Copy context snapshot ID" />
                    ) : null}
                  </span>
                </li>
                <li className="flex flex-col gap-2 py-4 sm:flex-row sm:items-center sm:justify-between">
                  <span className="shrink-0 text-sm font-medium text-neutral-800 dark:text-neutral-200">
                    Graph snapshot
                  </span>
                  <span className="flex min-w-0 flex-1 items-center justify-end gap-2">
                    <code className="truncate font-mono text-xs text-neutral-700 dark:text-neutral-300">
                      {resolvedDetail.run.graphSnapshotId ?? "—"}
                    </code>
                    {resolvedDetail.run.graphSnapshotId ? (
                      <CopyIdButton value={resolvedDetail.run.graphSnapshotId} aria-label="Copy graph snapshot ID" />
                    ) : null}
                  </span>
                </li>
                <li className="flex flex-col gap-2 py-4 sm:flex-row sm:items-center sm:justify-between">
                  <span className="shrink-0 text-sm font-medium text-neutral-800 dark:text-neutral-200">
                    Findings snapshot
                  </span>
                  <span className="flex min-w-0 flex-1 items-center justify-end gap-2">
                    <code className="truncate font-mono text-xs text-neutral-700 dark:text-neutral-300">
                      {resolvedDetail.run.findingsSnapshotId ?? "—"}
                    </code>
                    {resolvedDetail.run.findingsSnapshotId ? (
                      <CopyIdButton value={resolvedDetail.run.findingsSnapshotId} aria-label="Copy findings snapshot ID" />
                    ) : null}
                  </span>
                </li>
                <li className="flex flex-col gap-2 py-4 sm:flex-row sm:items-center sm:justify-between">
                  <span className="shrink-0 text-sm font-medium text-neutral-800 dark:text-neutral-200">
                    <GlossaryTooltip termKey="decision_trace">Decision trace</GlossaryTooltip>
                  </span>
                  <span className="flex min-w-0 flex-1 items-center justify-end gap-2">
                    <code className="truncate font-mono text-xs text-neutral-700 dark:text-neutral-300">
                      {resolvedDetail.run.decisionTraceId ?? "—"}
                    </code>
                    {resolvedDetail.run.decisionTraceId ? (
                      <CopyIdButton value={resolvedDetail.run.decisionTraceId} aria-label="Copy decision trace ID" />
                    ) : null}
                  </span>
                </li>
                <li className="flex flex-col gap-2 py-4 sm:flex-row sm:items-center sm:justify-between">
                  <span className="shrink-0 text-sm font-medium text-neutral-800 dark:text-neutral-200">
                    Artifact bundle
                  </span>
                  <span className="flex min-w-0 flex-1 items-center justify-end gap-2">
                    <code className="truncate font-mono text-xs text-neutral-700 dark:text-neutral-300">
                      {resolvedDetail.run.artifactBundleId ?? "—"}
                    </code>
                    {resolvedDetail.run.artifactBundleId ? (
                      <CopyIdButton value={resolvedDetail.run.artifactBundleId} aria-label="Copy artifact bundle ID" />
                    ) : null}
                  </span>
                </li>
              </ol>
            </CollapsibleSection>
          </CardContent>
        </Card>
      </section>

      <RunAgentForensicsSection runId={runId} />

      {!manifestId && (
        <OperatorEmptyState title="Manifest review not available yet">
          <p className="m-0">
            This run has not been finalized yet. Once the pipeline completes and the run is finalized, the
            manifest, artifacts, and exports will appear here.
          </p>
        </OperatorEmptyState>
      )}

      {manifestSummaryFailure && (
        <div className="space-y-2">
          <p className="m-0 text-sm font-semibold text-neutral-800 dark:text-neutral-200">
            Manifest summary could not be loaded.
          </p>
          <OperatorApiProblem
            problem={manifestSummaryFailure.problem}
            fallbackMessage={manifestSummaryFailure.message}
            correlationId={manifestSummaryFailure.correlationId}
            variant="warning"
          />
          <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
            This is a failed request (HTTP / transport / 404), not a malformed JSON body.
          </p>
          <OperatorSectionRetryButton label="Retry loading manifest summary" />
        </div>
      )}

      {manifestSummaryMalformed && (
        <OperatorMalformedCallout>
          <strong>Manifest summary response was not usable.</strong>
          <p className="mt-2">{manifestSummaryMalformed}</p>
        </OperatorMalformedCallout>
      )}

      {manifestId ? <PostCommitAdvancedAnalysisHint runId={runId} /> : null}

      {manifestId && (
        <section id="run-explanation" className="scroll-mt-24">
          <CollapsibleSection title="Explanation (aggregate)" defaultOpen={false}>
            {explanationFailure && (
              <>
                <p className="m-0 mb-2 text-sm font-semibold text-neutral-800 dark:text-neutral-200">
                  Aggregate explanation could not be loaded.
                </p>
                <OperatorApiProblem
                  problem={explanationFailure.problem}
                  fallbackMessage={explanationFailure.message}
                  correlationId={explanationFailure.correlationId}
                  variant="warning"
                />
                <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
                  The run and manifest loaded, but the explanation aggregate request failed (HTTP / transport / 404).
                </p>
                <OperatorSectionRetryButton label="Retry loading explanation" />
              </>
            )}
            {!explanationFailure && (
              <>
                <RunExplanationSection summary={explanationSummary} loading={false} error={null} runId={runId} />
                {(() => {
                  const traceRows =
                    explanationSummary?.findingTraceConfidences ??
                    explanationSummary?.explanation?.findingTraceConfidences ??
                    [];

                  if (traceRows.length === 0) {
                    return null;
                  }

                  return <RunFindingExplainabilityTable runId={runId} rows={traceRows} />;
                })()}
              </>
            )}
          </CollapsibleSection>
        </section>
      )}

      {manifestId && <BeforeAfterDeltaPanel variant="inline" runId={runId} />}

      {manifestId && (
        <section id="artifacts-exports" className="scroll-mt-24">
          <div className="relative overflow-visible pr-9 sm:pr-10">
            <div className="absolute end-0 top-0 z-10 sm:end-1 sm:top-1">
              <ContextualHelp helpKey="manifest-review" placement="left" />
            </div>
            <CollapsibleSection title="Artifacts & exports" defaultOpen>
              {artifactsFailure && (
                <>
                  <p className="m-0 mb-2 text-sm font-semibold text-neutral-800 dark:text-neutral-200">
                    Artifact list could not be loaded.
                  </p>
                  <OperatorApiProblem
                    problem={artifactsFailure.problem}
                    fallbackMessage={artifactsFailure.message}
                    correlationId={artifactsFailure.correlationId}
                    variant="warning"
                  />
                  <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
                    The artifacts request failed (network, 404, or server error)—distinct from an empty
                    list or malformed JSON.
                  </p>
                  <OperatorSectionRetryButton label="Retry loading artifacts" />
                </>
              )}

              {!artifactsFailure && artifactsMalformed && (
                <OperatorMalformedCallout>
                  <strong>Artifact list response was not usable.</strong>
                  <p className="mt-2">{artifactsMalformed}</p>
                </OperatorMalformedCallout>
              )}

              {!artifactsFailure && !artifactsMalformed && artifacts.length === 0 && (
                <OperatorEmptyState title="No artifacts for this manifest">
                  <p className="m-0">
                    The manifest exists but the artifact descriptor list is empty (valid empty result).
                    Bundle ZIP may return 404 when there is no bundle; run export may still include other
                    files.
                  </p>
                </OperatorEmptyState>
              )}

              {!artifactsFailure && !artifactsMalformed && artifacts.length > 0 && (
                <ArtifactListTable
                  manifestId={manifestId}
                  artifacts={artifacts}
                  runId={resolvedDetail.run.runId}
                />
              )}

              <div className="mt-4 flex flex-wrap gap-3">
                <Button variant="outline" size="sm" asChild>
                  <a href={getBundleDownloadUrl(manifestId)}>Download bundle (ZIP)</a>
                </Button>
                <Button variant="outline" size="sm" asChild>
                  <a href={getRunExportDownloadUrl(resolvedDetail.run.runId)}>Download run export (ZIP)</a>
                </Button>
              </div>
            </CollapsibleSection>
          </div>
        </section>
      )}

      <section id="run-actions" className="scroll-mt-24">
        <Card>
          <CardHeader>
            <h3 className={sectionHeadingClass}>Actions</h3>
            <CardDescription>Secondary downloads and sponsor collateral.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            {manifestId ? (
              <div className="max-w-xl space-y-2">
                <p className="m-0 text-sm font-medium text-neutral-800 dark:text-neutral-200">Sponsor collateral</p>
                <GenerateSponsorValueReportButton />
              </div>
            ) : null}
            <div className="flex flex-wrap gap-3">
              <Button variant="secondary" size="sm" asChild>
                <a href={getTraceabilityBundleDownloadUrl(resolvedDetail.run.runId)}>
                  Download traceability bundle (ZIP)
                </a>
              </Button>
              <Button variant="outline" size="sm" asChild>
                <Link href={`/compare?leftRunId=${encodeURIComponent(resolvedDetail.run.runId)}`}>
                  Compare two runs (base = this run)
                </Link>
              </Button>
              <Button variant="outline" size="sm" asChild>
                <Link href={`/replay?runId=${encodeURIComponent(resolvedDetail.run.runId)}`}>Replay this run</Link>
              </Button>
            </div>
          </CardContent>
        </Card>
      </section>
    </main>
  );
}

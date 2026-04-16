import Link from "next/link";

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
import { ArtifactListTable } from "@/components/ArtifactListTable";
import { AuthorityPipelineTimeline } from "@/components/AuthorityPipelineTimeline";
import { CollapsibleSection } from "@/components/CollapsibleSection";
import { RunExplanationSection } from "@/components/RunExplanationSection";
import { RunFindingExplainabilityTable } from "@/components/RunFindingExplainabilityTable";
import { RunDetailSectionNav, type RunDetailSection } from "@/components/RunDetailSectionNav";
import { RunProgressTracker } from "@/components/RunProgressTracker";
import { RunAgentForensicsSection } from "@/components/RunAgentForensicsSection";
import { CommitRunButton } from "@/components/CommitRunButton";
import { OperatorSectionRetryButton } from "@/components/OperatorSectionRetryButton";
import { RunTraceViewerLink } from "@/components/RunTraceViewerLink";
import {
  type ApiResponseWithTrace,
  getBundleDownloadUrl,
  getManifestSummary,
  getRunDetail,
  getRunExplanationSummary,
  getRunExportDownloadUrl,
  getRunPipelineTimeline,
  getRunSummary,
  listArtifacts,
} from "@/lib/api";
import type {
  ArtifactDescriptor,
  ManifestSummary,
  PipelineTimelineItem,
  RunDetail,
  RunSummary,
} from "@/types/authority";
import type { RunExplanationSummary } from "@/types/explanation";

/** Server-rendered run detail page. Shows run metadata, authority chain, manifest summary, aggregate explanation, artifacts, and downloads. */
export default async function RunDetailPage({
  params,
}: {
  params: Promise<{ runId: string }>;
}) {
  const { runId } = await params;

  let runDetailResponse: ApiResponseWithTrace<RunDetail> | null = null;
  let loadFailure: ApiLoadFailureState | null = null;

  try {
    runDetailResponse = await getRunDetail(runId);
  } catch (e) {
    loadFailure = toApiLoadFailure(e);
  }

  if (loadFailure || !runDetailResponse) {
    const fallback =
      loadFailure?.message ?? "Run not found or could not be loaded.";

    return (
      <main>
        <h2>Run detail</h2>
        <OperatorApiProblem
          problem={loadFailure?.problem ?? null}
          fallbackMessage={fallback}
          correlationId={loadFailure?.correlationId ?? null}
        />
        <p style={{ margin: "12px 0 0", fontSize: 14 }}>
          This indicates a failed request or missing run (HTTP / transport), not a JSON shape issue.
        </p>
        <p>
          <Link href="/runs?projectId=default">← Back to runs</Link>
        </p>
      </main>
    );
  }

  const envelope = coerceRunDetail(runDetailResponse.data);

  if (!envelope.ok) {
    return (
      <main>
        <h2>Run detail</h2>
        <OperatorMalformedCallout>
          <strong>Run detail response was not usable.</strong>
          <p style={{ margin: "8px 0 0" }}>{envelope.message}</p>
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>
            The API returned a body, but it did not match the expected run envelope. Compare UI and
            API versions.
          </p>
        </OperatorMalformedCallout>
        <p>
          <Link href="/runs?projectId=default">← Back to runs</Link>
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

  const showProgressTracker =
    !manifestId || !pipelineCompleteOnSummary(progressInitialSummary);

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
    { id: "agent-forensics", label: "Forensics", available: true },
    { id: "authority-chain", label: "Authority", available: true },
    { id: "manifest-summary", label: "Manifest", available: Boolean(manifestSummary) },
    { id: "run-explanation", label: "Explanation", available: Boolean(manifestId) },
    { id: "artifacts-exports", label: "Artifacts", available: Boolean(manifestId) },
    { id: "run-actions", label: "Actions", available: true },
  ];

  return (
    <main>
      <h2>Run detail</h2>
      <p className="text-sm text-neutral-600 dark:text-neutral-400">
        <Link href="/">Home</Link>
        {" · "}
        <Link href="/runs?projectId=default">Runs</Link>
        {" · "}
        <Link href="/graph">Graph</Link>
        {" · "}
        <Link href="/compare">Compare two runs</Link>
        {" · "}
        <Link href={`/runs/${runId}/provenance`}>Provenance</Link>
      </p>

      {showProgressTracker ? (
        <RunProgressTracker runId={runId} initialSummary={progressInitialSummary} />
      ) : null}

      <RunDetailSectionNav sections={runDetailNavSections} />

      <section id="run-metadata" style={{ marginBottom: 24 }}>
        <h3>Run</h3>
        <p style={{ fontSize: 14, color: "#64748b", marginTop: 0, maxWidth: 720 }}>
          Manifest summary and artifacts appear below when this run has a golden manifest (after commit).
        </p>
        <p>
          <strong>Run ID:</strong>{" "}
          <code className="rounded bg-neutral-100 px-1 py-0.5 text-xs dark:bg-neutral-800">
            {resolvedDetail.run.runId}
          </code>
        </p>
        <RunTraceViewerLink traceId={runDetailTraceId} />
        {resolvedDetail.run.otelTraceId && (
          <p>
            <strong>Creation trace:</strong>{" "}
            <RunTraceViewerLink traceId={resolvedDetail.run.otelTraceId} />
          </p>
        )}
        <p>
          <strong>Project:</strong> {resolvedDetail.run.projectId}
        </p>
        <p>
          <strong>Description:</strong> {resolvedDetail.run.description ?? ""}
        </p>
        <p>
          <strong>Created:</strong> {new Date(resolvedDetail.run.createdUtc).toLocaleString()}
        </p>
      </section>

      <section id="pipeline-timeline" style={{ marginBottom: 24 }} aria-labelledby="pipeline-timeline-title">
        <h3 id="pipeline-timeline-title">Pipeline timeline</h3>
        <p style={{ fontSize: 14, color: "#64748b", marginTop: 0, maxWidth: 720 }}>
          Audit events associated with this run (oldest first). Empty lists are normal when auditing is sparse or the run
          was created outside the authority pipeline.
        </p>
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
      </section>

      <RunAgentForensicsSection runId={runId} />

      <section id="authority-chain" style={{ marginBottom: 24 }}>
        <h3>Authority chain</h3>
        <ul>
          <li>Context Snapshot: {resolvedDetail.run.contextSnapshotId ?? "—"}</li>
          <li>Graph Snapshot: {resolvedDetail.run.graphSnapshotId ?? "—"}</li>
          <li>Findings Snapshot: {resolvedDetail.run.findingsSnapshotId ?? "—"}</li>
          <li>
            Golden Manifest:{" "}
            {manifestId ? (
              <Link href={`/manifests/${manifestId}`}>{manifestId}</Link>
            ) : (
              "—"
            )}
          </li>
          <li>Decision Trace: {resolvedDetail.run.decisionTraceId ?? "—"}</li>
          <li>Artifact Bundle: {resolvedDetail.run.artifactBundleId ?? "—"}</li>
        </ul>
      </section>

      {!manifestId && (
        <OperatorEmptyState title="Manifest review not available yet">
          <p style={{ margin: 0 }}>
            This run has no <strong>golden manifest</strong> yet (normal before commit). After the pipeline
            finishes, commit through the <strong>API or CLI</strong>, then reload this page for manifest summary,
            artifacts, and ZIP exports.
          </p>
          <ol style={{ margin: "12px 0 0", paddingLeft: 20, fontSize: 14, color: "#475569", lineHeight: 1.6 }}>
            <li>Confirm authority chain items above are populated (snapshots processing).</li>
            <li>Commit when ready — examples in <code>docs/OPERATOR_QUICKSTART.md</code>.</li>
            <li>Reload run detail; the manifest link and Artifacts section will appear.</li>
          </ol>
        </OperatorEmptyState>
      )}

      {manifestSummaryFailure && (
        <>
          <p style={{ margin: "0 0 8px", fontSize: 14, fontWeight: 600 }}>
            Manifest summary could not be loaded.
          </p>
          <OperatorApiProblem
            problem={manifestSummaryFailure.problem}
            fallbackMessage={manifestSummaryFailure.message}
            correlationId={manifestSummaryFailure.correlationId}
            variant="warning"
          />
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>
            This is a failed request (HTTP / transport / 404), not a malformed JSON body.
          </p>
          <OperatorSectionRetryButton label="Retry loading manifest summary" />
        </>
      )}

      {manifestSummaryMalformed && (
        <OperatorMalformedCallout>
          <strong>Manifest summary response was not usable.</strong>
          <p style={{ margin: "8px 0 0" }}>{manifestSummaryMalformed}</p>
        </OperatorMalformedCallout>
      )}

      {manifestSummary && (
        <section id="manifest-summary" style={{ marginBottom: 24 }}>
          <h3>Manifest summary</h3>
          {manifestSummary.operatorSummary && (
            <p style={{ margin: "0 0 12px", fontSize: 14, color: "#475569", lineHeight: 1.5 }}>
              {manifestSummary.operatorSummary}
            </p>
          )}
          <p>
            <strong>Status:</strong> {manifestSummary.status}
          </p>
          <p>
            <strong>Rule set:</strong> {manifestSummary.ruleSetId} {manifestSummary.ruleSetVersion}
          </p>
          <p>
            <strong>Decisions:</strong> {manifestSummary.decisionCount}
          </p>
          <p>
            <strong>Warnings:</strong> {manifestSummary.warningCount}
          </p>
          <p>
            <strong>Unresolved issues:</strong> {manifestSummary.unresolvedIssueCount}
          </p>
        </section>
      )}

      {manifestId && (
        <section id="run-explanation" className="scroll-mt-20">
        <CollapsibleSection title="Explanation (aggregate)" defaultOpen={false}>
          {explanationFailure && (
            <>
              <p style={{ margin: "0 0 8px", fontSize: 14, fontWeight: 600 }}>
                Aggregate explanation could not be loaded.
              </p>
              <OperatorApiProblem
                problem={explanationFailure.problem}
                fallbackMessage={explanationFailure.message}
                correlationId={explanationFailure.correlationId}
                variant="warning"
              />
              <p style={{ margin: "8px 0 0", fontSize: 14 }}>
                The run and manifest loaded, but the explanation aggregate request failed (HTTP / transport / 404).
              </p>
              <OperatorSectionRetryButton label="Retry loading explanation" />
            </>
          )}
          {!explanationFailure && (
            <>
              <RunExplanationSection summary={explanationSummary} loading={false} error={null} />
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

      {manifestId && (
        <section id="artifacts-exports" className="scroll-mt-20">
        <CollapsibleSection title="Artifacts & exports" defaultOpen>
          {artifactsFailure && (
            <>
              <p style={{ margin: "0 0 8px", fontSize: 14, fontWeight: 600 }}>
                Artifact list could not be loaded.
              </p>
              <OperatorApiProblem
                problem={artifactsFailure.problem}
                fallbackMessage={artifactsFailure.message}
                correlationId={artifactsFailure.correlationId}
                variant="warning"
              />
              <p style={{ margin: "8px 0 0", fontSize: 14 }}>
                The artifacts request failed (network, 404, or server error)—distinct from an empty
                list or malformed JSON.
              </p>
              <OperatorSectionRetryButton label="Retry loading artifacts" />
            </>
          )}

          {!artifactsFailure && artifactsMalformed && (
            <OperatorMalformedCallout>
              <strong>Artifact list response was not usable.</strong>
              <p style={{ margin: "8px 0 0" }}>{artifactsMalformed}</p>
            </OperatorMalformedCallout>
          )}

          {!artifactsFailure && !artifactsMalformed && artifacts.length === 0 && (
            <OperatorEmptyState title="No artifacts for this manifest">
              <p style={{ margin: 0 }}>
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

          <div style={{ display: "flex", gap: 16, marginTop: 12, flexWrap: "wrap" }}>
            <a href={getBundleDownloadUrl(manifestId)}>Download bundle (ZIP)</a>
            <a href={getRunExportDownloadUrl(resolvedDetail.run.runId)}>Download run export (ZIP)</a>
          </div>
        </CollapsibleSection>
        </section>
      )}

      <section id="run-actions" style={{ marginBottom: 24 }}>
        <h3>Actions</h3>
        <div className="mb-4 max-w-xl">
          <p className="m-0 mb-2 text-sm font-medium text-neutral-800 dark:text-neutral-200">Commit</p>
          <CommitRunButton runId={runId} disabled={Boolean(manifestId)} />
        </div>
        <div style={{ display: "flex", gap: 16, flexWrap: "wrap" }}>
          <Link href={`/compare?leftRunId=${encodeURIComponent(resolvedDetail.run.runId)}`}>
            Compare two runs (base = this run)
          </Link>
          <Link href={`/replay?runId=${encodeURIComponent(resolvedDetail.run.runId)}`}>Replay this run</Link>
        </div>
      </section>
    </main>
  );
}

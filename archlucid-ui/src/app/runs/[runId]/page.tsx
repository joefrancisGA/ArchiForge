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
import { RunTraceViewerLink } from "@/components/RunTraceViewerLink";
import {
  type ApiResponseWithTrace,
  getBundleDownloadUrl,
  getManifestSummary,
  getRunDetail,
  getRunExportDownloadUrl,
  listArtifacts,
} from "@/lib/api";
import type { ArtifactDescriptor, ManifestSummary, RunDetail } from "@/types/authority";

/** Server-rendered run detail page. Shows run metadata, authority chain, manifest summary, artifacts, and downloads. */
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

  let manifestSummary: ManifestSummary | null = null;
  let artifacts: ArtifactDescriptor[] = [];
  let manifestSummaryFailure: ApiLoadFailureState | null = null;
  let manifestSummaryMalformed: string | null = null;
  let artifactsFailure: ApiLoadFailureState | null = null;
  let artifactsMalformed: string | null = null;

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
  }

  return (
    <main>
      <h2>Run detail</h2>
      <p style={{ fontSize: 14 }}>
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

      <section style={{ marginBottom: 24 }}>
        <h3>Run</h3>
        <p style={{ fontSize: 14, color: "#64748b", marginTop: 0, maxWidth: 720 }}>
          Manifest summary and artifacts appear below when this run has a golden manifest (after commit).
        </p>
        <p>
          <strong>Run ID:</strong> {resolvedDetail.run.runId}
        </p>
        <RunTraceViewerLink traceId={runDetailTraceId} />
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

      <section style={{ marginBottom: 24 }}>
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
        </>
      )}

      {manifestSummaryMalformed && (
        <OperatorMalformedCallout>
          <strong>Manifest summary response was not usable.</strong>
          <p style={{ margin: "8px 0 0" }}>{manifestSummaryMalformed}</p>
        </OperatorMalformedCallout>
      )}

      {manifestSummary && (
        <section style={{ marginBottom: 24 }}>
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
        <section style={{ marginBottom: 24 }}>
          <h3>Artifacts</h3>

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
        </section>
      )}

      <section style={{ marginBottom: 24 }}>
        <h3>Actions</h3>
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

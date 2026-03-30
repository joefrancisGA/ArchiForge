import Link from "next/link";

import {
  OperatorEmptyState,
  OperatorErrorCallout,
  OperatorWarningCallout,
} from "@/components/OperatorShellMessage";
import {
  getArtifactDownloadUrl,
  getBundleDownloadUrl,
  getManifestSummary,
  getRunDetail,
  getRunExportDownloadUrl,
  listArtifacts,
} from "@/lib/api";
import type { ArtifactDescriptor, ManifestSummary } from "@/types/authority";

export default async function RunDetailPage({
  params,
}: {
  params: Promise<{ runId: string }>;
}) {
  const { runId } = await params;

  let detail: Awaited<ReturnType<typeof getRunDetail>> | null = null;
  let loadError: string | null = null;

  try {
    detail = await getRunDetail(runId);
  } catch (e) {
    loadError = e instanceof Error ? e.message : "Failed to load run.";
  }

  if (loadError || !detail) {
    return (
      <main>
        <h2>Run detail</h2>
        <OperatorErrorCallout>
          <strong>Run unavailable.</strong>
          <p style={{ margin: "8px 0 0" }}>
            {loadError ?? "Run not found or could not be loaded."}
          </p>
        </OperatorErrorCallout>
        <p>
          <Link href="/runs?projectId=default">← Back to runs</Link>
        </p>
      </main>
    );
  }

  const manifestId = detail.run.goldenManifestId;

  let manifestSummary: ManifestSummary | null = null;
  let artifacts: ArtifactDescriptor[] = [];
  let manifestSummaryError: string | null = null;
  let artifactsError: string | null = null;

  if (manifestId) {
    try {
      manifestSummary = await getManifestSummary(manifestId);
    } catch (e) {
      manifestSummaryError =
        e instanceof Error ? e.message : "Could not load manifest summary.";
    }

    try {
      artifacts = await listArtifacts(manifestId);
    } catch (e) {
      artifactsError = e instanceof Error ? e.message : "Could not load artifact list.";
    }
  }

  return (
    <main>
      <h2>Run detail</h2>
      <p>
        <Link href="/runs?projectId=default">← Runs</Link>
      </p>

      <section style={{ marginBottom: 24 }}>
        <h3>Run</h3>
        <p>
          <strong>Run ID:</strong> {detail.run.runId}
        </p>
        <p>
          <strong>Project:</strong> {detail.run.projectId}
        </p>
        <p>
          <strong>Description:</strong> {detail.run.description ?? ""}
        </p>
        <p>
          <strong>Created:</strong> {new Date(detail.run.createdUtc).toLocaleString()}
        </p>
      </section>

      <section style={{ marginBottom: 24 }}>
        <h3>Authority chain</h3>
        <ul>
          <li>Context Snapshot: {detail.run.contextSnapshotId ?? "—"}</li>
          <li>Graph Snapshot: {detail.run.graphSnapshotId ?? "—"}</li>
          <li>Findings Snapshot: {detail.run.findingsSnapshotId ?? "—"}</li>
          <li>
            Golden Manifest:{" "}
            {manifestId ? (
              <Link href={`/manifests/${manifestId}`}>{manifestId}</Link>
            ) : (
              "—"
            )}
          </li>
          <li>Decision Trace: {detail.run.decisionTraceId ?? "—"}</li>
          <li>Artifact Bundle: {detail.run.artifactBundleId ?? "—"}</li>
        </ul>
      </section>

      {!manifestId && (
        <OperatorEmptyState title="Manifest review not available yet">
          <p style={{ margin: 0 }}>
            This run has no <strong>golden manifest</strong> (typically before commit). Commit the run
            through the API or CLI to attach a manifest, then reload for summary, artifacts, and
            downloads.
          </p>
        </OperatorEmptyState>
      )}

      {manifestSummaryError && (
        <OperatorWarningCallout>
          <strong>Manifest summary could not be loaded.</strong>
          <p style={{ margin: "8px 0 0" }}>{manifestSummaryError}</p>
        </OperatorWarningCallout>
      )}

      {manifestSummary && (
        <section style={{ marginBottom: 24 }}>
          <h3>Manifest summary</h3>
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

          {artifactsError && (
            <OperatorWarningCallout>
              <strong>Artifact list could not be loaded.</strong>
              <p style={{ margin: "8px 0 0" }}>{artifactsError}</p>
            </OperatorWarningCallout>
          )}

          {!artifactsError && artifacts.length === 0 && (
            <OperatorEmptyState title="No artifacts for this manifest">
              <p style={{ margin: 0 }}>
                The manifest exists but no artifact descriptors were returned. Bundle/export links may
                still be available below if the API recorded a bundle.
              </p>
            </OperatorEmptyState>
          )}

          {artifacts.length > 0 && (
            <ul>
              {artifacts.map((artifact) => (
                <li key={artifact.artifactId}>
                  {artifact.name} ({artifact.artifactType}) —{" "}
                  <a href={getArtifactDownloadUrl(manifestId, artifact.artifactId)}>Download</a>
                </li>
              ))}
            </ul>
          )}

          <div style={{ display: "flex", gap: 16, marginTop: 12, flexWrap: "wrap" }}>
            <a href={getBundleDownloadUrl(manifestId)}>Download bundle (ZIP)</a>
            <a href={getRunExportDownloadUrl(detail.run.runId)}>Download run export (ZIP)</a>
          </div>
        </section>
      )}

      <section style={{ marginBottom: 24 }}>
        <h3>Actions</h3>
        <div style={{ display: "flex", gap: 16, flexWrap: "wrap" }}>
          <Link href={`/compare?leftRunId=${encodeURIComponent(detail.run.runId)}`}>Compare</Link>
          <Link href={`/replay?runId=${encodeURIComponent(detail.run.runId)}`}>Replay</Link>
        </div>
      </section>
    </main>
  );
}

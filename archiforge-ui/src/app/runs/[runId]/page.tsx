import Link from "next/link";
import {
  getArtifactDownloadUrl,
  getBundleDownloadUrl,
  getManifestSummary,
  getRunDetail,
  getRunExportDownloadUrl,
  listArtifacts,
} from "@/lib/api";

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
        <h2>Run Detail</h2>
        <p style={{ color: "crimson" }}>{loadError ?? "Run not found."}</p>
        <Link href="/runs?projectId=default">Back to runs</Link>
      </main>
    );
  }

  const manifestId = detail.run.goldenManifestId;

  let manifestSummary: Awaited<ReturnType<typeof getManifestSummary>> | null = null;
  let artifacts: Awaited<ReturnType<typeof listArtifacts>> = [];
  try {
    if (manifestId) {
      manifestSummary = await getManifestSummary(manifestId);
      artifacts = await listArtifacts(manifestId);
    }
  } catch {
    /* manifest / artifacts optional */
  }

  return (
    <main>
      <h2>Run Detail</h2>
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
          <li>Context Snapshot: {detail.run.contextSnapshotId ?? "N/A"}</li>
          <li>Graph Snapshot: {detail.run.graphSnapshotId ?? "N/A"}</li>
          <li>Findings Snapshot: {detail.run.findingsSnapshotId ?? "N/A"}</li>
          <li>
            Golden Manifest:{" "}
            {manifestId ? (
              <Link href={`/manifests/${manifestId}`}>{manifestId}</Link>
            ) : (
              "N/A"
            )}
          </li>
          <li>Decision Trace: {detail.run.decisionTraceId ?? "N/A"}</li>
          <li>Artifact Bundle: {detail.run.artifactBundleId ?? "N/A"}</li>
        </ul>
      </section>

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
          <ul>
            {artifacts.map((artifact) => (
              <li key={artifact.artifactId}>
                {artifact.name} ({artifact.artifactType}) —{" "}
                <a href={getArtifactDownloadUrl(manifestId, artifact.artifactId)}>Download</a>
              </li>
            ))}
          </ul>

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

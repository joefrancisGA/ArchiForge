import Link from "next/link";
import { getArtifactDownloadUrl, getBundleDownloadUrl, getManifestSummary, listArtifacts } from "@/lib/api";

export default async function ManifestDetailPage({
  params,
}: {
  params: Promise<{ manifestId: string }>;
}) {
  const { manifestId } = await params;

  let summary: Awaited<ReturnType<typeof getManifestSummary>> | null = null;
  let artifacts: Awaited<ReturnType<typeof listArtifacts>> = [];
  let loadError: string | null = null;
  try {
    summary = await getManifestSummary(manifestId);
    artifacts = await listArtifacts(manifestId);
  } catch (e) {
    loadError = e instanceof Error ? e.message : "Failed to load manifest.";
  }

  if (loadError || !summary) {
    return (
      <main>
        <h2>Manifest</h2>
        <p style={{ color: "crimson" }}>{loadError ?? "Not found."}</p>
        <Link href="/runs?projectId=default">Runs</Link>
      </main>
    );
  }

  return (
    <main>
      <h2>Manifest</h2>
      <p>
        <Link href={`/runs/${summary.runId}`}>← Run {summary.runId}</Link>
      </p>
      <p>
        <strong>Manifest ID:</strong> {summary.manifestId}
      </p>
      <p>
        <strong>Status:</strong> {summary.status}
      </p>
      <p>
        <strong>Rule set:</strong> {summary.ruleSetId} {summary.ruleSetVersion}
      </p>
      <p>
        <strong>Manifest hash:</strong>{" "}
        <span style={{ fontFamily: "monospace", fontSize: 13 }}>{summary.manifestHash}</span>
      </p>
      <p>
        <strong>Decisions:</strong> {summary.decisionCount}
      </p>
      <p>
        <strong>Warnings:</strong> {summary.warningCount}
      </p>
      <p>
        <strong>Unresolved issues:</strong> {summary.unresolvedIssueCount}
      </p>

      <h3>Artifacts</h3>
      <p>
        <a href={getBundleDownloadUrl(manifestId)}>Download bundle (ZIP)</a>
      </p>
      <ul>
        {artifacts.map((artifact) => (
          <li key={artifact.artifactId}>
            {artifact.name} ({artifact.artifactType}) —{" "}
            <a href={getArtifactDownloadUrl(manifestId, artifact.artifactId)}>Download</a>
          </li>
        ))}
      </ul>
    </main>
  );
}

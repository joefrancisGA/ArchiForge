import Link from "next/link";

import type { ArtifactDescriptor } from "@/types/authority";
import { getArtifactDownloadUrl } from "@/lib/api";
import { getArtifactTypeLabel } from "@/lib/artifact-review-helpers";

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleString();
  } catch {
    return iso;
  }
}

function reviewHrefForArtifact(
  manifestId: string,
  artifactId: string,
  runId: string | undefined,
): string {
  if (runId) {
    return `/runs/${encodeURIComponent(runId)}/artifacts/${encodeURIComponent(artifactId)}`;
  }

  return `/manifests/${encodeURIComponent(manifestId)}/artifacts/${encodeURIComponent(artifactId)}`;
}

/**
 * Deterministic artifact list for run and manifest pages (review + download).
 */
export function ArtifactListTable(props: {
  manifestId: string;
  artifacts: ArtifactDescriptor[];
  /** When set, the matching row is visually emphasized (e.g. on artifact review page). */
  currentArtifactId?: string;
  /**
   * When set, Review links use /runs/.../artifacts/... (redirects to manifest-scoped review).
   * Improves run-centric navigation from run detail.
   */
  runId?: string;
}) {
  const { manifestId, artifacts, currentArtifactId, runId } = props;
  const sorted = [...artifacts].sort((a, b) => a.name.localeCompare(b.name, undefined, { sensitivity: "base" }));

  return (
    <div style={{ overflowX: "auto" }}>
      <table style={{ borderCollapse: "collapse", width: "100%", fontSize: 14 }}>
        <thead>
          <tr style={{ textAlign: "left", borderBottom: "1px solid #ccc" }}>
            <th style={{ padding: "10px 8px" }}>Artifact</th>
            <th style={{ padding: "10px 8px" }}>Type</th>
            <th style={{ padding: "10px 8px" }}>Format</th>
            <th style={{ padding: "10px 8px" }}>Created</th>
            <th style={{ padding: "10px 8px" }}>Hash (short)</th>
            <th style={{ padding: "10px 8px" }}>Actions</th>
          </tr>
        </thead>
        <tbody>
          {sorted.map((artifact) => {
            const reviewHref = reviewHrefForArtifact(manifestId, artifact.artifactId, runId);
            const hashShort =
              artifact.contentHash.length > 12
                ? `${artifact.contentHash.slice(0, 8)}…`
                : artifact.contentHash;

            const isCurrent =
              currentArtifactId !== undefined && currentArtifactId === artifact.artifactId;

            return (
              <tr
                key={artifact.artifactId}
                style={{
                  borderBottom: "1px solid #eee",
                  background: isCurrent ? "#eff6ff" : undefined,
                }}
              >
                <td style={{ padding: "10px 8px", maxWidth: 280 }}>
                  <strong style={{ fontWeight: 600 }}>{artifact.name}</strong>
                </td>
                <td style={{ padding: "10px 8px", color: "#444" }}>
                  {getArtifactTypeLabel(artifact.artifactType)}
                </td>
                <td style={{ padding: "10px 8px", fontFamily: "monospace", fontSize: 13 }}>
                  {artifact.format}
                </td>
                <td style={{ padding: "10px 8px", whiteSpace: "nowrap", color: "#555" }}>
                  {formatDate(artifact.createdUtc)}
                </td>
                <td style={{ padding: "10px 8px", fontFamily: "monospace", fontSize: 12 }} title={artifact.contentHash}>
                  {hashShort}
                </td>
                <td style={{ padding: "10px 8px" }}>
                  <Link href={reviewHref}>Review</Link>
                  <span style={{ margin: "0 8px", color: "#ccc" }}>|</span>
                  <a href={getArtifactDownloadUrl(manifestId, artifact.artifactId)}>Download</a>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

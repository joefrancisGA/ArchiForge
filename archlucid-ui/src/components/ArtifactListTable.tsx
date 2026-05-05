import Link from "next/link";

import { ProductLearningFeedbackControls } from "@/components/ProductLearningFeedbackControls";
import type { ArtifactDescriptor } from "@/types/authority";
import { getArtifactDownloadUrl } from "@/lib/api";
import { getArtifactBusinessLabel, getArtifactFormatLabel } from "@/lib/artifact-review-helpers";

/** Formats an ISO 8601 date string for display, falling back to the raw string on failure. */
function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleString();
  } catch {
    return iso;
  }
}

/**
 * Builds the Preview link URL: run-scoped (/runs/{runId}/artifacts/...) when runId is provided
 * (redirects to manifest canonical), otherwise manifest-scoped (/manifests/{manifestId}/artifacts/...).
 */
function reviewHrefForArtifact(
  manifestId: string,
  artifactId: string,
  runId: string | undefined,
): string {
  if (runId) {
    return `/reviews/${encodeURIComponent(runId)}/artifacts/${encodeURIComponent(artifactId)}`;
  }

  return `/manifests/${encodeURIComponent(manifestId)}/artifacts/${encodeURIComponent(artifactId)}`;
}

/**
 * Deterministic artifact list for run and manifest pages (preview + download).
 */
export function ArtifactListTable(props: {
  manifestId: string;
  artifacts: ArtifactDescriptor[];
  /** When set, the matching row is visually emphasized (e.g. on artifact preview page). */
  currentArtifactId?: string;
  /**
   * When set, Preview links use /runs/.../artifacts/... (redirects to manifest-scoped preview).
   * Improves run-centric navigation from run detail.
   */
  runId?: string;
}) {
  const { manifestId, artifacts, currentArtifactId, runId } = props;
  const sorted = [...artifacts].sort((a, b) => a.name.localeCompare(b.name, undefined, { sensitivity: "base" }));

  return (
    <div className="overflow-x-auto">
      <table className="w-full border-collapse text-sm">
        <thead>
          <tr className="border-b border-neutral-300 text-left dark:border-neutral-600">
            <th className="px-2 py-2.5">Artifact</th>
            <th className="px-2 py-2.5">Type</th>
            <th className="px-2 py-2.5">File format</th>
            <th className="px-2 py-2.5">Created</th>
            <th className="px-2 py-2.5">Actions</th>
          </tr>
        </thead>
        <tbody>
          {sorted.map((artifact) => {
            const reviewHref = reviewHrefForArtifact(manifestId, artifact.artifactId, runId);

            const isCurrent =
              currentArtifactId !== undefined && currentArtifactId === artifact.artifactId;

            return (
              <tr
                key={artifact.artifactId}
                className={`border-b border-neutral-100 dark:border-neutral-800 ${isCurrent ? "bg-blue-50 dark:bg-blue-950/30" : ""}`}
                title={`Content hash: ${artifact.contentHash}`}
              >
                <td className="max-w-[280px] px-2 py-2.5">
                  <strong className="font-semibold">{getArtifactBusinessLabel(artifact.artifactType)}</strong>
                </td>
                <td className="px-2 py-2.5 text-neutral-600 dark:text-neutral-400">
                  <span title={getArtifactFormatLabel(artifact.format)} className="text-xs">
                    {getArtifactFormatLabel(artifact.format)}
                  </span>
                </td>
                <td className="whitespace-nowrap px-2 py-2.5 text-neutral-600 dark:text-neutral-400">
                  {formatDate(artifact.createdUtc)}
                </td>
                <td className="px-2 py-2.5">
                  <Link href={reviewHref}>Preview</Link>
                  <span className="mx-2 text-neutral-300 dark:text-neutral-600">|</span>
                  <a href={getArtifactDownloadUrl(manifestId, artifact.artifactId)}>Download</a>
                  {runId ? (
                    <div className="mt-2 max-w-xs">
                      <ProductLearningFeedbackControls
                        runId={runId}
                        subjectType="ManifestArtifact"
                        artifactHint={`${artifact.artifactType}:${artifact.name}`}
                        patternKey={`artifact:${artifact.artifactType}`}
                        detail={{
                          artifactId: artifact.artifactId,
                          manifestId,
                          format: artifact.format,
                        }}
                        compact
                        title="Artifact useful?"
                      />
                    </div>
                  ) : null}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

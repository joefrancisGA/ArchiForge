import type { ArtifactDescriptor } from "@/types/authority";

import { getShowcaseStaticDemoPayload } from "@/lib/showcase-static-demo";

import {
  FIXTURE_MANIFEST_ID,
  FIXTURE_RUN_ID,
} from "./ids";

/** Artifact rows aligned to static showcase (for mock API when manifest UUID matches marketing). */
export function fixtureArtifactDescriptorsForShowcase(urlRunId: string): ArtifactDescriptor[] {
  const d = getShowcaseStaticDemoPayload(urlRunId);
  const manifestId = d.manifest.manifestId;
  const runId = d.run.runId;

  return d.artifacts.map((a) => ({
    artifactId: a.artifactId,
    artifactType: a.artifactType,
    name: a.name,
    format: a.format,
    createdUtc: a.createdUtc,
    contentHash: a.contentHash,
    manifestId,
    runId,
  }));
}

/** Non-empty artifact list that passes `coerceArtifactDescriptorList`. */
export function fixtureArtifactDescriptorsNonEmpty(): ArtifactDescriptor[] {
  return [
    {
      artifactId: "art-fixture-001",
      artifactType: "MarkdownNarrative",
      name: "architecture-overview.md",
      format: "markdown",
      createdUtc: "2025-06-01T12:06:00.000Z",
      contentHash: "sha256:artifact_fixture_001",
      manifestId: FIXTURE_MANIFEST_ID,
      runId: FIXTURE_RUN_ID,
    },
    {
      artifactId: "art-fixture-002",
      artifactType: "MermaidDiagram",
      name: "topology.mmd",
      format: "mermaid",
      createdUtc: "2025-06-01T12:06:01.000Z",
      contentHash: "sha256:artifact_fixture_002",
      manifestId: FIXTURE_MANIFEST_ID,
      runId: FIXTURE_RUN_ID,
    },
  ];
}

/** Empty artifact list (valid JSON array, no rows). */
export function fixtureArtifactDescriptorsEmpty(): ArtifactDescriptor[] {
  return [];
}

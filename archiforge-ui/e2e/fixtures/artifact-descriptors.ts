import type { ArtifactDescriptor } from "@/types/authority";

import { FIXTURE_MANIFEST_ID, FIXTURE_RUN_ID } from "./ids";

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

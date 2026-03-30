/**
 * Operator-facing copy for synthesized artifact types (ArchiForge.ArtifactSynthesis.Models.ArtifactType).
 */
const ARTIFACT_TYPE_COPY: Record<string, { label: string; description: string }> = {
  ReferenceArchitectureMarkdown: {
    label: "Reference architecture (Markdown)",
    description:
      "Narrative reference architecture derived from the golden manifest—suitable for review and handoff as documentation.",
  },
  ArchitectureNarrative: {
    label: "Architecture narrative",
    description:
      "Structured narrative summary of the architecture decisions and context captured in the manifest.",
  },
  DiagramAst: {
    label: "Diagram AST (JSON)",
    description:
      "Machine-oriented graph of nodes and edges representing manifest-linked elements; used for rendering or tooling.",
  },
  MermaidDiagram: {
    label: "Mermaid diagram",
    description:
      "Mermaid source for a high-level diagram (often decisions linked to the manifest). Render in a Mermaid-capable viewer or download the file.",
  },
  Inventory: {
    label: "Inventory",
    description:
      "JSON inventory of resources or components inferred from the architecture context for this run.",
  },
  CostSummary: {
    label: "Cost summary",
    description:
      "JSON summary of cost signals associated with the architecture (where modeled in the manifest pipeline).",
  },
  ComplianceMatrix: {
    label: "Compliance matrix",
    description:
      "JSON matrix of compliance-related controls or requirements versus the current manifest posture.",
  },
  CoverageSummary: {
    label: "Coverage summary",
    description:
      "JSON summary of coverage dimensions (e.g. requirements or controls) for the committed manifest.",
  },
  UnresolvedIssuesReport: {
    label: "Unresolved issues",
    description:
      "JSON report listing unresolved issues or warnings that operators should triage before sign-off.",
  },
};

export type ArtifactViewKind = "markdown" | "json" | "mermaid" | "plain";

/**
 * Maps API format + type to how the shell should present body text (no markdown renderer dependency).
 */
export function classifyArtifactView(format: string, artifactType: string): ArtifactViewKind {
  const f = format.trim().toLowerCase();

  if (f === "markdown" || f === "md") {
    return "markdown";
  }

  if (f === "mermaid" || f === "mmd") {
    return "mermaid";
  }

  if (f === "json" || artifactType === "DiagramAst") {
    return "json";
  }

  return "plain";
}

/** Returns a human-readable label for an artifact type (e.g. "Cost summary" for "CostSummary"). */
export function getArtifactTypeLabel(artifactType: string): string {
  const entry = ARTIFACT_TYPE_COPY[artifactType];

  if (entry) {
    return entry.label;
  }

  return artifactType.replace(/([a-z])([A-Z])/g, "$1 $2").trim();
}

/** Returns a one-line description of what an artifact type represents, for the review panel header. */
export function getArtifactTypeDescription(artifactType: string): string {
  const entry = ARTIFACT_TYPE_COPY[artifactType];

  if (entry) {
    return entry.description;
  }

  return `Synthesized artifact of type "${artifactType}". Use metadata and content below to orient; download if you need an offline copy.`;
}

export type PreparedArtifactBody = {
  viewKind: ArtifactViewKind;
  /** Human-oriented body (pretty JSON when applicable). */
  readableText: string;
  /** Original UTF-8 text from the API (for raw disclosure). */
  rawText: string;
  /** True when JSON pretty-print failed; readableText falls back to raw. */
  jsonPrettyFailed: boolean;
};

/**
 * Produces readable vs raw UTF-8 text for review panels (deterministic, no HTML injection).
 */
export function prepareArtifactBodyText(
  utf8Text: string,
  format: string,
  artifactType: string,
): PreparedArtifactBody {
  const rawText = utf8Text;
  const viewKind = classifyArtifactView(format, artifactType);

  if (viewKind !== "json") {
    return {
      viewKind,
      readableText: utf8Text,
      rawText,
      jsonPrettyFailed: false,
    };
  }

  try {
    const parsed: unknown = JSON.parse(utf8Text);

    return {
      viewKind,
      readableText: `${JSON.stringify(parsed, null, 2)}\n`,
      rawText,
      jsonPrettyFailed: false,
    };
  } catch {
    return {
      viewKind,
      readableText: utf8Text,
      rawText,
      jsonPrettyFailed: true,
    };
  }
}

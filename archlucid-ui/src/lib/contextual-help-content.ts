export type ContextualHelpEntry = {
  text: string;
  learnMoreUrl?: string;
};

/**
 * In-app help copy for the core pilot flow. `learnMoreUrl` values are app-relative; see
 * {@link toDocsBlobUrl} when linking to the repo default branch on the web.
 */
export const contextualHelpByKey: Record<string, ContextualHelpEntry> = {
  "new-run-wizard": {
    text: "Create an architecture request that describes the system you want ArchLucid to analyze.",
    learnMoreUrl: "/docs/CORE_PILOT.md#new-run",
  },
  "run-pipeline-status": {
    text: "The pipeline shows each AI agent's progress. When all steps complete, the run is ready to commit.",
    learnMoreUrl: "/docs/CORE_PILOT.md#pipeline-status",
  },
  "commit-manifest": {
    text: "Committing produces a versioned golden manifest and synthesizes artifacts. This is the primary pilot deliverable.",
    learnMoreUrl: "/docs/CORE_PILOT.md#commit",
  },
  "manifest-review": {
    text: "Review the manifest's decisions, findings, and structured metadata. Download artifacts for offline review.",
    learnMoreUrl: "/docs/CORE_PILOT.md#manifest-review",
  },
  "governance-gate": {
    text: "When enabled, the governance gate checks findings against severity thresholds before allowing commit.",
    learnMoreUrl: "/docs/CORE_PILOT.md#governance-gate",
  },
};

const DEFAULT_BLOB_BASE = "https://github.com/joefrancisGA/ArchLucid/blob/main";

/**
 * Resolves a relative in-repo docs path (e.g. `/docs/CORE_PILOT.md#h`) to a `blob` URL for “Learn more”.
 * Override with <code>NEXT_PUBLIC_ARCHLUCID_DOCS_BLOB_BASE</code> when the default branch or fork differs.
 */
export function toDocsBlobUrl(learnMoreUrl: string): string {
  const custom = process.env.NEXT_PUBLIC_ARCHLUCID_DOCS_BLOB_BASE;

  if (custom && custom.length > 0) {
    return `${custom.replace(/\/$/, "")}${learnMoreUrl}`;
  }

  const withoutLeading = learnMoreUrl.replace(/^\//, "");
  return `${DEFAULT_BLOB_BASE}/${withoutLeading}`;
}

import Link from "next/link";

export type OperatorEvidenceLimitsExecutionProps = {
  readonly realModeFellBackToSimulator?: boolean;
  readonly pilotAoaiDeploymentSnapshot?: string | null;
};

export type OperatorEvidenceLimitsInspectMetaProps = {
  readonly modelDeploymentName?: string | null;
  readonly promptTemplateVersion?: string | null;
};

export type OperatorEvidenceLimitsFooterProps = {
  readonly runId: string;
  /** Finding detail: adds `/findings/{id}/inspect` alongside provenance. */
  readonly findingIdForInspectLink?: string | null;
  /** Link to aggregate explanation section on run detail (`#run-explanation`). */
  readonly showArchitectureReviewSummaryLink?: boolean;
  readonly execution?: OperatorEvidenceLimitsExecutionProps | null;
  readonly inspectMetadata?: OperatorEvidenceLimitsInspectMetaProps | null;
};

function trimmedOrEmpty(value: string | null | undefined): string {
  return typeof value === "string" ? value.trim() : "";
}

/**
 * Operator-facing footer: deep links to provenance / explain surfaces plus factual execution disclaimers
 * from existing API fields only (run fallback flags, inspect metadata).
 */
export function OperatorEvidenceLimitsFooter({
  runId,
  findingIdForInspectLink,
  showArchitectureReviewSummaryLink = true,
  execution,
  inspectMetadata,
}: OperatorEvidenceLimitsFooterProps) {
  const safeRunId = runId.trim();
  const runBase = `/runs/${encodeURIComponent(safeRunId)}`;
  const provenanceHref = `${runBase}/provenance`;
  const explainHref = `${runBase}#run-explanation`;
  const inspectFindingId = trimmedOrEmpty(findingIdForInspectLink);
  const inspectHref =
    inspectFindingId.length > 0
      ? `${runBase}/findings/${encodeURIComponent(inspectFindingId)}/inspect`
      : null;

  const showFallbackDisclaimer = execution?.realModeFellBackToSimulator === true;
  const deploymentSnapshot = trimmedOrEmpty(execution?.pilotAoaiDeploymentSnapshot);

  const modelName = trimmedOrEmpty(inspectMetadata?.modelDeploymentName);
  const promptVersion = trimmedOrEmpty(inspectMetadata?.promptTemplateVersion);
  const showInspectMetaLine = modelName.length > 0 || promptVersion.length > 0;

  return (
    <footer
      className="rounded-lg border border-neutral-200 bg-neutral-50/80 p-4 text-sm text-neutral-800 dark:border-neutral-700 dark:bg-neutral-900/40 dark:text-neutral-100"
      aria-labelledby="operator-evidence-limits-heading"
      data-testid="operator-evidence-limits-footer"
    >
      <h2 id="operator-evidence-limits-heading" className="m-0 text-sm font-semibold tracking-tight">
        Evidence and limits
      </h2>

      <p className="m-0 mt-2 text-xs leading-relaxed text-neutral-600 dark:text-neutral-400">
        Review structural provenance and recorded inspect metadata. This strip summarizes API-reported execution signals
        only; it does not assert production latency or external system health.
      </p>

      <ul className="m-0 mt-3 list-none space-y-2 p-0" data-testid="operator-evidence-limits-links">
        <li>
          <Link
            className="font-medium text-teal-800 underline underline-offset-2 hover:text-teal-900 dark:text-teal-300 dark:hover:text-teal-200"
            href={provenanceHref}
          >
            Review trail (provenance graph)
          </Link>
        </li>

        {showArchitectureReviewSummaryLink ? (
          <li>
            <Link
              className="font-medium text-teal-800 underline underline-offset-2 hover:text-teal-900 dark:text-teal-300 dark:hover:text-teal-200"
              href={explainHref}
            >
              Architecture review summary (explain aggregate)
            </Link>
          </li>
        ) : null}

        {inspectHref !== null ? (
          <li>
            <Link
              className="font-medium text-teal-800 underline underline-offset-2 hover:text-teal-900 dark:text-teal-300 dark:hover:text-teal-200"
              href={inspectHref}
            >
              Technical inspection trail
            </Link>
          </li>
        ) : null}
      </ul>

      {showFallbackDisclaimer ? (
        <p
          className="m-0 mt-3 text-xs leading-relaxed text-neutral-700 dark:text-neutral-300"
          data-testid="operator-evidence-limits-fallback-disclaimer"
        >
          This run is flagged in API data as real-mode fallback: Azure OpenAI execution did not complete and deterministic
          simulator output was substituted (see run record field{' '}
          <span className="font-mono text-[11px]">realModeFellBackToSimulator</span>
          ).
          {deploymentSnapshot.length > 0 ? (
            <>
              {" "}
              Recorded deployment snapshot at fallback:{" "}
              <span className="font-mono text-[11px]">{deploymentSnapshot}</span>.
            </>
          ) : null}
        </p>
      ) : null}

      {showInspectMetaLine ? (
        <p
          className="m-0 mt-3 text-xs leading-relaxed text-neutral-700 dark:text-neutral-300"
          data-testid="operator-evidence-limits-inspect-metadata"
        >
          Inspect API returned{" "}
          {modelName.length > 0 ? (
            <>
              model deployment name <span className="font-mono text-[11px]">{modelName}</span>
            </>
          ) : null}
          {modelName.length > 0 && promptVersion.length > 0 ? " and " : null}
          {promptVersion.length > 0 ? (
            <>
              prompt template version <span className="font-mono text-[11px]">{promptVersion}</span>
            </>
          ) : null}
          .
        </p>
      ) : null}
    </footer>
  );
}

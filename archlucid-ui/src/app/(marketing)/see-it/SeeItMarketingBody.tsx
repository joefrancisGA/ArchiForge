import Link from "next/link";

import { Button } from "@/components/ui/button";
import { getArtifactTypeLabel } from "@/lib/artifact-review-helpers";
import { policyPackBuyerLabel } from "@/lib/policy-pack-buyer-label";
import { SHOWCASE_STATIC_DEMO_RUN_ID } from "@/lib/showcase-static-demo";
import type { DemoCommitPagePreviewResponse } from "@/types/demo-preview";
import type { SeeItPreviewSource } from "./load-see-it-demo-preview";

export type SeeItMarketingBodyProps = {
  source: SeeItPreviewSource;
  payload: DemoCommitPagePreviewResponse;
};

function formatCount(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return "—";
  }

  if (typeof value !== "number" || !Number.isFinite(value)) {
    return "—";
  }

  return String(value);
}

/**
 * Anonymous “30 second” marketing slice — only fields present on `DemoCommitPagePreviewResponse`.
 */
export function SeeItMarketingBody({ source, payload }: SeeItMarketingBodyProps) {
  const artifactList = Array.isArray(payload.artifacts) ? payload.artifacts : [];
  const firstArtifacts = artifactList.slice(0, 3);
  const ruleSetId = payload.manifest?.ruleSetId ?? "";
  const ruleSetVersion = payload.manifest?.ruleSetVersion ?? "";
  const policyPackLabel = policyPackBuyerLabel(ruleSetId, ruleSetVersion);
  const runExplanation = payload.runExplanation;
  const findingCountDisplay = formatCount(runExplanation?.findingCount);
  const complianceGapDisplay = formatCount(runExplanation?.complianceGapCount);
  const rid = payload.run?.runId ?? "";
  const reviewTitle =
    rid === SHOWCASE_STATIC_DEMO_RUN_ID
      ? "Claims Intake Modernization Review"
      : (payload.run?.description ?? "").trim().length > 0
        ? String(payload.run?.description)
        : "Architecture review";

  return (
    <div className="space-y-6">
      <div
        role="status"
        data-testid="see-it-demo-banner"
        className="rounded border border-neutral-200 bg-neutral-50 px-3 py-2 text-sm text-neutral-800 dark:border-neutral-700 dark:bg-neutral-900/50 dark:text-neutral-200"
      >
        <p className="font-semibold">Sample healthcare architecture review — for evaluation only</p>
        {source === "snapshot" ? (
          <p className="mt-1 text-xs text-neutral-600 dark:text-neutral-400" data-testid="see-it-snapshot-notice">
            Stable sample — finalized January 2026.
          </p>
        ) : null}
      </div>

      <section
        data-testid="see-it-summary"
        className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-950"
      >
        <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">
          Finalized sample architecture review (read-only)
        </h2>
        <p className="mt-1 text-xs text-neutral-600 dark:text-neutral-400">
          Full artifact bytes for your tenant require a signed-in workspace.
        </p>
        <dl className="mt-3 space-y-2 text-sm text-neutral-800 dark:text-neutral-200">
          <div>
            <dt className="font-medium text-neutral-600 dark:text-neutral-400">Review</dt>
            <dd>{reviewTitle}</dd>
          </div>
          <div>
            <dt className="font-medium text-neutral-600 dark:text-neutral-400">Policy pack</dt>
            <dd>{policyPackLabel}</dd>
          </div>
          <div>
            <dt className="font-medium text-neutral-600 dark:text-neutral-400">Findings and compliance gaps</dt>
            <dd data-testid="see-it-finding-counts">
              Findings: {findingCountDisplay} · Compliance gaps: {complianceGapDisplay}
            </dd>
          </div>
        </dl>
      </section>

      <section
        data-testid="see-it-artifacts"
        className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-950"
      >
        <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">Key deliverables (preview)</h2>
        <ul className="mt-3 list-disc space-y-2 pl-5 text-sm text-neutral-800 dark:text-neutral-200">
          {firstArtifacts.length ? (
            firstArtifacts.map((artifact) => (
              <li key={artifact.artifactId}>
                <span className="font-medium">{artifact.name}</span>
                <span className="text-neutral-600 dark:text-neutral-400">
                  {" "}
                  · {getArtifactTypeLabel(artifact.artifactType)}
                </span>
              </li>
            ))
          ) : (
            <li data-testid="see-it-no-artifacts">
              Artifact descriptors will appear here once the preview payload includes generated outputs.
            </li>
          )}
        </ul>
      </section>

      <section className="flex flex-col gap-3 sm:flex-row sm:flex-wrap">
        <Button asChild variant="primary">
          <a
            data-testid="see-it-proof-pack-download"
            href="/api/proxy/v1/marketing/why-archlucid-pack.pdf"
            download="why-archlucid-pack.pdf"
          >
            Download proof pack (PDF)
          </a>
        </Button>
        <Link
          data-testid="see-it-full-preview-link"
          className="inline-flex items-center justify-center rounded-md border border-neutral-300 px-4 py-2 text-sm font-medium text-neutral-900 hover:bg-neutral-50 dark:border-neutral-600 dark:text-neutral-100 dark:hover:bg-neutral-900"
          href="/demo/preview"
        >
          Open full demo preview
        </Link>
      </section>
      <p className="text-xs text-neutral-600 dark:text-neutral-400">
        The PDF is a no-sign-in marketing bundle aligned with this sample.
      </p>
    </div>
  );
}

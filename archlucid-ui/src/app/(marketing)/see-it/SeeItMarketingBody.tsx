import Link from "next/link";

import type { DemoCommitPagePreviewResponse } from "@/types/demo-preview";
import type { SeeItPreviewSource } from "./load-see-it-demo-preview";

export type SeeItMarketingBodyProps = {
  source: SeeItPreviewSource;
  payload: DemoCommitPagePreviewResponse;
};

/**
 * Anonymous “30 second” marketing slice — only fields present on `DemoCommitPagePreviewResponse`.
 */
export function SeeItMarketingBody({ source, payload }: SeeItMarketingBodyProps) {
  const firstArtifacts = payload.artifacts.slice(0, 3);
  const manifestVersionLabel = `${payload.manifest.ruleSetId} @ ${payload.manifest.ruleSetVersion}`;

  return (
    <div className="space-y-6">
      <div
        role="status"
        data-testid="see-it-demo-banner"
        className="rounded border border-amber-300 bg-amber-50 px-3 py-2 text-sm text-amber-950 dark:border-amber-700 dark:bg-amber-950 dark:text-amber-100"
      >
        <p className="font-semibold">This is sample data from a fictional Contoso tenant. isDemoData=true.</p>
        {source === "snapshot" ? (
          <p className="mt-1 text-xs text-amber-900 dark:text-amber-200" data-testid="see-it-snapshot-notice">
            Showing a checked-in static snapshot because the live preview API was unreachable or returned a non-success
            response for this build or request.
          </p>
        ) : null}
      </div>

      <section data-testid="see-it-summary" className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-950">
        <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">Committed demo run (read-only)</h2>
        <dl className="mt-3 space-y-2 text-sm text-neutral-800 dark:text-neutral-200">
          <div>
            <dt className="font-medium text-neutral-600 dark:text-neutral-400">Run id</dt>
            <dd>
              <code className="rounded bg-neutral-100 px-1 py-0.5 text-xs dark:bg-neutral-900">{payload.run.runId}</code>
            </dd>
          </div>
          <div>
            <dt className="font-medium text-neutral-600 dark:text-neutral-400">Manifest version (rule set)</dt>
            <dd>
              <code className="rounded bg-neutral-100 px-1 py-0.5 text-xs dark:bg-neutral-900">{manifestVersionLabel}</code>
            </dd>
          </div>
          <div>
            <dt className="font-medium text-neutral-600 dark:text-neutral-400">Finding counts (from run explanation)</dt>
            <dd data-testid="see-it-finding-counts">
              findingCount={payload.runExplanation.findingCount}, complianceGapCount=
              {payload.runExplanation.complianceGapCount}
            </dd>
          </div>
        </dl>
      </section>

      <section data-testid="see-it-artifacts" className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-950">
        <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">First three artifacts (descriptors)</h2>
        <ul className="mt-3 list-disc space-y-2 pl-5 text-sm text-neutral-800 dark:text-neutral-200">
          {firstArtifacts.length ? (
            firstArtifacts.map((artifact) => (
              <li key={artifact.artifactId}>
                <span className="font-medium">{artifact.name}</span> · {artifact.artifactType} · {artifact.format} · id{" "}
                <code className="text-xs">{artifact.artifactId}</code>
              </li>
            ))
          ) : (
            <li data-testid="see-it-no-artifacts">No artifacts in this preview payload.</li>
          )}
        </ul>
      </section>

      <section className="flex flex-col gap-3 sm:flex-row sm:flex-wrap">
        <a
          data-testid="see-it-proof-pack-download"
          className="inline-flex items-center justify-center rounded-md bg-teal-700 px-4 py-2 text-sm font-medium text-white hover:bg-teal-800 dark:bg-teal-800 dark:hover:bg-teal-700"
          href="/api/proxy/v1/marketing/why-archlucid-pack.pdf"
          download="why-archlucid-pack.pdf"
        >
          Download proof pack (PDF)
        </a>
        <Link
          data-testid="see-it-full-preview-link"
          className="inline-flex items-center justify-center rounded-md border border-neutral-300 px-4 py-2 text-sm font-medium text-neutral-900 hover:bg-neutral-50 dark:border-neutral-600 dark:text-neutral-100 dark:hover:bg-neutral-900"
          href="/demo/preview"
        >
          Open full commit preview
        </Link>
      </section>
      <p className="text-xs text-neutral-600 dark:text-neutral-400">
        The PDF is the anonymous marketing bundle sourced from the same cached demo preview as{" "}
        <code>GET /v1/demo/preview</code> (no sign-in). Individual synthesized artifact bytes still require operator
        scope today — see docs/library/DEMO_PREVIEW.md.
      </p>
    </div>
  );
}

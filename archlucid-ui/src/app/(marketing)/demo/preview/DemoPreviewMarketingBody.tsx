import Link from "next/link";

import type { DemoCommitPagePreviewResponse } from "@/types/demo-preview";

export function DemoPreviewNotAvailable() {
  return (
    <div
      data-testid="demo-preview-not-available"
      role="status"
      className="rounded border border-neutral-300 bg-neutral-50 p-4 text-sm text-neutral-700 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300"
    >
      <p className="font-medium">Demo preview is not available on this host.</p>
      <p className="mt-1 text-neutral-600 dark:text-neutral-400">This usually means one of two things:</p>
      <ul className="mt-1 list-disc pl-5 text-neutral-600 dark:text-neutral-400">
        <li>
          The demo seed has not been applied yet on this host. Run <code>archlucid try</code> or{" "}
          <code>POST /v1/demo/seed</code>, then refresh.
        </li>
        <li>
          This deployment is not configured with <code>Demo:Enabled=true</code> — the demo surface is intentionally hidden
          on production-like hosts.
        </li>
      </ul>
    </div>
  );
}

function DemoStatusBanner({ payload }: { readonly payload: DemoCommitPagePreviewResponse }) {
  return (
    <div
      data-testid="demo-preview-status-banner"
      className="rounded border border-amber-300 bg-amber-50 px-3 py-2 text-xs text-amber-900 dark:border-amber-700 dark:bg-amber-950 dark:text-amber-200"
    >
      <span className="font-semibold">{payload.demoStatusMessage}</span> · run <code>{payload.run.runId}</code> ·
      generated <code>{payload.generatedUtc}</code>
    </div>
  );
}

/** Marketing-only commit page projection (no operator CTAs). */
export function DemoPreviewMarketingBody({ payload }: { readonly payload: DemoCommitPagePreviewResponse }) {
  const themes = payload.runExplanation.themeSummaries.slice(0, 5);
  const citationCount = payload.runExplanation.citations?.length ?? 0;

  return (
    <div className="space-y-8">
      <DemoStatusBanner payload={payload} />

      <section data-testid="demo-preview-run">
        <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">Run</h2>
        <p className="mt-2 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Run ID:</strong>{" "}
          <code className="rounded bg-neutral-100 px-1 py-0.5 text-xs dark:bg-neutral-800">{payload.run.runId}</code>
        </p>
        <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Project:</strong> {payload.run.projectId}
        </p>
        <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Description:</strong> {payload.run.description ?? ""}
        </p>
        <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Created (UTC):</strong> {payload.run.createdUtc}
        </p>
      </section>

      <section data-testid="demo-preview-authority-chain">
        <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">Provenance chain</h2>
        <ul className="mt-2 list-disc pl-5 text-sm text-neutral-700 dark:text-neutral-300">
          <li>Context Snapshot: {payload.authorityChain.contextSnapshotId ?? "—"}</li>
          <li>Graph Snapshot: {payload.authorityChain.graphSnapshotId ?? "—"}</li>
          <li>Findings Snapshot: {payload.authorityChain.findingsSnapshotId ?? "—"}</li>
          <li>Golden Manifest: {payload.authorityChain.goldenManifestId ?? "—"}</li>
          <li>Decision Trace: {payload.authorityChain.decisionTraceId ?? "—"}</li>
          <li>Artifact Bundle: {payload.authorityChain.artifactBundleId ?? "—"}</li>
        </ul>
      </section>

      <section data-testid="demo-preview-manifest-summary">
        <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">Manifest summary</h2>
        {payload.manifest.operatorSummary ? (
          <p className="mt-2 text-sm text-neutral-700 dark:text-neutral-300">{payload.manifest.operatorSummary}</p>
        ) : null}
        <p className="mt-2 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Status:</strong> {payload.manifest.status}
        </p>
        <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Rule set:</strong> {payload.manifest.ruleSetId} {payload.manifest.ruleSetVersion}
        </p>
        <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Decisions:</strong> {payload.manifest.decisionCount}
        </p>
        <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Warnings:</strong> {payload.manifest.warningCount}
        </p>
        <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Unresolved issues:</strong> {payload.manifest.unresolvedIssueCount}
        </p>
      </section>

      <section data-testid="demo-preview-aggregate-explanation">
        <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">Aggregate explanation</h2>
        <p className="mt-2 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Overall assessment:</strong> {payload.runExplanation.overallAssessment}
        </p>
        <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Risk posture:</strong> {payload.runExplanation.riskPosture}
        </p>
        <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Themes (up to 5):</strong> {themes.length ? themes.join(" · ") : "—"}
        </p>
        <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Citation count:</strong> {citationCount}
        </p>
      </section>

      <section data-testid="demo-preview-pipeline-timeline">
        <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">Pipeline timeline</h2>
        <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
          First {payload.pipelineTimeline.length} events (oldest first).
        </p>
        <ul className="mt-2 list-disc pl-5 text-sm text-neutral-700 dark:text-neutral-300">
          {payload.pipelineTimeline.map((e) => (
            <li key={e.eventId}>
              <code>{e.eventType}</code> · {e.occurredUtc} · {e.actorUserName}
            </li>
          ))}
        </ul>
        <p className="mt-3 text-xs text-neutral-500 dark:text-neutral-400">
          Show the full timeline on an{" "}
          <Link className="text-teal-700 underline dark:text-teal-300" href="/auth/signin">
            operator install
          </Link>
          .
        </p>
      </section>

      <section data-testid="demo-preview-artifacts">
        <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">Artifacts</h2>
        <div className="mt-2 overflow-x-auto rounded border border-neutral-200 dark:border-neutral-800">
          <table className="min-w-full text-left text-sm text-neutral-800 dark:text-neutral-200">
            <thead className="bg-neutral-100 text-xs uppercase tracking-wide text-neutral-600 dark:bg-neutral-900 dark:text-neutral-400">
              <tr>
                <th className="px-3 py-2">Name</th>
                <th className="px-3 py-2">Type</th>
                <th className="px-3 py-2">Format</th>
                <th className="px-3 py-2">Created (UTC)</th>
                <th className="px-3 py-2">Hash</th>
              </tr>
            </thead>
            <tbody>
              {payload.artifacts.map((a) => (
                <tr key={a.artifactId} className="border-t border-neutral-200 dark:border-neutral-800">
                  <td className="px-3 py-2">{a.name}</td>
                  <td className="px-3 py-2">{a.artifactType}</td>
                  <td className="px-3 py-2">{a.format}</td>
                  <td className="px-3 py-2">{a.createdUtc}</td>
                  <td className="px-3 py-2 font-mono text-xs">{a.contentHash}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <footer
        data-testid="demo-preview-footer"
        className="border-t border-neutral-200 pt-3 text-xs text-neutral-500 dark:border-neutral-800 dark:text-neutral-400"
      >
        Source: <code>GET /v1/demo/preview</code> · This page is a real ArchLucid commit page sourced from the demo seed.
      </footer>
    </div>
  );
}

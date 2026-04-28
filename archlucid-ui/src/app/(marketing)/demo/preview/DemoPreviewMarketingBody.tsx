import Link from "next/link";

import { ShowcaseOutcomeStrip } from "@/components/showcase/ShowcaseOutcomeStrip";
import type { DemoCommitPagePreviewResponse } from "@/types/demo-preview";
import { manifestStatusForDisplay } from "@/lib/manifest-status-display";

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
  const runIdLabel = typeof payload.run?.runId === "string" ? payload.run.runId : "—";
  const generatedUtc = typeof payload.generatedUtc === "string" ? payload.generatedUtc : "—";
  const demoMode = process.env.NEXT_PUBLIC_DEMO_MODE === "true";

  if (demoMode) {
    return (
      <div
        data-testid="demo-preview-status-banner"
        className="rounded border border-neutral-200 bg-neutral-50 px-3 py-2 text-xs text-neutral-700 dark:border-neutral-700 dark:bg-neutral-900/50 dark:text-neutral-200"
        role="status"
      >
        <span className="font-semibold">Sample data preview</span>
        {" · "}
        <span className="text-neutral-600 dark:text-neutral-400">
          Scenario: {payload.demoStatusMessage ?? "Demonstration"} — generated {generatedUtc}
        </span>
      </div>
    );
  }

  return (
    <div
      data-testid="demo-preview-status-banner"
      className="rounded border border-amber-300 bg-amber-50 px-3 py-2 text-xs text-amber-900 dark:border-amber-700 dark:bg-amber-950 dark:text-amber-200"
    >
      <span className="font-semibold">{payload.demoStatusMessage ?? "Demonstration preview"}</span> · run{" "}
      <code>{runIdLabel}</code> · generated <code>{generatedUtc}</code>
    </div>
  );
}

/** Marketing-only commit page projection (no operator CTAs). */
export function DemoPreviewMarketingBody({ payload }: { readonly payload: DemoCommitPagePreviewResponse }) {
  const chain = payload.authorityChain ?? {};
  const runEx = payload.runExplanation ?? null;
  const themeRaw = Array.isArray(runEx?.themeSummaries) ? runEx.themeSummaries : [];
  const themesDisplay = themeRaw.slice(0, 5).map((t) => (typeof t === "string" ? t : String(t)));
  const themes = themesDisplay.length > 0 ? themesDisplay.join(" · ") : "—";
  const citationCount =
    Array.isArray(runEx?.citations) && runEx.citations !== null ? runEx.citations.length : 0;
  const pipelineTimeline = Array.isArray(payload.pipelineTimeline) ? payload.pipelineTimeline : [];
  const artifacts = Array.isArray(payload.artifacts) ? payload.artifacts : [];
  const manifest = payload.manifest ?? null;

  return (
    <div className="space-y-8">
      <ShowcaseOutcomeStrip
        runId={typeof payload.run?.runId === "string" ? payload.run.runId : "—"}
        manifestId={manifest?.manifestId}
      />

      <DemoStatusBanner payload={payload} />

      <section data-testid="demo-preview-run">
        <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">Run</h2>
        <p className="mt-2 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Run ID:</strong>{" "}
          <code className="rounded bg-neutral-100 px-1 py-0.5 text-xs dark:bg-neutral-800">
            {typeof payload.run?.runId === "string" ? payload.run.runId : "—"}
          </code>
        </p>
        <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Project:</strong> {payload.run?.projectId ?? "—"}
        </p>
        <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Description:</strong> {payload.run?.description ?? ""}
        </p>
        <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Created (UTC):</strong> {typeof payload.run?.createdUtc === "string" ? payload.run.createdUtc : "—"}
        </p>
      </section>

      <section data-testid="demo-preview-authority-chain">
        <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">Review trail</h2>
        <ul className="mt-2 list-disc pl-5 text-sm text-neutral-700 dark:text-neutral-300">
          <li>Context Snapshot: {chain.contextSnapshotId ?? "—"}</li>
          <li>Graph Snapshot: {chain.graphSnapshotId ?? "—"}</li>
          <li>Findings Snapshot: {chain.findingsSnapshotId ?? "—"}</li>
          <li>Reviewed manifest: {chain.goldenManifestId ?? "—"}</li>
          <li>Decision Trace: {chain.decisionTraceId ?? "—"}</li>
          <li>Artifact Bundle: {chain.artifactBundleId ?? "—"}</li>
        </ul>
      </section>

      <section data-testid="demo-preview-manifest-summary">
        <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">Manifest summary</h2>
        {manifest?.operatorSummary ? (
          <p className="mt-2 text-sm text-neutral-700 dark:text-neutral-300">{manifest.operatorSummary}</p>
        ) : null}
        {!manifest ? (
          <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">Manifest summary unavailable.</p>
        ) : (
          <>
            <p className="mt-2 text-sm text-neutral-700 dark:text-neutral-300">
              <strong>Status:</strong> {manifestStatusForDisplay(manifest.status)}
            </p>
            <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
              <strong>Rule set:</strong>{" "}
              {manifest.ruleSetId ?? "—"} {manifest.ruleSetVersion ?? ""}
            </p>
            <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
              <strong>Decisions:</strong>{" "}
              {typeof manifest.decisionCount === "number" ? manifest.decisionCount : "—"}
            </p>
            <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
              <strong>Warnings:</strong>{" "}
              {typeof manifest.warningCount === "number" ? manifest.warningCount : "—"}
            </p>
            <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
              <strong>Unresolved issues:</strong>{" "}
              {typeof manifest.unresolvedIssueCount === "number" ? manifest.unresolvedIssueCount : "—"}
            </p>
          </>
        )}
      </section>

      <section data-testid="demo-preview-aggregate-explanation">
        <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">Aggregate explanation</h2>
        <p className="mt-2 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Overall assessment:</strong> {runEx?.overallAssessment ?? "—"}
        </p>
        <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Risk posture:</strong> {runEx?.riskPosture ?? "—"}
        </p>
        <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Themes (up to 5):</strong> {themes}
        </p>
        <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
          <strong>Citation count:</strong> {citationCount}
        </p>
      </section>

      <section data-testid="demo-preview-pipeline-timeline">
        <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">Pipeline timeline</h2>
        <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
          First {pipelineTimeline.length} events (oldest first).
        </p>
        <ul className="mt-2 list-disc pl-5 text-sm text-neutral-700 dark:text-neutral-300">
          {pipelineTimeline.map((e, index) => {
            const key =
              typeof e.eventId === "string" && e.eventId.trim().length > 0 ? e.eventId : `timeline-${index}`;

            const eventType =
              typeof e.eventType === "string" ? e.eventType : e.eventType !== undefined ? String(e.eventType) : "—";

            const occurred = typeof e.occurredUtc === "string" ? e.occurredUtc : "—";

            const actor =
              typeof e.actorUserName === "string"
                ? e.actorUserName
                : e.actorUserName !== undefined && e.actorUserName !== null
                  ? String(e.actorUserName)
                  : "—";

            return (
              <li key={key}>
                <code>{eventType}</code> · {occurred} · {actor}
              </li>
            );
          })}
        </ul>
        <p className="mt-3 text-xs text-neutral-500 dark:text-neutral-400">
          Show the full timeline after{" "}
          <Link className="text-teal-700 underline dark:text-teal-300" href="/auth/signin">
            opening in workspace
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
              {artifacts.map((a, index) => {
                const artifactKey =
                  typeof a.artifactId === "string" && a.artifactId.trim().length > 0
                    ? a.artifactId
                    : `artifact-${index}-${a.name ?? index}`;

                return (
                  <tr key={artifactKey} className="border-t border-neutral-200 dark:border-neutral-800">
                    <td className="px-3 py-2">{a.name ?? "—"}</td>
                    <td className="px-3 py-2">{a.artifactType ?? "—"}</td>
                    <td className="px-3 py-2">{a.format ?? "—"}</td>
                    <td className="px-3 py-2">{typeof a.createdUtc === "string" ? a.createdUtc : "—"}</td>
                    <td className="px-3 py-2 font-mono text-xs">{a.contentHash ?? "—"}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </section>

      <p
        data-testid="demo-preview-footer"
        className="border-t border-neutral-200 pt-3 text-xs text-neutral-500 dark:border-neutral-800 dark:text-neutral-400"
      >
        Powered by ArchLucid.
      </p>
    </div>
  );
}

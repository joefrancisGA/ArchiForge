"use client";

import Link from "next/link";
import { Suspense, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { ClientErrorBoundary } from "@/components/ClientErrorBoundary";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import {
  OperatorEmptyState,
  OperatorLoadingNotice,
  OperatorMalformedCallout,
  OperatorTryNext,
} from "@/components/OperatorShellMessage";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { isNextPublicDemoMode } from "@/lib/demo-ui-env";
import { isStaticDemoPayloadFallbackEnabled } from "@/lib/operator-static-demo";
import { LayerHeader } from "@/components/LayerHeader";
import { OperatorPageHeader } from "@/components/OperatorPageHeader";
import { RunIdPicker } from "@/components/RunIdPicker";
import { coerceReplayResponse } from "@/lib/operator-response-guards";
import { replayRun } from "@/lib/api";
import { replayModeLabel, sortReplayNotes } from "@/lib/replay-display";
import type { ReplayResponse } from "@/types/authority";

/** Matches ArchLucid.Persistence.Replay.ReplayMode */
const replayModes = ["ReconstructOnly", "RebuildManifest", "RebuildArtifacts"] as const;

/** Replay form: operator enters a run ID, selects a mode, and triggers an authority chain replay. */
function ReplayForm() {
  const searchParams = useSearchParams();
  const [runId, setRunId] = useState("");
  const [mode, setMode] = useState<string>(replayModes[0]);
  const [result, setResult] = useState<ReplayResponse | null>(null);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);
  const [malformedMessage, setMalformedMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const r = searchParams.get("runId");
    if (r) setRunId(r);
  }, [searchParams]);

  async function onReplay() {
    setLoading(true);
    setFailure(null);
    setMalformedMessage(null);
    setResult(null);

    const trimmedRunId = runId.trim();

    try {
      const response: unknown = await replayRun(trimmedRunId, mode);
      const coerced = coerceReplayResponse(response);

      if (!coerced.ok) {
        setResult(null);
        setMalformedMessage(coerced.message);
      } else {
        setResult(coerced.value);
      }
    } catch (err) {
      setFailure(toApiLoadFailure(err));
      setResult(null);
    } finally {
      setLoading(false);
    }
  }

  const runIdTrimmed = runId.trim();

  return (
    <main>
      <LayerHeader pageKey="replay" density="compact" />
      <OperatorPageHeader title="Replay" helpKey="replay-run" />
      <p className="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
        <Link href="/">Home</Link>
        {" · "}
        <Link href="/reviews?projectId=default">Runs</Link>
        {" · "}
        <Link href="/compare">Compare two runs</Link>
      </p>
      <p className="max-w-3xl leading-relaxed text-neutral-700 dark:text-neutral-300">
        Re-run the stored validation pipeline for a run. Choose a mode, then read validation flags and notes
        below.
      </p>

      <div className="grid max-w-3xl gap-3">
        <RunIdPicker
          label="Run to replay"
          placeholder="Run ID"
          value={runId}
          onChange={setRunId}
          inputId="replay-run-id"
        />

        <select
          className="max-w-xl rounded-md border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 dark:border-neutral-600 dark:bg-neutral-900 dark:text-neutral-100"
          value={mode}
          onChange={(e) => setMode(e.target.value)}
          aria-label="Replay mode"
        >
          {replayModes.map((item) => (
            <option key={item} value={item} title={replayModeLabel(item)}>
              {item}
            </option>
          ))}
        </select>
        <p className="m-0 text-sm text-neutral-500 dark:text-neutral-400">{replayModeLabel(mode)}</p>

        <button
          type="button"
          className="w-fit rounded-md border border-neutral-300 bg-white px-4 py-2.5 text-sm font-medium text-neutral-900 shadow-sm hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-neutral-600 dark:bg-neutral-900 dark:text-neutral-100 dark:hover:bg-neutral-800"
          onClick={() => void onReplay()}
          disabled={loading || !runIdTrimmed}
        >
          {loading ? "Replaying…" : "Replay"}
        </button>
      </div>

      {!runIdTrimmed && (
        <OperatorEmptyState title="Waiting for a run ID">
          <p className="m-0">
            Enter the run to replay, open this page with <code>?runId=…</code>, or go from{" "}
            <Link href="/reviews?projectId=default">Runs</Link> → run detail → <strong>Replay this run</strong>.
          </p>
        </OperatorEmptyState>
      )}

      {loading && runIdTrimmed && (
        <OperatorLoadingNotice>
          <strong>Replay in progress.</strong>
          <p className="mt-2 text-sm">
            Waiting for the API to finish the authority-chain replay. Large manifests or artifact rebuild modes can
            take longer—avoid navigating away until this clears.
          </p>
        </OperatorLoadingNotice>
      )}

      {failure !== null && (
        <>
          <OperatorApiProblem failure={failure} />
          <OperatorTryNext>
            Confirm the run exists, you have operator permissions, and the API is healthy. Retry with a lighter mode
            (e.g. <code>ReconstructOnly</code>) before <code>RebuildArtifacts</code>. Copy the correlation ID for API
            logs.
          </OperatorTryNext>
        </>
      )}

      {malformedMessage && (
        <>
          <OperatorMalformedCallout>
            <strong>Replay response was not usable.</strong>
            <p className="mt-2">{malformedMessage}</p>
          </OperatorMalformedCallout>
          <OperatorTryNext>
            Compare API and UI versions. If HTTP succeeded but validation JSON drifted, open a defect with{" "}
            <code>GET /version</code> and the correlation ID from any paired failing request.
          </OperatorTryNext>
        </>
      )}

      {result && (
        <ClientErrorBoundary title="Replay result failed to render">
        <section className="mt-6 rounded-lg border border-neutral-200 bg-white p-4 max-w-3xl dark:border-neutral-700 dark:bg-neutral-950">
          <h3 className="mt-0">Replay result</h3>
          <p className="mt-0 text-sm text-neutral-500 dark:text-neutral-400">
            Deterministic summary of what the API validated after replay. Use notes below for operator
            follow-up.
          </p>
          <dl className="grid grid-cols-[220px_1fr] gap-x-4 gap-y-2 text-sm mb-5">
            <dt className="text-neutral-500 dark:text-neutral-400">Run ID</dt>
            <dd className="m-0 font-mono text-[13px]">{result.runId}</dd>
            <dt className="text-neutral-500 dark:text-neutral-400">Mode</dt>
            <dd className="m-0">
              <span className="font-mono text-[13px]">{result.mode}</span>
              <span className="block text-[13px] text-neutral-500 dark:text-neutral-400 mt-1">
                {replayModeLabel(result.mode)}
              </span>
            </dd>
            <dt className="text-neutral-500 dark:text-neutral-400">Replayed (local)</dt>
            <dd className="m-0">{new Date(result.replayedUtc).toLocaleString()}</dd>
            {result.rebuiltManifestId && (
              <>
                <dt className="text-neutral-500 dark:text-neutral-400">Rebuilt manifest</dt>
                <dd className="m-0 font-mono text-xs">{result.rebuiltManifestId}</dd>
              </>
            )}
            {result.rebuiltManifestHash && (
              <>
                <dt className="text-neutral-500 dark:text-neutral-400">Rebuilt manifest hash</dt>
                <dd className="m-0 font-mono text-xs break-all">
                  {result.rebuiltManifestHash}
                </dd>
              </>
            )}
            {result.rebuiltArtifactBundleId && (
              <>
                <dt className="text-neutral-500 dark:text-neutral-400">Rebuilt artifact bundle</dt>
                <dd className="m-0 font-mono text-xs">
                  {result.rebuiltArtifactBundleId}
                </dd>
              </>
            )}
          </dl>

          <h4 className="text-[15px] mb-2">Validation flags</h4>
          <dl className="grid grid-cols-[240px_1fr] gap-x-3 gap-y-1.5 text-sm mb-5">
            <dt className="text-neutral-500 dark:text-neutral-400">Context present</dt>
            <dd className="m-0">{String(result.validation.contextPresent)}</dd>
            <dt className="text-neutral-500 dark:text-neutral-400">Graph present</dt>
            <dd className="m-0">{String(result.validation.graphPresent)}</dd>
            <dt className="text-neutral-500 dark:text-neutral-400">Findings present</dt>
            <dd className="m-0">{String(result.validation.findingsPresent)}</dd>
            <dt className="text-neutral-500 dark:text-neutral-400">Manifest present</dt>
            <dd className="m-0">{String(result.validation.manifestPresent)}</dd>
            <dt className="text-neutral-500 dark:text-neutral-400">Trace present</dt>
            <dd className="m-0">{String(result.validation.tracePresent)}</dd>
            <dt className="text-neutral-500 dark:text-neutral-400">Artifacts present</dt>
            <dd className="m-0">{String(result.validation.artifactsPresent)}</dd>
            <dt className="text-neutral-500 dark:text-neutral-400">Manifest hash matches</dt>
            <dd className="m-0">{String(result.validation.manifestHashMatches)}</dd>
            <dt className="text-neutral-500 dark:text-neutral-400">Artifact bundle after replay</dt>
            <dd className="m-0">{String(result.validation.artifactBundlePresentAfterReplay)}</dd>
          </dl>

          <h4 className="text-[15px] mb-2">Validation notes</h4>
          {result.validation.notes.length === 0 ? (
            <OperatorEmptyState title="No validation notes">
              <p className="m-0">The replay completed; the API returned zero note lines.</p>
            </OperatorEmptyState>
          ) : (
            <ul className="leading-relaxed m-0 pl-5">
              {sortReplayNotes(result.validation.notes).map((note, index) => (
                <li key={index}>{note}</li>
              ))}
            </ul>
          )}
        </section>
        </ClientErrorBoundary>
      )}
    </main>
  );
}

/** Suspense fallback shown while the ReplayForm client component is initializing. */
function ReplaySuspenseFallback() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading replay.</strong>
        <p className="mt-2 text-sm">
          Reading <code>runId</code> from the URL so “Replay this run” deep links open with the field prefilled…
        </p>
      </OperatorLoadingNotice>
    </main>
  );
}

/** Replay page entry point. Wraps ReplayForm in Suspense for useSearchParams hydration. */
export default function ReplayPage() {
  if (isNextPublicDemoMode() || isStaticDemoPayloadFallbackEnabled()) {
    return (
      <div className="rounded-lg border border-neutral-200 bg-neutral-50 p-6 text-sm text-neutral-600 dark:border-neutral-800 dark:bg-neutral-900 dark:text-neutral-400">
        <p className="m-0 font-medium text-neutral-800 dark:text-neutral-200">Replay not available in demo mode.</p>
        <p className="m-0 mt-1">Re-validating stored pipeline output requires a live API connection.</p>
      </div>
    );
  }

  return (
    <Suspense fallback={<ReplaySuspenseFallback />}>
      <ReplayForm />
    </Suspense>
  );
}

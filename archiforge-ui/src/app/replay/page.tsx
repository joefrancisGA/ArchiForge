"use client";

import { Suspense, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import {
  OperatorEmptyState,
  OperatorErrorCallout,
  OperatorLoadingNotice,
  OperatorMalformedCallout,
} from "@/components/OperatorShellMessage";
import { coerceReplayResponse } from "@/lib/operator-response-guards";
import { replayRun } from "@/lib/api";
import type { ReplayResponse } from "@/types/authority";

/** Matches ArchiForge.Persistence.Replay.ReplayMode */
const replayModes = ["ReconstructOnly", "RebuildManifest", "RebuildArtifacts"] as const;

/** Replay form: operator enters a run ID, selects a mode, and triggers an authority chain replay. */
function ReplayForm() {
  const searchParams = useSearchParams();
  const [runId, setRunId] = useState("");
  const [mode, setMode] = useState<string>(replayModes[0]);
  const [result, setResult] = useState<ReplayResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [malformedMessage, setMalformedMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const r = searchParams.get("runId");
    if (r) setRunId(r);
  }, [searchParams]);

  async function onReplay() {
    setLoading(true);
    setError(null);
    setMalformedMessage(null);

    try {
      const response: unknown = await replayRun(runId, mode);
      const coerced = coerceReplayResponse(response);

      if (!coerced.ok) {
        setResult(null);
        setMalformedMessage(coerced.message);
      } else {
        setResult(coerced.value);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Replay failed.");
      setResult(null);
    } finally {
      setLoading(false);
    }
  }

  return (
    <main>
      <h2>Replay run</h2>

      <div style={{ display: "grid", gap: 12, maxWidth: 800 }}>
        <input value={runId} onChange={(e) => setRunId(e.target.value)} placeholder="Run ID" />

        <select value={mode} onChange={(e) => setMode(e.target.value)}>
          {replayModes.map((item) => (
            <option key={item} value={item}>
              {item}
            </option>
          ))}
        </select>

        <button
          type="button"
          onClick={() => void onReplay()}
          disabled={loading || !runId}
        >
          {loading ? "Replaying…" : "Replay"}
        </button>
      </div>

      {!runId && (
        <OperatorEmptyState title="Waiting for a run ID">
          <p style={{ margin: 0 }}>
            Enter the run to replay, or open this page with <code>?runId=…</code> in the URL.
          </p>
        </OperatorEmptyState>
      )}

      {loading && runId && (
        <OperatorLoadingNotice>
          <strong>Replay in progress.</strong>
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>Waiting for the API to finish the replay…</p>
        </OperatorLoadingNotice>
      )}

      {error && (
        <OperatorErrorCallout>
          <strong>Replay request failed.</strong>
          <p style={{ margin: "8px 0 0" }}>{error}</p>
        </OperatorErrorCallout>
      )}

      {malformedMessage && (
        <OperatorMalformedCallout>
          <strong>Replay response was not usable.</strong>
          <p style={{ margin: "8px 0 0" }}>{malformedMessage}</p>
        </OperatorMalformedCallout>
      )}

      {result && (
        <section
          style={{
            marginTop: 24,
            padding: 16,
            border: "1px solid #e2e8f0",
            borderRadius: 8,
            background: "#fff",
            maxWidth: 800,
          }}
        >
          <h3 style={{ marginTop: 0 }}>Replay result</h3>
          <p style={{ fontSize: 14, color: "#64748b", marginTop: 0 }}>
            Deterministic summary of what the API validated after replay. Use notes below for operator
            follow-up.
          </p>
          <dl
            style={{
              display: "grid",
              gridTemplateColumns: "220px 1fr",
              gap: "8px 16px",
              fontSize: 14,
              margin: "0 0 20px",
            }}
          >
            <dt style={{ color: "#64748b" }}>Run ID</dt>
            <dd style={{ margin: 0, fontFamily: "monospace", fontSize: 13 }}>{result.runId}</dd>
            <dt style={{ color: "#64748b" }}>Mode</dt>
            <dd style={{ margin: 0 }}>{result.mode}</dd>
            <dt style={{ color: "#64748b" }}>Replayed (local)</dt>
            <dd style={{ margin: 0 }}>{new Date(result.replayedUtc).toLocaleString()}</dd>
            {result.rebuiltManifestId && (
              <>
                <dt style={{ color: "#64748b" }}>Rebuilt manifest</dt>
                <dd style={{ margin: 0, fontFamily: "monospace", fontSize: 12 }}>{result.rebuiltManifestId}</dd>
              </>
            )}
            {result.rebuiltArtifactBundleId && (
              <>
                <dt style={{ color: "#64748b" }}>Rebuilt artifact bundle</dt>
                <dd style={{ margin: 0, fontFamily: "monospace", fontSize: 12 }}>
                  {result.rebuiltArtifactBundleId}
                </dd>
              </>
            )}
          </dl>

          <h4 style={{ fontSize: 15, marginBottom: 8 }}>Validation flags</h4>
          <dl
            style={{
              display: "grid",
              gridTemplateColumns: "240px 1fr",
              gap: "6px 12px",
              fontSize: 14,
              margin: "0 0 20px",
            }}
          >
            <dt style={{ color: "#64748b" }}>Context present</dt>
            <dd style={{ margin: 0 }}>{String(result.validation.contextPresent)}</dd>
            <dt style={{ color: "#64748b" }}>Graph present</dt>
            <dd style={{ margin: 0 }}>{String(result.validation.graphPresent)}</dd>
            <dt style={{ color: "#64748b" }}>Findings present</dt>
            <dd style={{ margin: 0 }}>{String(result.validation.findingsPresent)}</dd>
            <dt style={{ color: "#64748b" }}>Manifest present</dt>
            <dd style={{ margin: 0 }}>{String(result.validation.manifestPresent)}</dd>
            <dt style={{ color: "#64748b" }}>Trace present</dt>
            <dd style={{ margin: 0 }}>{String(result.validation.tracePresent)}</dd>
            <dt style={{ color: "#64748b" }}>Artifacts present</dt>
            <dd style={{ margin: 0 }}>{String(result.validation.artifactsPresent)}</dd>
            <dt style={{ color: "#64748b" }}>Manifest hash matches</dt>
            <dd style={{ margin: 0 }}>{String(result.validation.manifestHashMatches)}</dd>
            <dt style={{ color: "#64748b" }}>Artifact bundle after replay</dt>
            <dd style={{ margin: 0 }}>{String(result.validation.artifactBundlePresentAfterReplay)}</dd>
          </dl>

          <h4 style={{ fontSize: 15, marginBottom: 8 }}>Validation notes</h4>
          {result.validation.notes.length === 0 ? (
            <OperatorEmptyState title="No validation notes">
              <p style={{ margin: 0 }}>The replay completed; the API returned zero note lines.</p>
            </OperatorEmptyState>
          ) : (
            <ul style={{ lineHeight: 1.55, margin: 0, paddingLeft: 20 }}>
              {result.validation.notes.map((note, index) => (
                <li key={index}>{note}</li>
              ))}
            </ul>
          )}
        </section>
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
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Reading URL parameters for this page…</p>
      </OperatorLoadingNotice>
    </main>
  );
}

/** Replay page entry point. Wraps ReplayForm in Suspense for useSearchParams hydration. */
export default function ReplayPage() {
  return (
    <Suspense fallback={<ReplaySuspenseFallback />}>
      <ReplayForm />
    </Suspense>
  );
}

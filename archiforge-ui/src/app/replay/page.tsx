"use client";

import { Suspense, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { replayRun } from "@/lib/api";
import type { ReplayResponse } from "@/types/authority";

/** Matches ArchiForge.Persistence.Replay.ReplayMode */
const replayModes = ["ReconstructOnly", "RebuildManifest", "RebuildArtifacts"] as const;

function ReplayForm() {
  const searchParams = useSearchParams();
  const [runId, setRunId] = useState("");
  const [mode, setMode] = useState<string>(replayModes[0]);
  const [result, setResult] = useState<ReplayResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const r = searchParams.get("runId");
    if (r) setRunId(r);
  }, [searchParams]);

  async function onReplay() {
    setLoading(true);
    setError(null);

    try {
      const response = await replayRun(runId, mode);
      setResult(response);
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

        <button type="button" onClick={onReplay} disabled={loading || !runId}>
          {loading ? "Replaying…" : "Replay"}
        </button>
      </div>

      {error && <p style={{ color: "crimson" }}>{error}</p>}

      {result && (
        <section style={{ marginTop: 24 }}>
          <h3>Replay result</h3>
          <p>
            <strong>Run ID:</strong> {result.runId}
          </p>
          <p>
            <strong>Mode:</strong> {result.mode}
          </p>
          <p>
            <strong>Replayed:</strong> {new Date(result.replayedUtc).toLocaleString()}
          </p>
          <p>
            <strong>Manifest hash matches:</strong> {String(result.validation.manifestHashMatches)}
          </p>
          <p>
            <strong>Artifacts present after replay:</strong>{" "}
            {String(result.validation.artifactBundlePresentAfterReplay)}
          </p>

          <h4>Validation notes</h4>
          <ul>
            {result.validation.notes.map((note, index) => (
              <li key={index}>{note}</li>
            ))}
          </ul>
        </section>
      )}
    </main>
  );
}

export default function ReplayPage() {
  return (
    <Suspense fallback={<p>Loading…</p>}>
      <ReplayForm />
    </Suspense>
  );
}

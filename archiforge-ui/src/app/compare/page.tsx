"use client";

import { Suspense, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { compareRuns } from "@/lib/api";
import type { RunComparison } from "@/types/authority";

function CompareForm() {
  const searchParams = useSearchParams();
  const [leftRunId, setLeftRunId] = useState("");
  const [rightRunId, setRightRunId] = useState("");
  const [result, setResult] = useState<RunComparison | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const left = searchParams.get("leftRunId");
    const right = searchParams.get("rightRunId");
    if (left) setLeftRunId(left);
    if (right) setRightRunId(right);
  }, [searchParams]);

  async function onCompare() {
    setLoading(true);
    setError(null);

    try {
      const response = await compareRuns(leftRunId, rightRunId);
      setResult(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Comparison failed.");
      setResult(null);
    } finally {
      setLoading(false);
    }
  }

  return (
    <main>
      <h2>Compare runs</h2>

      <div style={{ display: "grid", gap: 12, maxWidth: 800 }}>
        <input
          value={leftRunId}
          onChange={(e) => setLeftRunId(e.target.value)}
          placeholder="Left run ID"
        />
        <input
          value={rightRunId}
          onChange={(e) => setRightRunId(e.target.value)}
          placeholder="Right run ID"
        />
        <button type="button" onClick={onCompare} disabled={loading || !leftRunId || !rightRunId}>
          {loading ? "Comparing…" : "Compare"}
        </button>
      </div>

      {error && <p style={{ color: "crimson" }}>{error}</p>}

      {result && (
        <section style={{ marginTop: 24 }}>
          <h3>Run-level differences</h3>
          <ul>
            {result.runLevelDiffs.map((diff, index) => (
              <li key={`${diff.section}-${diff.key}-${index}`}>
                [{diff.diffKind}] {diff.section} / {diff.key}: {diff.beforeValue ?? ""} →{" "}
                {diff.afterValue ?? ""}
              </li>
            ))}
          </ul>

          <h3>Manifest differences</h3>
          {result.manifestComparison ? (
            <>
              <p>
                Added: {result.manifestComparison.addedCount} | Removed:{" "}
                {result.manifestComparison.removedCount} | Changed:{" "}
                {result.manifestComparison.changedCount}
              </p>
              <ul>
                {result.manifestComparison.diffs.map((diff, index) => (
                  <li key={`${diff.section}-${diff.key}-${index}`}>
                    [{diff.diffKind}] {diff.section} / {diff.key}: {diff.beforeValue ?? ""} →{" "}
                    {diff.afterValue ?? ""}
                    {diff.notes ? ` (${diff.notes})` : ""}
                  </li>
                ))}
              </ul>
            </>
          ) : (
            <p>No manifest comparison available.</p>
          )}
        </section>
      )}
    </main>
  );
}

export default function ComparePage() {
  return (
    <Suspense fallback={<p>Loading…</p>}>
      <CompareForm />
    </Suspense>
  );
}

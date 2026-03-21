"use client";

import { Suspense, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import {
  compareGoldenManifestRuns,
  compareRuns,
  getArchitecturePackageDocxUrl,
} from "@/lib/api";
import type { GoldenManifestComparison } from "@/types/comparison";
import type { RunComparison } from "@/types/authority";

function CompareForm() {
  const searchParams = useSearchParams();
  const [leftRunId, setLeftRunId] = useState("");
  const [rightRunId, setRightRunId] = useState("");
  const [result, setResult] = useState<RunComparison | null>(null);
  const [golden, setGolden] = useState<GoldenManifestComparison | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [goldenError, setGoldenError] = useState<string | null>(null);
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
    setGoldenError(null);
    setResult(null);
    setGolden(null);

    try {
      const legacy = await compareRuns(leftRunId, rightRunId);
      setResult(legacy);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Run comparison failed.");
    }

    try {
      const structured = await compareGoldenManifestRuns(leftRunId, rightRunId);
      setGolden(structured);
    } catch (err) {
      setGoldenError(
        err instanceof Error ? err.message : "Structured manifest comparison failed.",
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <main>
      <h2>Compare runs</h2>
      <p style={{ maxWidth: 720, color: "#444" }}>
        <strong>Base (left)</strong> is the earlier / reference run; <strong>target (right)</strong> is
        what you are evaluating. The structured comparison uses GoldenManifest sections (decisions,
        requirements, security, topology, cost).
      </p>

      <div style={{ display: "grid", gap: 12, maxWidth: 800 }}>
        <input
          value={leftRunId}
          onChange={(e) => setLeftRunId(e.target.value)}
          placeholder="Base run ID (left)"
          style={{ padding: 8 }}
        />
        <input
          value={rightRunId}
          onChange={(e) => setRightRunId(e.target.value)}
          placeholder="Target run ID (right)"
          style={{ padding: 8 }}
        />
        <button
          type="button"
          onClick={() => void onCompare()}
          disabled={loading || !leftRunId || !rightRunId}
          style={{ padding: "10px 16px", width: "fit-content" }}
        >
          {loading ? "Comparing…" : "Compare"}
        </button>
      </div>

      {error && <p style={{ color: "crimson" }}>{error}</p>}
      {goldenError && <p style={{ color: "darkorange" }}>{goldenError}</p>}

      {golden && (
        <section style={{ marginTop: 28 }}>
          <h3>Structured manifest comparison</h3>
          <p style={{ color: "#555", fontSize: 14 }}>
            Base: {golden.baseRunId} → Target: {golden.targetRunId}
          </p>
          <p>
            <a href={getArchitecturePackageDocxUrl(leftRunId, rightRunId)} rel="noreferrer">
              Download architecture package DOCX (includes comparison section)
            </a>
          </p>

          <h4>Summary</h4>
          <ul>
            {golden.summaryHighlights.map((h, i) => (
              <li key={i}>{h}</li>
            ))}
          </ul>

          <h4>Decision changes</h4>
          {golden.decisionChanges.length === 0 ? (
            <p>None.</p>
          ) : (
            <ul>
              {golden.decisionChanges.map((d, i) => (
                <li key={i}>
                  {d.decisionKey}: {d.baseValue ?? "—"} → {d.targetValue ?? "—"} ({d.changeType})
                </li>
              ))}
            </ul>
          )}

          <h4>Requirement changes</h4>
          {golden.requirementChanges.length === 0 ? (
            <p>None.</p>
          ) : (
            <ul>
              {golden.requirementChanges.map((r, i) => (
                <li key={i}>
                  {r.requirementName}: {r.changeType}
                </li>
              ))}
            </ul>
          )}

          <h4>Security posture delta</h4>
          {golden.securityChanges.length === 0 ? (
            <p>None.</p>
          ) : (
            <ul>
              {golden.securityChanges.map((s, i) => (
                <li key={i}>
                  {s.controlName}: {s.baseStatus ?? "—"} → {s.targetStatus ?? "—"}
                </li>
              ))}
            </ul>
          )}

          <h4>Topology changes</h4>
          {golden.topologyChanges.length === 0 ? (
            <p>None.</p>
          ) : (
            <ul>
              {golden.topologyChanges.map((t, i) => (
                <li key={i}>
                  {t.resource} ({t.changeType})
                </li>
              ))}
            </ul>
          )}

          <h4>Cost delta</h4>
          {golden.costChanges.length === 0 ? (
            <p>Max monthly cost unchanged.</p>
          ) : (
            <ul>
              {golden.costChanges.map((c, i) => (
                <li key={i}>
                  {c.baseCost ?? "—"} → {c.targetCost ?? "—"}
                </li>
              ))}
            </ul>
          )}
        </section>
      )}

      {result && (
        <section style={{ marginTop: 28 }}>
          <h3>Authority run / manifest diff (legacy)</h3>
          <ul>
            {result.runLevelDiffs.map((diff, index) => (
              <li key={`${diff.section}-${diff.key}-${index}`}>
                [{diff.diffKind}] {diff.section} / {diff.key}: {diff.beforeValue ?? ""} →{" "}
                {diff.afterValue ?? ""}
              </li>
            ))}
          </ul>

          <h4>Manifest differences (flat)</h4>
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

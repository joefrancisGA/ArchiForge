"use client";

import { Suspense, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import {
  OperatorEmptyState,
  OperatorErrorCallout,
  OperatorLoadingNotice,
  OperatorMalformedCallout,
  OperatorWarningCallout,
} from "@/components/OperatorShellMessage";
import {
  coerceComparisonExplanation,
  coerceGoldenManifestComparison,
  coerceRunComparison,
} from "@/lib/operator-response-guards";
import { AiComparisonExplanationView } from "@/components/compare/AiComparisonExplanationView";
import { LegacyRunComparisonView } from "@/components/compare/LegacyRunComparisonView";
import { StructuredComparisonView } from "@/components/compare/StructuredComparisonView";
import { compareGoldenManifestRuns, compareRuns, explainComparisonRuns } from "@/lib/api";
import type { GoldenManifestComparison } from "@/types/comparison";
import type { ComparisonExplanation } from "@/types/explanation";
import type { RunComparison } from "@/types/authority";

/**
 * Compare form: accepts two run IDs, fetches legacy + structured + AI comparisons in parallel,
 * validates responses via coerce functions, and renders results in three sub-views.
 */
function CompareForm() {
  const searchParams = useSearchParams();
  const [leftRunId, setLeftRunId] = useState("");
  const [rightRunId, setRightRunId] = useState("");
  const [result, setResult] = useState<RunComparison | null>(null);
  const [golden, setGolden] = useState<GoldenManifestComparison | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [goldenError, setGoldenError] = useState<string | null>(null);
  const [legacyMalformed, setLegacyMalformed] = useState<string | null>(null);
  const [goldenMalformed, setGoldenMalformed] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [aiExplanation, setAiExplanation] = useState<ComparisonExplanation | null>(null);
  const [aiError, setAiError] = useState<string | null>(null);
  const [aiMalformed, setAiMalformed] = useState<string | null>(null);
  const [aiLoading, setAiLoading] = useState(false);

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
    setLegacyMalformed(null);
    setGoldenMalformed(null);
    setResult(null);
    setGolden(null);
    setAiExplanation(null);
    setAiError(null);
    setAiMalformed(null);

    try {
      const legacy: unknown = await compareRuns(leftRunId, rightRunId);
      const coercedLegacy = coerceRunComparison(legacy);

      if (!coercedLegacy.ok) {
        setResult(null);
        setLegacyMalformed(coercedLegacy.message);
      } else {
        setResult(coercedLegacy.value);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Run comparison failed.");
      setResult(null);
    }

    try {
      const structured: unknown = await compareGoldenManifestRuns(leftRunId, rightRunId);
      const coercedGolden = coerceGoldenManifestComparison(structured);

      if (!coercedGolden.ok) {
        setGolden(null);
        setGoldenMalformed(coercedGolden.message);
      } else {
        setGolden(coercedGolden.value);
      }
    } catch (err) {
      setGoldenError(
        err instanceof Error ? err.message : "Structured manifest comparison failed.",
      );
      setGolden(null);
    } finally {
      setLoading(false);
    }
  }

  async function loadAiExplanation() {
    if (!leftRunId || !rightRunId) return;
    setAiLoading(true);
    setAiError(null);
    setAiExplanation(null);
    setAiMalformed(null);
    try {
      const ex: unknown = await explainComparisonRuns(leftRunId, rightRunId);
      const coerced = coerceComparisonExplanation(ex);

      if (!coerced.ok) {
        setAiExplanation(null);
        setAiMalformed(coerced.message);
      } else {
        setAiExplanation(coerced.value);
      }
    } catch (err) {
      setAiError(err instanceof Error ? err.message : "AI explanation failed.");
      setAiExplanation(null);
    } finally {
      setAiLoading(false);
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
        <div style={{ display: "flex", gap: 12, flexWrap: "wrap", alignItems: "center" }}>
          <button
            type="button"
            onClick={() => void onCompare()}
            disabled={loading || !leftRunId || !rightRunId}
            style={{ padding: "10px 16px" }}
          >
            {loading ? "Comparing…" : "Compare"}
          </button>
          <button
            type="button"
            onClick={() => void loadAiExplanation()}
            disabled={aiLoading || !leftRunId || !rightRunId}
            style={{ padding: "10px 16px" }}
          >
            {aiLoading ? "Explaining…" : "Explain changes (AI)"}
          </button>
        </div>
      </div>

      {(!leftRunId || !rightRunId) && (
        <OperatorEmptyState title="Waiting for both run IDs">
          <p style={{ margin: 0 }}>
            Enter a <strong>base</strong> and <strong>target</strong> run ID before comparing. Query
            parameters <code>leftRunId</code> and <code>rightRunId</code> prefill these fields.
          </p>
        </OperatorEmptyState>
      )}

      {loading && leftRunId && rightRunId && (
        <OperatorLoadingNotice>
          <strong>Comparing runs.</strong>
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>
            Calling legacy diff and structured golden-manifest comparison endpoints…
          </p>
        </OperatorLoadingNotice>
      )}

      {aiLoading && (
        <OperatorLoadingNotice>
          <strong>Requesting AI explanation.</strong>
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>This depends on server LLM configuration.</p>
        </OperatorLoadingNotice>
      )}

      {error && (
        <OperatorErrorCallout>
          <strong>Legacy run comparison failed.</strong>
          <p style={{ margin: "8px 0 0" }}>{error}</p>
        </OperatorErrorCallout>
      )}

      {legacyMalformed && (
        <OperatorMalformedCallout>
          <strong>Legacy comparison response was not usable.</strong>
          <p style={{ margin: "8px 0 0" }}>{legacyMalformed}</p>
        </OperatorMalformedCallout>
      )}

      {goldenError && (
        <OperatorWarningCallout>
          <strong>Structured manifest comparison request failed.</strong>
          <p style={{ margin: "8px 0 0" }}>{goldenError}</p>
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>
            The legacy comparison may still have succeeded; check the sections below.
          </p>
        </OperatorWarningCallout>
      )}

      {goldenMalformed && (
        <OperatorMalformedCallout>
          <strong>Structured comparison JSON did not match the UI contract.</strong>
          <p style={{ margin: "8px 0 0" }}>{goldenMalformed}</p>
        </OperatorMalformedCallout>
      )}

      {aiError && (
        <OperatorWarningCallout>
          <strong>AI explanation request failed.</strong>
          <p style={{ margin: "8px 0 0" }}>{aiError}</p>
        </OperatorWarningCallout>
      )}

      {aiMalformed && (
        <OperatorMalformedCallout>
          <strong>AI explanation response was not usable.</strong>
          <p style={{ margin: "8px 0 0" }}>{aiMalformed}</p>
        </OperatorMalformedCallout>
      )}

      {golden && <StructuredComparisonView golden={golden} />}

      {aiExplanation && <AiComparisonExplanationView explanation={aiExplanation} />}

      {result && <LegacyRunComparisonView result={result} />}
    </main>
  );
}

/** Suspense fallback shown while the CompareForm client component is initializing (reading URL params). */
function CompareSuspenseFallback() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading compare.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Reading URL parameters for this page…</p>
      </OperatorLoadingNotice>
    </main>
  );
}

/** Compare page entry point. Wraps CompareForm in Suspense for useSearchParams hydration. */
export default function ComparePage() {
  return (
    <Suspense fallback={<CompareSuspenseFallback />}>
      <CompareForm />
    </Suspense>
  );
}

"use client";

import Link from "next/link";
import { Suspense, useEffect, useRef, useState } from "react";
import { useSearchParams } from "next/navigation";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import {
  OperatorEmptyState,
  OperatorLoadingNotice,
  OperatorMalformedCallout,
  OperatorTryNext,
  OperatorWarningCallout,
} from "@/components/OperatorShellMessage";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
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

type ComparedPair = { left: string; right: string };

function outcomeLabel(params: {
  hasValue: boolean;
  failure: ApiLoadFailureState | null;
  malformed: string | null;
}): string {
  if (params.failure !== null) {
    return "Request failed";
  }

  if (params.malformed !== null) {
    return "Response not usable (shape)";
  }

  if (params.hasValue) {
    return "OK";
  }

  return "—";
}

/**
 * Compare form: two run IDs; fetches legacy compare then structured compare sequentially on "Compare";
 * optional AI explanation on a separate button. Renders structured section before legacy for review order.
 */
function CompareForm() {
  const searchParams = useSearchParams();
  const compareGenerationRef = useRef(0);
  const aiGenerationRef = useRef(0);
  const [leftRunId, setLeftRunId] = useState("");
  const [rightRunId, setRightRunId] = useState("");
  const [result, setResult] = useState<RunComparison | null>(null);
  const [golden, setGolden] = useState<GoldenManifestComparison | null>(null);
  const [legacyFailure, setLegacyFailure] = useState<ApiLoadFailureState | null>(null);
  const [goldenFailure, setGoldenFailure] = useState<ApiLoadFailureState | null>(null);
  const [legacyMalformed, setLegacyMalformed] = useState<string | null>(null);
  const [goldenMalformed, setGoldenMalformed] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [aiExplanation, setAiExplanation] = useState<ComparisonExplanation | null>(null);
  const [aiFailure, setAiFailure] = useState<ApiLoadFailureState | null>(null);
  const [aiMalformed, setAiMalformed] = useState<string | null>(null);
  const [aiLoading, setAiLoading] = useState(false);
  const [lastComparedPair, setLastComparedPair] = useState<ComparedPair | null>(null);

  useEffect(() => {
    const left = searchParams.get("leftRunId");
    const right = searchParams.get("rightRunId");
    if (left) setLeftRunId(left);
    if (right) setRightRunId(right);
  }, [searchParams]);

  const leftTrim = leftRunId.trim();
  const rightTrim = rightRunId.trim();
  const pairAligned =
    lastComparedPair !== null &&
    lastComparedPair.left === leftTrim &&
    lastComparedPair.right === rightTrim;
  const showStaleInputsWarning =
    !pairAligned &&
    lastComparedPair !== null &&
    (result !== null ||
      golden !== null ||
      legacyFailure !== null ||
      goldenFailure !== null ||
      legacyMalformed !== null ||
      goldenMalformed !== null ||
      aiExplanation !== null ||
      aiFailure !== null ||
      aiMalformed !== null);

  async function onCompare() {
    const leftAtStart = leftTrim;
    const rightAtStart = rightTrim;
    const gen = ++compareGenerationRef.current;

    setLoading(true);
    setLegacyFailure(null);
    setGoldenFailure(null);
    setLegacyMalformed(null);
    setGoldenMalformed(null);
    setResult(null);
    setGolden(null);
    setAiExplanation(null);
    setAiFailure(null);
    setAiMalformed(null);
    setLastComparedPair(null);

    try {
      const legacy: unknown = await compareRuns(leftAtStart, rightAtStart);

      if (gen !== compareGenerationRef.current) {
        return;
      }

      const coercedLegacy = coerceRunComparison(legacy);

      if (!coercedLegacy.ok) {
        setResult(null);
        setLegacyMalformed(coercedLegacy.message);
      } else {
        setResult(coercedLegacy.value);
      }
    } catch (err) {
      if (gen !== compareGenerationRef.current) {
        return;
      }

      setLegacyFailure(toApiLoadFailure(err));
      setResult(null);
    }

    try {
      const structured: unknown = await compareGoldenManifestRuns(leftAtStart, rightAtStart);

      if (gen !== compareGenerationRef.current) {
        return;
      }

      const coercedGolden = coerceGoldenManifestComparison(structured);

      if (!coercedGolden.ok) {
        setGolden(null);
        setGoldenMalformed(coercedGolden.message);
      } else {
        setGolden(coercedGolden.value);
      }
    } catch (err) {
      if (gen !== compareGenerationRef.current) {
        return;
      }

      setGoldenFailure(toApiLoadFailure(err));
      setGolden(null);
    } finally {
      if (gen === compareGenerationRef.current) {
        setLoading(false);
        setLastComparedPair({ left: leftAtStart, right: rightAtStart });
      }
    }
  }

  async function loadAiExplanation() {
    if (!leftTrim || !rightTrim) return;

    const leftAtStart = leftTrim;
    const rightAtStart = rightTrim;
    const gen = ++aiGenerationRef.current;

    setAiLoading(true);
    setAiFailure(null);
    setAiExplanation(null);
    setAiMalformed(null);

    try {
      const ex: unknown = await explainComparisonRuns(leftAtStart, rightAtStart);

      if (gen !== aiGenerationRef.current) {
        return;
      }

      const coerced = coerceComparisonExplanation(ex);

      if (!coerced.ok) {
        setAiExplanation(null);
        setAiMalformed(coerced.message);
      } else {
        setAiExplanation(coerced.value);
      }
    } catch (err) {
      if (gen !== aiGenerationRef.current) {
        return;
      }

      setAiFailure(toApiLoadFailure(err));
      setAiExplanation(null);
    } finally {
      if (gen === aiGenerationRef.current) {
        setAiLoading(false);
      }
    }
  }

  const hasResultsToNavigate =
    pairAligned && !loading && (golden !== null || result !== null || aiExplanation !== null);

  return (
    <main>
      <h2>Compare runs</h2>
      <p style={{ marginTop: 4, fontSize: 14 }}>
        <Link href="/">Home</Link>
        {" · "}
        <Link href="/runs?projectId=default">Runs</Link>
        {" · "}
        <Link href="/graph">Graph</Link>
      </p>
      <p style={{ maxWidth: 720, color: "#334155", lineHeight: 1.55 }}>
        <strong>Base (left)</strong> is the reference run; <strong>target (right)</strong> is what you are
        evaluating. The page <strong>loads</strong> legacy compare then structured compare; <strong>below</strong>,
        read <strong>structured first</strong>, then the legacy flat diff. AI explanation is optional and separate—
        use it after the tables.
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
            disabled={loading || !leftTrim || !rightTrim}
            style={{ padding: "10px 16px" }}
          >
            {loading ? "Comparing…" : "Compare"}
          </button>
          <button
            type="button"
            onClick={() => void loadAiExplanation()}
            disabled={aiLoading || !leftTrim || !rightTrim}
            style={{ padding: "10px 16px" }}
          >
            {aiLoading ? "Explaining…" : "Explain changes (AI)"}
          </button>
        </div>
      </div>

      {(!leftTrim || !rightTrim) && (
        <OperatorEmptyState title="Waiting for both run IDs">
          <p style={{ margin: 0 }}>
            Enter a <strong>base</strong> and <strong>target</strong> run ID before comparing. Query
            parameters <code>leftRunId</code> and <code>rightRunId</code> prefill these fields. Get IDs from{" "}
            <Link href="/runs?projectId=default">Runs</Link> or the <strong>Compare two runs (base = this run)</strong>{" "}
            link on run detail.
          </p>
        </OperatorEmptyState>
      )}

      {showStaleInputsWarning && (
        <OperatorWarningCallout>
          <strong>Run IDs no longer match the results below.</strong>
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>
            Content below still reflects{" "}
            <code style={{ fontSize: 13 }}>{lastComparedPair?.left}</code> →{" "}
            <code style={{ fontSize: 13 }}>{lastComparedPair?.right}</code>. Click <strong>Compare</strong> or{" "}
            <strong>Explain changes (AI)</strong> again after fixing IDs, or restore the previous values.
          </p>
        </OperatorWarningCallout>
      )}

      {loading && leftTrim && rightTrim && (
        <OperatorLoadingNotice>
          <strong>Comparing runs.</strong>
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>
            Legacy compare API first, then structured golden-manifest compare (same pair). Sections below
            are ordered for review: structured first, then legacy.
          </p>
        </OperatorLoadingNotice>
      )}

      {aiLoading && (
        <OperatorLoadingNotice>
          <strong>Requesting AI explanation.</strong>
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>This depends on server LLM configuration.</p>
        </OperatorLoadingNotice>
      )}

      {legacyFailure && (
        <>
          <p style={{ margin: "0 0 8px", fontSize: 14, fontWeight: 600 }}>
            Legacy run comparison failed.
          </p>
          <OperatorApiProblem
            problem={legacyFailure.problem}
            fallbackMessage={legacyFailure.message}
            correlationId={legacyFailure.correlationId}
          />
          <OperatorTryNext>
            Confirm both run IDs exist and are in scope (same tenant/project as the shell). Re-copy IDs from{" "}
            <Link href="/runs?projectId=default">Runs</Link> or run detail, then click <strong>Compare</strong> again.
            Use the correlation ID in API logs if you escalate.
          </OperatorTryNext>
        </>
      )}

      {legacyMalformed && (
        <>
          <OperatorMalformedCallout>
            <strong>Legacy comparison response was not usable.</strong>
            <p style={{ margin: "8px 0 0" }}>{legacyMalformed}</p>
          </OperatorMalformedCallout>
          <OperatorTryNext>
            Align API and UI versions (<code>GET /version</code>). If structured compare succeeded below, use that
            section for review while legacy is investigated.
          </OperatorTryNext>
        </>
      )}

      {goldenFailure && (
        <>
          <p style={{ margin: "0 0 8px", fontSize: 14, fontWeight: 600 }}>
            Structured manifest comparison request failed.
          </p>
          <OperatorApiProblem
            problem={goldenFailure.problem}
            fallbackMessage={goldenFailure.message}
            correlationId={goldenFailure.correlationId}
            variant="warning"
          />
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>
            The legacy comparison may still have succeeded; check the sections below.
          </p>
          <OperatorTryNext>
            Verify both runs have committed golden manifests in scope. If only legacy diff is needed for now, scroll
            to <strong>Legacy authority diff</strong> after confirming the pair in the summary panel.
          </OperatorTryNext>
        </>
      )}

      {goldenMalformed && (
        <>
          <OperatorMalformedCallout>
            <strong>Structured comparison JSON did not match the UI contract.</strong>
            <p style={{ margin: "8px 0 0" }}>{goldenMalformed}</p>
          </OperatorMalformedCallout>
          <OperatorTryNext>
            Treat this as contract drift—compare deployed API vs UI. The legacy diff section may still render if that
            response was valid.
          </OperatorTryNext>
        </>
      )}

      {aiFailure && (
        <>
          <p style={{ margin: "0 0 8px", fontSize: 14, fontWeight: 600 }}>
            AI explanation request failed.
          </p>
          <OperatorApiProblem
            problem={aiFailure.problem}
            fallbackMessage={aiFailure.message}
            correlationId={aiFailure.correlationId}
            variant="warning"
          />
          <OperatorTryNext>
            AI is optional—use structured and legacy tables above for the authoritative diff. If this should work,
            check API LLM configuration, quotas, and proxy timeouts, then retry <strong>Explain changes (AI)</strong>.
          </OperatorTryNext>
        </>
      )}

      {aiMalformed && (
        <>
          <OperatorMalformedCallout>
            <strong>AI explanation response was not usable.</strong>
            <p style={{ margin: "8px 0 0" }}>{aiMalformed}</p>
          </OperatorMalformedCallout>
          <OperatorTryNext>
            Fall back to structured/legacy compare. Capture the correlation ID and API version if filing a defect.
          </OperatorTryNext>
        </>
      )}

      {pairAligned && !loading && lastComparedPair !== null && (
        <section
          style={{
            marginTop: 20,
            padding: 14,
            border: "1px solid #e2e8f0",
            borderRadius: 8,
            background: "#f8fafc",
            maxWidth: 800,
          }}
          aria-label="Comparison request outcome"
        >
          <h3 style={{ marginTop: 0, marginBottom: 10, fontSize: 16 }}>Last compare request</h3>
          <p style={{ margin: "0 0 10px", fontSize: 14, color: "#475569" }}>
            <code style={{ fontSize: 13 }}>{lastComparedPair.left}</code>
            <span style={{ margin: "0 6px", color: "#94a3b8" }}>→</span>
            <code style={{ fontSize: 13 }}>{lastComparedPair.right}</code>
          </p>
          <dl
            style={{
              display: "grid",
              gridTemplateColumns: "minmax(160px, 220px) 1fr",
              gap: "6px 12px",
              fontSize: 14,
              margin: 0,
            }}
          >
            <dt style={{ color: "#64748b", margin: 0 }}>Structured manifest</dt>
            <dd style={{ margin: 0 }}>
              {outcomeLabel({
                hasValue: golden !== null,
                failure: goldenFailure,
                malformed: goldenMalformed,
              })}
            </dd>
            <dt style={{ color: "#64748b", margin: 0 }}>Legacy run / manifest diff</dt>
            <dd style={{ margin: 0 }}>
              {outcomeLabel({
                hasValue: result !== null,
                failure: legacyFailure,
                malformed: legacyMalformed,
              })}
            </dd>
          </dl>
          <p style={{ margin: "10px 0 0", fontSize: 13, color: "#64748b" }}>
            AI explanation is not included here—use the AI button for that pair.
          </p>
        </section>
      )}

      {hasResultsToNavigate && (
        <nav
          aria-label="Comparison results outline"
          style={{
            marginTop: 16,
            padding: 12,
            border: "1px solid #e2e8f0",
            borderRadius: 8,
            background: "#fff",
            maxWidth: 800,
            fontSize: 14,
          }}
        >
          <strong style={{ display: "block", marginBottom: 8 }}>Review order</strong>
          <ol style={{ margin: 0, paddingLeft: 22, lineHeight: 1.6 }}>
            {golden !== null && (
              <li>
                <a href="#compare-structured">Structured manifest comparison</a>
              </li>
            )}
            {result !== null && (
              <li>
                <a href="#compare-legacy">Legacy authority diff</a>
              </li>
            )}
            {aiExplanation !== null && (
              <li>
                <a href="#compare-ai">AI explanation</a>
              </li>
            )}
          </ol>
        </nav>
      )}

      {golden !== null && <StructuredComparisonView golden={golden} />}

      {result !== null && <LegacyRunComparisonView result={result} />}

      {aiExplanation !== null && <AiComparisonExplanationView explanation={aiExplanation} />}
    </main>
  );
}

/** Suspense fallback shown while the CompareForm client component is initializing (reading URL params). */
function CompareSuspenseFallback() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading compare.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>
          Reading <code>leftRunId</code> / <code>rightRunId</code> from the URL so shared compare links open with
          fields prefilled…
        </p>
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

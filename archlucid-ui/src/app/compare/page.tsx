"use client";

import Link from "next/link";
import { Suspense, useEffect, useRef, useState } from "react";
import { useSearchParams } from "next/navigation";
import { EmptyState } from "@/components/EmptyState";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { ShortcutHint } from "@/components/ShortcutHint";
import {
  OperatorLoadingNotice,
  OperatorMalformedCallout,
  OperatorTryNext,
  OperatorWarningCallout,
} from "@/components/OperatorShellMessage";
import { COMPARE_WAITING } from "@/lib/empty-state-presets";
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
import { RunIdPicker } from "@/components/RunIdPicker";
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
      <h2 className="flex flex-wrap items-baseline gap-2">
        Compare runs
        <ShortcutHint shortcut="Alt+C" className="align-middle text-[0.75rem] text-neutral-500" />
      </h2>
      <p className="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
        <Link href="/">Home</Link>
        {" · "}
        <Link href="/runs?projectId=default">Runs</Link>
        {" · "}
        <Link href="/graph">Graph</Link>
      </p>
      <p className="max-w-3xl leading-relaxed text-neutral-700 dark:text-neutral-300">
        <strong>Base (left)</strong> is the reference run; <strong>target (right)</strong> is what you are
        evaluating. The page <strong>loads</strong> legacy compare then structured compare; <strong>below</strong>,
        read <strong>structured first</strong>, then the legacy flat diff. AI explanation is optional and separate—
        use it after the tables.
      </p>

      <div className="grid max-w-3xl gap-3">
        <RunIdPicker
          label="Base run (left)"
          placeholder="Base run ID (left)"
          value={leftRunId}
          onChange={setLeftRunId}
          inputId="compare-left-run-id"
        />
        <RunIdPicker
          label="Target run (right)"
          placeholder="Target run ID (right)"
          value={rightRunId}
          onChange={setRightRunId}
          inputId="compare-right-run-id"
        />
        <div className="flex flex-wrap items-center gap-3">
          <button
            type="button"
            className="rounded-md border border-neutral-300 bg-white px-4 py-2.5 text-sm font-medium text-neutral-900 shadow-sm hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-neutral-600 dark:bg-neutral-900 dark:text-neutral-100 dark:hover:bg-neutral-800"
            onClick={() => void onCompare()}
            disabled={loading || !leftTrim || !rightTrim}
          >
            {loading ? "Comparing…" : "Compare"}
          </button>
          <button
            type="button"
            className="rounded-md border border-neutral-300 bg-white px-4 py-2.5 text-sm font-medium text-neutral-900 shadow-sm hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-neutral-600 dark:bg-neutral-900 dark:text-neutral-100 dark:hover:bg-neutral-800"
            onClick={() => void loadAiExplanation()}
            disabled={aiLoading || !leftTrim || !rightTrim}
          >
            {aiLoading ? "Explaining…" : "Explain changes (AI)"}
          </button>
        </div>
      </div>

      {(!leftTrim || !rightTrim) && <EmptyState {...COMPARE_WAITING} />}

      {showStaleInputsWarning && (
        <OperatorWarningCallout>
          <strong>Run IDs no longer match the results below.</strong>
          <p className="mt-2 text-sm text-neutral-700 dark:text-neutral-300">
            Content below still reflects{" "}
            <code className="rounded bg-neutral-100 px-1 text-xs dark:bg-neutral-800">{lastComparedPair?.left}</code> →{" "}
            <code className="rounded bg-neutral-100 px-1 text-xs dark:bg-neutral-800">{lastComparedPair?.right}</code>. Click <strong>Compare</strong> or{" "}
            <strong>Explain changes (AI)</strong> again after fixing IDs, or restore the previous values.
          </p>
        </OperatorWarningCallout>
      )}

      {loading && leftTrim && rightTrim && (
        <OperatorLoadingNotice>
          <strong>Comparing runs.</strong>
          <p className="mt-2 text-sm text-neutral-700 dark:text-neutral-300">
            Legacy compare API first, then structured golden-manifest compare (same pair). Sections below
            are ordered for review: structured first, then legacy.
          </p>
        </OperatorLoadingNotice>
      )}

      {aiLoading && (
        <OperatorLoadingNotice>
          <strong>Requesting AI explanation.</strong>
          <p className="mt-2 text-sm text-neutral-700 dark:text-neutral-300">This depends on server LLM configuration.</p>
        </OperatorLoadingNotice>
      )}

      {legacyFailure && (
        <>
          <p className="mb-2 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
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
            <p className="mt-2">{legacyMalformed}</p>
          </OperatorMalformedCallout>
          <OperatorTryNext>
            Align API and UI versions (<code>GET /version</code>). If structured compare succeeded below, use that
            section for review while legacy is investigated.
          </OperatorTryNext>
        </>
      )}

      {goldenFailure && (
        <>
          <p className="mb-2 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
            Structured manifest comparison request failed.
          </p>
          <OperatorApiProblem
            problem={goldenFailure.problem}
            fallbackMessage={goldenFailure.message}
            correlationId={goldenFailure.correlationId}
            variant="warning"
          />
          <p className="mt-2 text-sm text-neutral-700 dark:text-neutral-300">
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
            <p className="mt-2">{goldenMalformed}</p>
          </OperatorMalformedCallout>
          <OperatorTryNext>
            Treat this as contract drift—compare deployed API vs UI. The legacy diff section may still render if that
            response was valid.
          </OperatorTryNext>
        </>
      )}

      {aiFailure && (
        <>
          <p className="mb-2 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
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
            <p className="mt-2">{aiMalformed}</p>
          </OperatorMalformedCallout>
          <OperatorTryNext>
            Fall back to structured/legacy compare. Capture the correlation ID and API version if filing a defect.
          </OperatorTryNext>
        </>
      )}

      {pairAligned && !loading && lastComparedPair !== null && (
        <section
          className="mt-5 max-w-3xl rounded-lg border border-neutral-200 bg-neutral-50 p-4 dark:border-neutral-700 dark:bg-neutral-900/50"
          aria-label="Comparison request outcome"
        >
          <h3 className="mb-2.5 mt-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">Last compare request</h3>
          <p className="mb-2.5 text-sm text-neutral-600 dark:text-neutral-400">
            <code className="rounded bg-neutral-100 px-1 text-xs dark:bg-neutral-800">{lastComparedPair.left}</code>
            <span className="mx-1.5 text-neutral-400 dark:text-neutral-500">→</span>
            <code className="rounded bg-neutral-100 px-1 text-xs dark:bg-neutral-800">{lastComparedPair.right}</code>
          </p>
          <dl className="m-0 grid grid-cols-[minmax(10rem,14rem)_1fr] gap-x-3 gap-y-1.5 text-sm">
            <dt className="m-0 text-neutral-500 dark:text-neutral-400">Structured manifest</dt>
            <dd className="m-0 text-neutral-800 dark:text-neutral-200">
              {outcomeLabel({
                hasValue: golden !== null,
                failure: goldenFailure,
                malformed: goldenMalformed,
              })}
            </dd>
            <dt className="m-0 text-neutral-500 dark:text-neutral-400">Legacy run / manifest diff</dt>
            <dd className="m-0 text-neutral-800 dark:text-neutral-200">
              {outcomeLabel({
                hasValue: result !== null,
                failure: legacyFailure,
                malformed: legacyMalformed,
              })}
            </dd>
          </dl>
          <p className="mb-0 mt-2.5 text-xs text-neutral-500 dark:text-neutral-400">
            AI explanation is not included here—use the AI button for that pair.
          </p>
        </section>
      )}

      {hasResultsToNavigate && (
        <nav
          aria-label="Comparison results outline"
          className="mt-4 max-w-3xl rounded-lg border border-neutral-200 bg-white p-3 text-sm dark:border-neutral-700 dark:bg-neutral-900"
        >
          <strong className="mb-2 block text-neutral-900 dark:text-neutral-100">Review order</strong>
          <ol className="m-0 list-decimal pl-6 leading-relaxed text-neutral-800 dark:text-neutral-200">
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
        <p className="mt-2 text-sm text-neutral-700 dark:text-neutral-300">
          Reading <code className="rounded bg-neutral-100 px-1 text-xs dark:bg-neutral-800">leftRunId</code> /{" "}
          <code className="rounded bg-neutral-100 px-1 text-xs dark:bg-neutral-800">rightRunId</code> from the URL so shared compare links open with
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

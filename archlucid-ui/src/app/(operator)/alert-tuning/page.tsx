"use client";

import { useState } from "react";
import { AlertOperatorToolingRankCue } from "@/components/EnterpriseControlsContextHints";
import { LayerHeader } from "@/components/LayerHeader";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { recommendAlertThreshold } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure, uiFailureFromMessage } from "@/lib/api-load-failure";
import type { ThresholdCandidateEvaluation, ThresholdRecommendationResult } from "@/types/alert-tuning";

const SIMPLE_RULE_TYPES = [
  { value: "CriticalRecommendationCount", label: "Critical / high recommendation count" },
  { value: "NewComplianceGapCount", label: "New compliance gap count" },
  { value: "CostIncreasePercent", label: "Cost increase %" },
  { value: "DeferredHighPriorityRecommendationAgeDays", label: "Deferred high-priority age (days)" },
  { value: "RejectedSecurityRecommendation", label: "Rejected security recommendation" },
  { value: "AcceptanceRateDrop", label: "Acceptance rate below %" },
];

const COMPOSITE_METRICS = [
  { value: "CriticalRecommendationCount", label: "Critical/high recommendation count" },
  { value: "NewComplianceGapCount", label: "New compliance gap count" },
  { value: "CostIncreasePercent", label: "Cost increase %" },
  { value: "DeferredHighPriorityRecommendationCount", label: "Deferred high-priority count" },
  { value: "RejectedSecurityRecommendationCount", label: "Rejected security recommendations" },
  { value: "AcceptanceRatePercent", label: "Acceptance rate %" },
];

const COND_OPS = [
  { value: "GreaterThanOrEqual", label: "≥" },
  { value: "GreaterThan", label: ">" },
  { value: "LessThanOrEqual", label: "≤" },
  { value: "LessThan", label: "<" },
];

const SEVERITIES = ["Info", "Warning", "High", "Critical"];

function CandidateCard({
  evaluation,
  highlight,
}: {
  evaluation: ThresholdCandidateEvaluation;
  highlight: boolean;
}) {
  const { candidate, simulationResult, scoreBreakdown } = evaluation;
  return (
    <div
      style={{
        border: highlight ? "2px solid #333" : "1px solid #ddd",
        borderRadius: 8,
        padding: 12,
        background: highlight ? "#f9f9f9" : "#fff",
      }}
    >
      <strong>Threshold: {candidate.thresholdValue}</strong> ({candidate.label})
      <div style={{ marginTop: 8, fontSize: 14 }}>
        <div>Evaluated runs: {simulationResult.evaluatedRunCount}</div>
        <div>Matched: {simulationResult.matchedCount}</div>
        <div>Would create: {simulationResult.wouldCreateCount}</div>
        <div>Would suppress: {simulationResult.wouldSuppressCount}</div>
      </div>
      <div style={{ marginTop: 8 }}>
        <strong>Score breakdown</strong>
        <ul style={{ margin: "4px 0", paddingLeft: 20 }}>
          <li>Coverage: {scoreBreakdown.coverageScore.toFixed(2)}</li>
          <li>Noise penalty: {scoreBreakdown.noisePenalty.toFixed(2)}</li>
          <li>Suppression penalty: {scoreBreakdown.suppressionPenalty.toFixed(2)}</li>
          <li>Density penalty: {scoreBreakdown.densityPenalty.toFixed(2)}</li>
          <li>
            <strong>Final: {scoreBreakdown.finalScore.toFixed(2)}</strong>
          </li>
        </ul>
        <ul style={{ margin: "8px 0 0", paddingLeft: 20, fontSize: 13, color: "#444" }}>
          {scoreBreakdown.notes.map((note, i) => (
            <li key={i}>{note}</li>
          ))}
        </ul>
      </div>
    </div>
  );
}

export default function AlertTuningPage() {
  const [ruleKind, setRuleKind] = useState<"Simple" | "Composite">("Simple");
  const [ruleType, setRuleType] = useState("CostIncreasePercent");
  const [tunedMetricComposite, setTunedMetricComposite] = useState("CostIncreasePercent");
  const [severity, setSeverity] = useState("Warning");
  const [name, setName] = useState("Tuning candidate");
  const [candidateThresholdsStr, setCandidateThresholdsStr] = useState("5,10,15,20,25");
  const [recentRunCount, setRecentRunCount] = useState(10);
  const [targetMin, setTargetMin] = useState(1);
  const [targetMax, setTargetMax] = useState(5);
  const [runSlug, setRunSlug] = useState("default");

  const [cJoin, setCJoin] = useState("And");
  const [cSuppression, setCSuppression] = useState(1440);
  const [cCooldown, setCCooldown] = useState(60);
  const [cDedupe, setCDedupe] = useState("RuleAndRun");
  const [cM1, setCM1] = useState("CostIncreasePercent");
  const [cO1, setCO1] = useState("GreaterThanOrEqual");
  const [cV1, setCV1] = useState(10);
  const [cM2, setCM2] = useState("NewComplianceGapCount");
  const [cO2, setCO2] = useState("GreaterThanOrEqual");
  const [cV2, setCV2] = useState(1);

  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);
  const [result, setResult] = useState<ThresholdRecommendationResult | null>(null);

  async function recommend() {
    setFailure(null);
    setResult(null);
    const thresholds = candidateThresholdsStr
      .split(",")
      .map((x) => Number(x.trim()))
      .filter((x) => !Number.isNaN(x));

    if (thresholds.length === 0) {
      setFailure(uiFailureFromMessage("Enter at least one numeric candidate threshold (comma-separated)."));
      return;
    }

    const first = thresholds[0]!;

    setLoading(true);
    try {
      if (ruleKind === "Simple") {
        const data = await recommendAlertThreshold({
          ruleKind: "Simple",
          tunedMetricType: ruleType,
          candidateThresholds: thresholds,
          recentRunCount,
          targetCreatedAlertCountMin: targetMin,
          targetCreatedAlertCountMax: targetMax,
          runProjectSlug: runSlug.trim() || "default",
          baseSimpleRule: {
            ruleId: "00000000-0000-0000-0000-000000000000",
            tenantId: "00000000-0000-0000-0000-000000000000",
            workspaceId: "00000000-0000-0000-0000-000000000000",
            projectId: "00000000-0000-0000-0000-000000000000",
            name: name.trim() || "Candidate rule",
            ruleType,
            severity,
            thresholdValue: first,
            isEnabled: true,
            targetChannelType: "DigestOnly",
            metadataJson: "{}",
            createdUtc: new Date().toISOString(),
          },
        });
        setResult(data);
      } else {
        if (cM1 !== tunedMetricComposite && cM2 !== tunedMetricComposite) {
          setFailure(
            uiFailureFromMessage('Set "Metric to tune" to match condition 1 or condition 2 metric.'),
          );
          setLoading(false);
          return;
        }
        const data = await recommendAlertThreshold({
          ruleKind: "Composite",
          tunedMetricType: tunedMetricComposite,
          candidateThresholds: thresholds,
          recentRunCount,
          targetCreatedAlertCountMin: targetMin,
          targetCreatedAlertCountMax: targetMax,
          runProjectSlug: runSlug.trim() || "default",
          baseCompositeRule: {
            compositeRuleId: "00000000-0000-0000-0000-000000000000",
            tenantId: "00000000-0000-0000-0000-000000000000",
            workspaceId: "00000000-0000-0000-0000-000000000000",
            projectId: "00000000-0000-0000-0000-000000000000",
            name: name.trim() || "Composite tuning",
            severity,
            operator: cJoin,
            isEnabled: true,
            suppressionWindowMinutes: cSuppression,
            cooldownMinutes: cCooldown,
            reopenDeltaThreshold: 0,
            dedupeScope: cDedupe,
            targetChannelType: "AlertRouting",
            createdUtc: new Date().toISOString(),
            conditions: [
              { metricType: cM1, operator: cO1, thresholdValue: cV1 },
              { metricType: cM2, operator: cO2, thresholdValue: cV2 },
            ],
          },
        });
        setResult(data);
      }
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }

  const recommendedLabel = result?.recommendedCandidate?.candidate.label;

  return (
    <main style={{ maxWidth: 900 }}>
      <LayerHeader pageKey="alert-tuning" />
      <h2 style={{ marginTop: 0 }}>Alert tuning</h2>
      <p style={{ color: "#444", fontSize: 14, maxWidth: "42rem" }}>
        <strong>Suggest thresholds</strong> from simulated candidates (same evaluators as production), balancing noise vs
        coverage for a target alert band.
      </p>
      <AlertOperatorToolingRankCue />

      {failure !== null ? (
        <div role="alert">
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
        </div>
      ) : null}

      <div style={{ display: "grid", gap: 12, maxWidth: 720, marginBottom: 24 }}>
        <label>
          Rule kind
          <select
            value={ruleKind}
            onChange={(e) => setRuleKind(e.target.value as "Simple" | "Composite")}
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
          >
            <option value="Simple">Simple</option>
            <option value="Composite">Composite</option>
          </select>
        </label>

        <label>
          Name
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
          />
        </label>

        {ruleKind === "Simple" ? (
          <label>
            Rule type (simple)
            <select
              value={ruleType}
              onChange={(e) => setRuleType(e.target.value)}
              style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
            >
              {SIMPLE_RULE_TYPES.map((r) => (
                <option key={r.value} value={r.value}>
                  {r.label}
                </option>
              ))}
            </select>
          </label>
        ) : (
          <>
            <label>
              Metric to tune (must match a condition below)
              <select
                value={tunedMetricComposite}
                onChange={(e) => setTunedMetricComposite(e.target.value)}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              >
                {COMPOSITE_METRICS.map((m) => (
                  <option key={m.value} value={m.value}>
                    {m.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Join
              <select
                value={cJoin}
                onChange={(e) => setCJoin(e.target.value)}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              >
                <option value="And">All (AND)</option>
                <option value="Or">Any (OR)</option>
              </select>
            </label>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
              <label>
                Suppression window (min)
                <input
                  type="number"
                  value={cSuppression}
                  onChange={(e) => setCSuppression(Number(e.target.value))}
                  style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
                />
              </label>
              <label>
                Cooldown (min)
                <input
                  type="number"
                  value={cCooldown}
                  onChange={(e) => setCCooldown(Number(e.target.value))}
                  style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
                />
              </label>
            </div>
            <label>
              Dedupe scope
              <select
                value={cDedupe}
                onChange={(e) => setCDedupe(e.target.value)}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              >
                <option value="RuleOnly">Rule only</option>
                <option value="RuleAndRun">Rule + run</option>
                <option value="RuleAndComparison">Rule + run + comparison</option>
              </select>
            </label>
            <p style={{ margin: 0, fontWeight: 600 }}>Condition 1</p>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 8 }}>
              <select value={cM1} onChange={(e) => setCM1(e.target.value)}>
                {COMPOSITE_METRICS.map((m) => (
                  <option key={m.value} value={m.value}>
                    {m.label}
                  </option>
                ))}
              </select>
              <select value={cO1} onChange={(e) => setCO1(e.target.value)}>
                {COND_OPS.map((o) => (
                  <option key={o.value} value={o.value}>
                    {o.label}
                  </option>
                ))}
              </select>
              <input type="number" value={cV1} onChange={(e) => setCV1(Number(e.target.value))} />
            </div>
            <p style={{ margin: 0, fontWeight: 600 }}>Condition 2</p>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 8 }}>
              <select value={cM2} onChange={(e) => setCM2(e.target.value)}>
                {COMPOSITE_METRICS.map((m) => (
                  <option key={m.value} value={m.value}>
                    {m.label}
                  </option>
                ))}
              </select>
              <select value={cO2} onChange={(e) => setCO2(e.target.value)}>
                {COND_OPS.map((o) => (
                  <option key={o.value} value={o.value}>
                    {o.label}
                  </option>
                ))}
              </select>
              <input type="number" value={cV2} onChange={(e) => setCV2(Number(e.target.value))} />
            </div>
          </>
        )}

        <label>
          Severity
          <select
            value={severity}
            onChange={(e) => setSeverity(e.target.value)}
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
          >
            {SEVERITIES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </label>

        <label>
          Candidate thresholds (comma-separated)
          <input
            value={candidateThresholdsStr}
            onChange={(e) => setCandidateThresholdsStr(e.target.value)}
            placeholder="5,10,15,20,25"
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
          />
        </label>

        <label>
          Recent run count (1–50)
          <input
            type="number"
            min={1}
            max={50}
            value={recentRunCount}
            onChange={(e) => setRecentRunCount(Number(e.target.value))}
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
          />
        </label>

        <label>
          Run project slug
          <input
            value={runSlug}
            onChange={(e) => setRunSlug(e.target.value)}
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
          />
        </label>

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
          <label>
            Target created alerts (min)
            <input
              type="number"
              min={0}
              value={targetMin}
              onChange={(e) => setTargetMin(Number(e.target.value))}
              style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
            />
          </label>
          <label>
            Target created alerts (max)
            <input
              type="number"
              min={0}
              value={targetMax}
              onChange={(e) => setTargetMax(Number(e.target.value))}
              style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
            />
          </label>
        </div>

        <button
          type="button"
          onClick={() => void recommend()}
          disabled={loading}
          style={{ padding: "10px 16px", cursor: loading ? "wait" : "pointer", maxWidth: 240 }}
        >
          {loading ? "Running…" : "Recommend threshold"}
        </button>
      </div>

      {result ? (
        <>
          <h3>Summary</h3>
          <ul>
            {result.summaryNotes.map((note, index) => (
              <li key={index}>{note}</li>
            ))}
          </ul>

          {result.recommendedCandidate ? (
            <section style={{ marginBottom: 24 }}>
              <h3>Recommended candidate</h3>
              <CandidateCard evaluation={result.recommendedCandidate} highlight />
            </section>
          ) : null}

          <h3>All candidates (sorted by final score, highest first)</h3>
          <div style={{ display: "grid", gap: 12 }}>
            {[...result.candidates]
              .sort((a, b) => b.scoreBreakdown.finalScore - a.scoreBreakdown.finalScore)
              .map((c, i) => (
                <CandidateCard
                  key={`${c.candidate.thresholdValue}-${i}`}
                  evaluation={c}
                  highlight={c.candidate.label === recommendedLabel}
                />
              ))}
          </div>
        </>
      ) : null}
    </main>
  );
}

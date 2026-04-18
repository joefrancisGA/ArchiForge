"use client";

import { useState } from "react";
import { AlertOperatorToolingRankCue } from "@/components/EnterpriseControlsContextHints";
import { LayerHeader } from "@/components/LayerHeader";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { compareAlertRuleCandidates, simulateAlertRule } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import type {
  RuleCandidateComparisonResult,
  RuleSimulationResult,
  SimulatedAlertOutcome,
} from "@/types/alert-simulation";

const SIMPLE_RULE_TYPES = [
  { value: "CriticalRecommendationCount", label: "Critical / high recommendation count" },
  { value: "NewComplianceGapCount", label: "New compliance gap count" },
  { value: "CostIncreasePercent", label: "Cost increase %" },
  { value: "DeferredHighPriorityRecommendationAgeDays", label: "Deferred high-priority age (days)" },
  { value: "RejectedSecurityRecommendation", label: "Rejected security recommendation" },
  { value: "AcceptanceRateDrop", label: "Acceptance rate below %" },
];

const METRICS = [
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
const TABS = ["simple", "composite", "compare"] as const;
type Tab = (typeof TABS)[number];

function OutcomeTable({ outcomes }: { outcomes: SimulatedAlertOutcome[] }) {
  if (outcomes.length === 0) return <p style={{ color: "#666" }}>No per-run rows.</p>;
  return (
    <div style={{ overflowX: "auto" }}>
      <table
        style={{
          width: "100%",
          borderCollapse: "collapse",
          fontSize: 13,
          marginTop: 8,
        }}
      >
        <thead>
          <tr style={{ textAlign: "left", borderBottom: "1px solid #ccc" }}>
            <th style={{ padding: 6 }}>Run</th>
            <th style={{ padding: 6 }}>Match</th>
            <th style={{ padding: 6 }}>Would create</th>
            <th style={{ padding: 6 }}>Suppressed</th>
            <th style={{ padding: 6 }}>Severity</th>
            <th style={{ padding: 6 }}>Title / description</th>
            <th style={{ padding: 6 }}>Suppression / dedupe</th>
          </tr>
        </thead>
        <tbody>
          {outcomes.map((o, i) => (
            <tr key={`${o.runId ?? "x"}-${i}`} style={{ borderBottom: "1px solid #eee", verticalAlign: "top" }}>
              <td style={{ padding: 6, whiteSpace: "nowrap" }}>{o.runId ?? "—"}</td>
              <td style={{ padding: 6 }}>{o.ruleMatched ? "yes" : "no"}</td>
              <td style={{ padding: 6 }}>{o.wouldCreateAlert ? "yes" : "no"}</td>
              <td style={{ padding: 6 }}>{o.wouldBeSuppressed ? "yes" : "no"}</td>
              <td style={{ padding: 6 }}>{o.severity}</td>
              <td style={{ padding: 6 }}>
                <strong>{o.title}</strong>
                <div style={{ color: "#444", marginTop: 4 }}>{o.description}</div>
                {o.notes?.length ? (
                  <ul style={{ margin: "6px 0 0", paddingLeft: 18, color: "#555" }}>
                    {o.notes.map((n, j) => (
                      <li key={j}>{n}</li>
                    ))}
                  </ul>
                ) : null}
              </td>
              <td style={{ padding: 6, fontSize: 12 }}>
                <div>
                  <strong>Reason:</strong> {o.suppressionReason || "—"}
                </div>
                <div style={{ marginTop: 4 }}>
                  <strong>Dedupe:</strong> {o.deduplicationKey || "—"}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function SummaryBlock({ result }: { result: RuleSimulationResult | null }) {
  if (!result) return null;
  return (
    <section style={{ marginTop: 16 }}>
      <h3 style={{ margin: "0 0 8px" }}>Summary</h3>
      <ul style={{ margin: 0 }}>
        <li>Evaluated runs: {result.evaluatedRunCount}</li>
        <li>Matched: {result.matchedCount}</li>
        <li>Would create alerts: {result.wouldCreateCount}</li>
        <li>Would suppress: {result.wouldSuppressCount}</li>
      </ul>
      {result.summaryNotes?.length ? (
        <ul style={{ marginTop: 8 }}>
          {result.summaryNotes.map((n, i) => (
            <li key={i}>{n}</li>
          ))}
        </ul>
      ) : null}
      <h4 style={{ margin: "16px 0 8px" }}>Outcomes</h4>
      <OutcomeTable outcomes={result.outcomes} />
    </section>
  );
}

export default function AlertSimulationPage() {
  const [tab, setTab] = useState<Tab>("simple");
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);
  const [simpleResult, setSimpleResult] = useState<RuleSimulationResult | null>(null);
  const [compositeResult, setCompositeResult] = useState<RuleSimulationResult | null>(null);
  const [compareResult, setCompareResult] = useState<RuleCandidateComparisonResult | null>(null);

  // Simple
  const [sName, setSName] = useState("Dry-run rule");
  const [sRuleType, setSRuleType] = useState("CostIncreasePercent");
  const [sSeverity, setSSeverity] = useState("Warning");
  const [sThreshold, setSThreshold] = useState(15);
  const [sRecent, setSRecent] = useState(10);
  const [sSlug, setSSlug] = useState("default");
  const [sRunId, setSRunId] = useState("");
  const [sCompareRun, setSCompareRun] = useState("");
  const [sUseHistory, setSUseHistory] = useState(true);

  // Composite
  const [cName, setCName] = useState("Composite dry-run");
  const [cSeverity, setCSeverity] = useState("High");
  const [cJoin, setCJoin] = useState("And");
  const [cSuppression, setCSuppression] = useState(1440);
  const [cCooldown, setCCooldown] = useState(60);
  const [cDedupe, setCDedupe] = useState("RuleAndRun");
  const [cRecent, setCRecent] = useState(10);
  const [cSlug, setCSlug] = useState("default");
  const [cM1, setCM1] = useState("CostIncreasePercent");
  const [cO1, setCO1] = useState("GreaterThanOrEqual");
  const [cV1, setCV1] = useState(15);
  const [cM2, setCM2] = useState("NewComplianceGapCount");
  const [cO2, setCO2] = useState("GreaterThanOrEqual");
  const [cV2, setCV2] = useState(1);

  // Compare simple
  const [cmpName, setCmpName] = useState("Threshold compare");
  const [cmpRuleType, setCmpRuleType] = useState("CostIncreasePercent");
  const [cmpSeverity, setCmpSeverity] = useState("Warning");
  const [cmpA, setCmpA] = useState(10);
  const [cmpB, setCmpB] = useState(20);
  const [cmpRecent, setCmpRecent] = useState(10);
  const [cmpSlug, setCmpSlug] = useState("default");

  function parseOptionalGuid(s: string): string | undefined {
    const t = s.trim();
    if (!t) return undefined;
    return t;
  }

  async function runSimple() {
    setLoading(true);
    setFailure(null);
    setSimpleResult(null);
    try {
      const runId = parseOptionalGuid(sRunId);
      const comparedToRunId = parseOptionalGuid(sCompareRun);
      const res = await simulateAlertRule({
        ruleKind: "Simple",
        simpleRule: {
          ruleId: "00000000-0000-0000-0000-000000000000",
          tenantId: "00000000-0000-0000-0000-000000000000",
          workspaceId: "00000000-0000-0000-0000-000000000000",
          projectId: "00000000-0000-0000-0000-000000000000",
          name: sName.trim() || "Rule",
          ruleType: sRuleType,
          severity: sSeverity,
          thresholdValue: sThreshold,
          isEnabled: true,
          targetChannelType: "DigestOnly",
          metadataJson: "{}",
          createdUtc: new Date().toISOString(),
        },
        runId: runId ?? null,
        comparedToRunId: comparedToRunId ?? null,
        recentRunCount: sRecent,
        useHistoricalWindow: sUseHistory,
        runProjectSlug: sSlug.trim() || "default",
      });
      setSimpleResult(res);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }

  async function runComposite() {
    setLoading(true);
    setFailure(null);
    setCompositeResult(null);
    try {
      const res = await simulateAlertRule({
        ruleKind: "Composite",
        compositeRule: {
          compositeRuleId: "00000000-0000-0000-0000-000000000000",
          tenantId: "00000000-0000-0000-0000-000000000000",
          workspaceId: "00000000-0000-0000-0000-000000000000",
          projectId: "00000000-0000-0000-0000-000000000000",
          name: cName.trim() || "Composite",
          severity: cSeverity,
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
        recentRunCount: cRecent,
        useHistoricalWindow: true,
        runProjectSlug: cSlug.trim() || "default",
      });
      setCompositeResult(res);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }

  async function runCompare() {
    setLoading(true);
    setFailure(null);
    setCompareResult(null);
    try {
      const base = {
        ruleId: "00000000-0000-0000-0000-000000000000",
        tenantId: "00000000-0000-0000-0000-000000000000",
        workspaceId: "00000000-0000-0000-0000-000000000000",
        projectId: "00000000-0000-0000-0000-000000000000",
        name: cmpName.trim() || "Candidate",
        ruleType: cmpRuleType,
        severity: cmpSeverity,
        isEnabled: true,
        targetChannelType: "DigestOnly",
        metadataJson: "{}",
        createdUtc: new Date().toISOString(),
      };
      const res = await compareAlertRuleCandidates({
        ruleKind: "Simple",
        candidateA_SimpleRule: { ...base, thresholdValue: cmpA },
        candidateB_SimpleRule: { ...base, thresholdValue: cmpB },
        recentRunCount: cmpRecent,
        runProjectSlug: cmpSlug.trim() || "default",
      });
      setCompareResult(res);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }

  return (
    <main style={{ maxWidth: 1100 }}>
      <LayerHeader pageKey="alert-simulation" />
      <h2 style={{ marginTop: 0 }}>Alert rule simulation</h2>
      <p style={{ color: "#444", fontSize: 14, maxWidth: "42rem" }}>
        <strong>What-if</strong> against recent runs (or one run ID): same evaluators and suppression as production;{" "}
        <strong>no</strong> alerts persisted or delivered.
      </p>
      <AlertOperatorToolingRankCue />

      <div style={{ display: "flex", gap: 8, marginBottom: 20, flexWrap: "wrap" }}>
        {TABS.map((t) => (
          <button
            key={t}
            type="button"
            onClick={() => setTab(t)}
            style={{
              padding: "8px 14px",
              borderRadius: 6,
              border: tab === t ? "2px solid #333" : "1px solid #ccc",
              background: tab === t ? "#f4f4f4" : "#fff",
              cursor: "pointer",
              textTransform: "capitalize",
            }}
          >
            {t}
          </button>
        ))}
      </div>

      {failure !== null ? (
        <div role="alert">
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
        </div>
      ) : null}

      {tab === "simple" ? (
        <section>
          <h3 style={{ marginTop: 0 }}>Simple rule</h3>
          <div style={{ display: "grid", gap: 12, maxWidth: 640 }}>
            <label>
              Name
              <input
                value={sName}
                onChange={(e) => setSName(e.target.value)}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
            <label>
              Rule type
              <select
                value={sRuleType}
                onChange={(e) => setSRuleType(e.target.value)}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              >
                {SIMPLE_RULE_TYPES.map((r) => (
                  <option key={r.value} value={r.value}>
                    {r.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Severity
              <select
                value={sSeverity}
                onChange={(e) => setSSeverity(e.target.value)}
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
              Threshold
              <input
                type="number"
                value={sThreshold}
                onChange={(e) => setSThreshold(Number(e.target.value))}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
            <label>
              Recent run count (1–50)
              <input
                type="number"
                min={1}
                max={50}
                value={sRecent}
                onChange={(e) => setSRecent(Number(e.target.value))}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
            <label>
              Run project slug
              <input
                value={sSlug}
                onChange={(e) => setSSlug(e.target.value)}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
            <label>
              Specific run ID (optional; overrides recent list)
              <input
                value={sRunId}
                onChange={(e) => setSRunId(e.target.value)}
                placeholder="00000000-0000-0000-0000-000000000000"
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
            <label>
              Compared-to run ID (optional)
              <input
                value={sCompareRun}
                onChange={(e) => setSCompareRun(e.target.value)}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
            <label style={{ display: "flex", alignItems: "center", gap: 8 }}>
              <input
                type="checkbox"
                checked={sUseHistory}
                onChange={(e) => setSUseHistory(e.target.checked)}
              />
              Use historical window (recent runs)
            </label>
            <button
              type="button"
              onClick={() => void runSimple()}
              disabled={loading}
              style={{ padding: "10px 16px", cursor: loading ? "wait" : "pointer" }}
            >
              {loading ? "Running…" : "Simulate"}
            </button>
          </div>
          <SummaryBlock result={simpleResult} />
        </section>
      ) : null}

      {tab === "composite" ? (
        <section>
          <h3 style={{ marginTop: 0 }}>Composite rule</h3>
          <div style={{ display: "grid", gap: 12, maxWidth: 720 }}>
            <label>
              Name
              <input
                value={cName}
                onChange={(e) => setCName(e.target.value)}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
            <label>
              Severity
              <select
                value={cSeverity}
                onChange={(e) => setCSeverity(e.target.value)}
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
                {METRICS.map((m) => (
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
                {METRICS.map((m) => (
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
            <label>
              Recent run count
              <input
                type="number"
                min={1}
                max={50}
                value={cRecent}
                onChange={(e) => setCRecent(Number(e.target.value))}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
            <label>
              Run project slug
              <input
                value={cSlug}
                onChange={(e) => setCSlug(e.target.value)}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
            <button
              type="button"
              onClick={() => void runComposite()}
              disabled={loading}
              style={{ padding: "10px 16px", cursor: loading ? "wait" : "pointer" }}
            >
              {loading ? "Running…" : "Simulate"}
            </button>
          </div>
          <SummaryBlock result={compositeResult} />
        </section>
      ) : null}

      {tab === "compare" ? (
        <section>
          <h3 style={{ marginTop: 0 }}>Compare simple-rule thresholds</h3>
          <p style={{ color: "#555", fontSize: 14 }}>
            Same rule type and severity; only thresholds differ. Useful for tuning (e.g. 10 vs 20).
          </p>
          <div style={{ display: "grid", gap: 12, maxWidth: 640 }}>
            <label>
              Name
              <input
                value={cmpName}
                onChange={(e) => setCmpName(e.target.value)}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
            <label>
              Rule type
              <select
                value={cmpRuleType}
                onChange={(e) => setCmpRuleType(e.target.value)}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              >
                {SIMPLE_RULE_TYPES.map((r) => (
                  <option key={r.value} value={r.value}>
                    {r.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Severity
              <select
                value={cmpSeverity}
                onChange={(e) => setCmpSeverity(e.target.value)}
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
              Candidate A threshold
              <input
                type="number"
                value={cmpA}
                onChange={(e) => setCmpA(Number(e.target.value))}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
            <label>
              Candidate B threshold
              <input
                type="number"
                value={cmpB}
                onChange={(e) => setCmpB(Number(e.target.value))}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
            <label>
              Recent run count
              <input
                type="number"
                min={1}
                max={50}
                value={cmpRecent}
                onChange={(e) => setCmpRecent(Number(e.target.value))}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
            <label>
              Run project slug
              <input
                value={cmpSlug}
                onChange={(e) => setCmpSlug(e.target.value)}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
            <button
              type="button"
              onClick={() => void runCompare()}
              disabled={loading}
              style={{ padding: "10px 16px", cursor: loading ? "wait" : "pointer" }}
            >
              {loading ? "Running…" : "Compare candidates"}
            </button>
          </div>
          {compareResult ? (
            <div style={{ marginTop: 24 }}>
              <h3>Comparison notes</h3>
              <ul>
                {compareResult.summaryNotes.map((n, i) => (
                  <li key={i}>{n}</li>
                ))}
              </ul>
              <h3>Candidate A</h3>
              <SummaryBlock result={compareResult.candidateA} />
              <h3>Candidate B</h3>
              <SummaryBlock result={compareResult.candidateB} />
            </div>
          ) : null}
        </section>
      ) : null}
    </main>
  );
}

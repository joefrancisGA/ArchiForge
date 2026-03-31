"use client";

import { useCallback, useEffect, useState } from "react";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { createAlertRule, listAlertRules } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import type { AlertRule } from "@/types/alerts";

const RULE_TYPES = [
  { value: "CriticalRecommendationCount", label: "Critical / high recommendation count" },
  { value: "NewComplianceGapCount", label: "New compliance gap count (security deltas)" },
  { value: "CostIncreasePercent", label: "Cost increase %" },
  { value: "DeferredHighPriorityRecommendationAgeDays", label: "Deferred high-priority age (days)" },
  { value: "RejectedSecurityRecommendation", label: "Rejected security recommendation" },
  { value: "AcceptanceRateDrop", label: "Acceptance rate below %" },
];

const SEVERITIES = ["Info", "Warning", "High", "Critical"];

export default function AlertRulesPage() {
  const [items, setItems] = useState<AlertRule[]>([]);
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);

  const [name, setName] = useState("Architecture alert rule");
  const [ruleType, setRuleType] = useState("CriticalRecommendationCount");
  const [severity, setSeverity] = useState("Warning");
  const [threshold, setThreshold] = useState(3);

  const load = useCallback(async () => {
    setLoading(true);
    setFailure(null);
    try {
      const data = await listAlertRules();
      setItems(data);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function onCreate() {
    setFailure(null);
    try {
      await createAlertRule({
        name: name.trim() || "Rule",
        ruleType,
        severity,
        thresholdValue: threshold,
        isEnabled: true,
      });
      await load();
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    }
  }

  return (
    <main style={{ maxWidth: 800 }}>
      <h2 style={{ marginTop: 0 }}>Alert rules</h2>
      <p style={{ color: "#444", fontSize: 14 }}>
        Typed, deterministic rules evaluated on each scheduled advisory scan. Threshold meaning depends on rule type
        (count, percent, or days).
      </p>

      {failure !== null ? (
        <div role="alert">
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
        </div>
      ) : null}

      <div style={{ display: "grid", gap: 12, maxWidth: 700, marginBottom: 24 }}>
        <label>
          Name
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
          />
        </label>
        <label>
          Rule type
          <select
            value={ruleType}
            onChange={(e) => setRuleType(e.target.value)}
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
          >
            {RULE_TYPES.map((r) => (
              <option key={r.value} value={r.value}>
                {r.label}
              </option>
            ))}
          </select>
        </label>
        <label>
          Severity (when triggered)
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
          Threshold value
          <input
            type="number"
            step="any"
            value={threshold}
            onChange={(e) => setThreshold(Number(e.target.value))}
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
          />
        </label>
        <button type="button" onClick={() => void onCreate()} disabled={loading}>
          Create rule
        </button>
      </div>

      <button type="button" onClick={() => void load()} disabled={loading} style={{ marginBottom: 16 }}>
        {loading ? "Loading…" : "Refresh"}
      </button>

      <h3>Rules in scope</h3>
      <div style={{ display: "grid", gap: 12 }}>
        {items.length === 0 ? (
          <p style={{ color: "#666" }}>None yet.</p>
        ) : (
          items.map((r) => (
            <div
              key={r.ruleId}
              style={{
                border: "1px solid #ddd",
                borderRadius: 8,
                padding: 12,
                background: "#fff",
              }}
            >
              <strong>{r.name}</strong>
              <div style={{ fontSize: 14, marginTop: 8 }}>
                <div>Type: {r.ruleType}</div>
                <div>Severity: {r.severity}</div>
                <div>Threshold: {r.thresholdValue}</div>
                <div>Enabled: {String(r.isEnabled)}</div>
                <div>Channel: {r.targetChannelType}</div>
              </div>
            </div>
          ))
        )}
      </div>
    </main>
  );
}

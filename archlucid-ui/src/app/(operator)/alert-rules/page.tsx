"use client";

import { useCallback, useEffect, useState } from "react";
import { AlertOperatorToolingRankCue } from "@/components/EnterpriseControlsContextHints";
import { LayerHeader } from "@/components/LayerHeader";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { useEnterpriseMutationCapability } from "@/hooks/use-enterprise-mutation-capability";
import { createAlertRule, listAlertRules } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import {
  alertRulesChangeConfigurationLeadReaderLine,
  alertRulesDefinedListEmptyOperatorLine,
  alertRulesDefinedListEmptyReaderLine,
  alertToolingConfigureSectionSubline,
  enterpriseMutationControlDisabledTitle,
} from "@/lib/enterprise-controls-context-copy";
import { cn } from "@/lib/utils";
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
  const canMutateAlertRules = useEnterpriseMutationCapability();
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
    if (!canMutateAlertRules) {
      return;
    }

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
      <LayerHeader pageKey="alert-rules" />
      <h2 style={{ marginTop: 0 }}>Alert rules</h2>
      <p className="max-w-prose text-sm text-neutral-600 dark:text-neutral-400">
        Current rules first; create and thresholds in the form below.
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

      <div className="flex flex-col gap-6">
        <section className="min-w-0" aria-labelledby="alert-rules-current-heading">
          <h3 id="alert-rules-current-heading" style={{ fontSize: "1rem", marginTop: 4, marginBottom: 8 }}>
            Current rules
          </h3>
          <button type="button" onClick={() => void load()} disabled={loading} style={{ marginBottom: 8 }}>
            {loading ? "Loading…" : "Refresh"}
          </button>
          <div style={{ display: "grid", gap: 12 }}>
            {items.length === 0 ? (
              <p style={{ color: "#666", maxWidth: "40rem", fontSize: 14 }}>
                {canMutateAlertRules ? alertRulesDefinedListEmptyOperatorLine : alertRulesDefinedListEmptyReaderLine}
              </p>
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
        </section>

        <section
          className={cn("min-w-0", !canMutateAlertRules && "opacity-90")}
          aria-labelledby="alert-rules-change-heading"
        >
          <h3 id="alert-rules-change-heading" style={{ fontSize: "1rem", marginTop: 4, marginBottom: 8 }}>
            {canMutateAlertRules ? "Change configuration" : "Change configuration (operator access)"}
          </h3>
          {canMutateAlertRules ? null : (
            <p style={{ color: "#64748b", fontSize: 12, maxWidth: "40rem", marginTop: 0, marginBottom: 8 }}>
              {alertRulesChangeConfigurationLeadReaderLine}
            </p>
          )}
          <p style={{ color: "#64748b", fontSize: 12, maxWidth: "40rem", marginTop: 0, marginBottom: 10 }}>
            {alertToolingConfigureSectionSubline}
          </p>
          <div style={{ display: "grid", gap: 12, maxWidth: 700, marginBottom: 16 }}>
            <label>
              Name
              <input
                value={name}
                onChange={(e) => setName(e.target.value)}
                disabled={!canMutateAlertRules}
                title={canMutateAlertRules ? undefined : enterpriseMutationControlDisabledTitle}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
            <label>
              Rule type
              <select
                value={ruleType}
                onChange={(e) => setRuleType(e.target.value)}
                disabled={!canMutateAlertRules}
                title={canMutateAlertRules ? undefined : enterpriseMutationControlDisabledTitle}
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
                disabled={!canMutateAlertRules}
                title={canMutateAlertRules ? undefined : enterpriseMutationControlDisabledTitle}
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
                disabled={!canMutateAlertRules}
                title={canMutateAlertRules ? undefined : enterpriseMutationControlDisabledTitle}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
            <button
              type="button"
              onClick={() => void onCreate()}
              disabled={loading || !canMutateAlertRules}
              title={canMutateAlertRules ? undefined : enterpriseMutationControlDisabledTitle}
              className={cn(
                !canMutateAlertRules &&
                  "rounded border border-neutral-300 bg-neutral-50 text-neutral-600 dark:border-neutral-600 dark:bg-neutral-900/50 dark:text-neutral-400",
              )}
            >
              Create rule
            </button>
          </div>
        </section>
      </div>
    </main>
  );
}

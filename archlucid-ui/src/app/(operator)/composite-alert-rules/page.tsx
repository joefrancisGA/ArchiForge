"use client";

import { useCallback, useEffect, useState } from "react";
import { AlertOperatorToolingRankCue } from "@/components/EnterpriseControlsContextHints";
import { LayerHeader } from "@/components/LayerHeader";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { useEnterpriseMutationCapability } from "@/hooks/use-enterprise-mutation-capability";
import { createCompositeAlertRule, listCompositeAlertRules } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import {
  alertToolingConfigureSectionSubline,
  compositeRulesChangeConfigurationLeadReaderLine,
  compositeRulesDefinedListEmptyOperatorLine,
  compositeRulesDefinedListEmptyReaderLine,
  enterpriseMutationControlDisabledTitle,
} from "@/lib/enterprise-controls-context-copy";
import { cn } from "@/lib/utils";
import type { CompositeAlertRule } from "@/types/composite-alert-rules";

const METRICS = [
  { value: "CriticalRecommendationCount", label: "Critical/high recommendation count" },
  { value: "NewComplianceGapCount", label: "New compliance gap count (security deltas)" },
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
  { value: "Equal", label: "=" },
  { value: "NotEqual", label: "≠" },
];

const SEVERITIES = ["Info", "Warning", "High", "Critical"];
const JOIN_OPS = [
  { value: "And", label: "All conditions (AND)" },
  { value: "Or", label: "Any condition (OR)" },
];

const DEDUPE = [
  { value: "RuleOnly", label: "Rule only" },
  { value: "RuleAndRun", label: "Rule + run" },
  { value: "RuleAndComparison", label: "Rule + run + comparison" },
];

export default function CompositeAlertRulesPage() {
  const canMutateComposite = useEnterpriseMutationCapability();
  const [items, setItems] = useState<CompositeAlertRule[]>([]);
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);

  const [name, setName] = useState("Cost + compliance composite");
  const [severity, setSeverity] = useState("High");
  const [joinOperator, setJoinOperator] = useState("And");
  const [suppressionWindowMinutes, setSuppressionWindowMinutes] = useState(1440);
  const [cooldownMinutes, setCooldownMinutes] = useState(60);
  const [reopenDeltaThreshold, setReopenDeltaThreshold] = useState(0);
  const [dedupeScope, setDedupeScope] = useState("RuleAndRun");

  const [m1, setM1] = useState("CostIncreasePercent");
  const [o1, setO1] = useState("GreaterThanOrEqual");
  const [v1, setV1] = useState(10);

  const [m2, setM2] = useState("NewComplianceGapCount");
  const [o2, setO2] = useState("GreaterThanOrEqual");
  const [v2, setV2] = useState(1);

  const load = useCallback(async () => {
    setLoading(true);
    setFailure(null);
    try {
      const data = await listCompositeAlertRules();
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
    if (!canMutateComposite) {
      return;
    }

    setFailure(null);
    try {
      await createCompositeAlertRule({
        name: name.trim() || "Composite rule",
        severity,
        operator: joinOperator,
        suppressionWindowMinutes,
        cooldownMinutes,
        reopenDeltaThreshold,
        dedupeScope,
        conditions: [
          { metricType: m1, operator: o1, thresholdValue: v1 },
          { metricType: m2, operator: o2, thresholdValue: v2 },
        ],
      });
      await load();
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    }
  }

  return (
    <main style={{ maxWidth: 900 }}>
      <LayerHeader pageKey="composite-alert-rules" />
      <h2 style={{ marginTop: 0 }}>Composite alert rules</h2>
      <p className="max-w-prose text-sm text-neutral-600 dark:text-neutral-400">
        Defined composites first; create with the form below.
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

      <div className="flex flex-col gap-8">
        <section className="min-w-0" aria-labelledby="composite-rules-current-heading">
          <h3 id="composite-rules-current-heading" style={{ fontSize: "1rem", marginTop: 8 }}>
            Current composite rules
          </h3>
          <button type="button" onClick={() => void load()} disabled={loading} style={{ marginBottom: 16 }}>
            {loading ? "Loading…" : "Refresh"}
          </button>
          <div style={{ display: "grid", gap: 14 }}>
            {items.length === 0 ? (
              <p style={{ color: "#666", maxWidth: "40rem", fontSize: 14 }}>
                {canMutateComposite ? compositeRulesDefinedListEmptyOperatorLine : compositeRulesDefinedListEmptyReaderLine}
              </p>
            ) : (
              items.map((r) => (
                <div
                  key={r.compositeRuleId}
                  style={{ border: "1px solid #ddd", borderRadius: 8, padding: 12, background: "#fff" }}
                >
                  <strong>{r.name}</strong>
                  <div style={{ fontSize: 14, marginTop: 8 }}>
                    <div>
                      Join: {r.operator} · Severity: {r.severity} · Enabled: {String(r.isEnabled)}
                    </div>
                    <div>
                      Suppression: {r.suppressionWindowMinutes} min · Cooldown: {r.cooldownMinutes} min · Dedupe:{" "}
                      {r.dedupeScope}
                    </div>
                    <ul style={{ marginTop: 8 }}>
                      {(r.conditions ?? []).map((c) => (
                        <li key={c.conditionId ?? `${c.metricType}-${c.thresholdValue}`}>
                          {c.metricType} {c.operator} {c.thresholdValue}
                        </li>
                      ))}
                    </ul>
                  </div>
                </div>
              ))
            )}
          </div>
        </section>

        <section
          className={cn("min-w-0", !canMutateComposite && "opacity-90")}
          aria-labelledby="composite-rules-change-heading"
        >
          <h3 id="composite-rules-change-heading" style={{ fontSize: "1rem" }}>
            {canMutateComposite ? "Change configuration" : "Change configuration (operator access)"}
          </h3>
          {canMutateComposite ? null : (
            <p style={{ color: "#64748b", fontSize: 12, maxWidth: "42rem", marginTop: 4, marginBottom: 8 }}>
              {compositeRulesChangeConfigurationLeadReaderLine}
            </p>
          )}
          <p style={{ color: "#64748b", fontSize: 12, maxWidth: "42rem", marginTop: 0, marginBottom: 10 }}>
            {alertToolingConfigureSectionSubline}
          </p>
      <fieldset
        disabled={!canMutateComposite}
        title={canMutateComposite ? undefined : enterpriseMutationControlDisabledTitle}
        aria-label="New composite rule form"
        style={{ border: "none", margin: 0, padding: 0 }}
      >
      <div style={{ display: "grid", gap: 12, maxWidth: 720, marginBottom: 28 }}>
        <label>
          Name
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
          />
        </label>
        <label>
          Severity when fired
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
          Combine conditions
          <select
            value={joinOperator}
            onChange={(e) => setJoinOperator(e.target.value)}
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
          >
            {JOIN_OPS.map((j) => (
              <option key={j.value} value={j.value}>
                {j.label}
              </option>
            ))}
          </select>
        </label>

        <fieldset style={{ border: "1px solid #ccc", borderRadius: 8, padding: 12 }}>
          <legend>Condition 1</legend>
          <div style={{ display: "grid", gap: 8 }}>
            <label>
              Metric
              <select
                value={m1}
                onChange={(e) => setM1(e.target.value)}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              >
                {METRICS.map((x) => (
                  <option key={x.value} value={x.value}>
                    {x.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Operator
              <select
                value={o1}
                onChange={(e) => setO1(e.target.value)}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              >
                {COND_OPS.map((x) => (
                  <option key={x.value} value={x.value}>
                    {x.label} {x.value}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Threshold value
              <input
                type="number"
                step="any"
                value={v1}
                onChange={(e) => setV1(Number(e.target.value))}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
          </div>
        </fieldset>

        <fieldset style={{ border: "1px solid #ccc", borderRadius: 8, padding: 12 }}>
          <legend>Condition 2</legend>
          <div style={{ display: "grid", gap: 8 }}>
            <label>
              Metric
              <select
                value={m2}
                onChange={(e) => setM2(e.target.value)}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              >
                {METRICS.map((x) => (
                  <option key={x.value} value={x.value}>
                    {x.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Operator
              <select
                value={o2}
                onChange={(e) => setO2(e.target.value)}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              >
                {COND_OPS.map((x) => (
                  <option key={x.value} value={x.value}>
                    {x.label} {x.value}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Threshold value
              <input
                type="number"
                step="any"
                value={v2}
                onChange={(e) => setV2(Number(e.target.value))}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
          </div>
        </fieldset>

        <label>
          Suppression window (minutes)
          <input
            type="number"
            value={suppressionWindowMinutes}
            onChange={(e) => setSuppressionWindowMinutes(Number(e.target.value))}
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
          />
        </label>
        <label>
          Cooldown (minutes)
          <input
            type="number"
            value={cooldownMinutes}
            onChange={(e) => setCooldownMinutes(Number(e.target.value))}
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
          />
        </label>
        <label>
          Reopen delta threshold (reserved for future use)
          <input
            type="number"
            step="any"
            value={reopenDeltaThreshold}
            onChange={(e) => setReopenDeltaThreshold(Number(e.target.value))}
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
          />
        </label>
        <label>
          Dedupe scope
          <select
            value={dedupeScope}
            onChange={(e) => setDedupeScope(e.target.value)}
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
          >
            {DEDUPE.map((d) => (
              <option key={d.value} value={d.value}>
                {d.label}
              </option>
            ))}
          </select>
        </label>

        <button
          type="button"
          onClick={() => void onCreate()}
          disabled={loading || !canMutateComposite}
          title={canMutateComposite ? undefined : enterpriseMutationControlDisabledTitle}
          className={cn(
            !canMutateComposite &&
              "rounded border border-neutral-300 bg-neutral-50 text-neutral-600 dark:border-neutral-600 dark:bg-neutral-900/50 dark:text-neutral-400",
          )}
        >
          Create composite rule
        </button>
      </div>
      </fieldset>
        </section>
      </div>
    </main>
  );
}

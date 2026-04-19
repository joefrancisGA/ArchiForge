"use client";

import { useCallback, useEffect, useState } from "react";
import { GovernanceResolutionRankCue } from "@/components/EnterpriseControlsContextHints";
import { LayerHeader } from "@/components/LayerHeader";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { useEnterpriseMutationCapability } from "@/hooks/use-enterprise-mutation-capability";
import {
  governanceResolutionChangeRelatedControlsLead,
  governanceResolutionChangeRelatedControlsReaderSupplement,
  governanceResolutionPageSubline,
} from "@/lib/enterprise-controls-context-copy";
import { cn } from "@/lib/utils";
import { getGovernanceResolution } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import type { EffectiveGovernanceResolutionResult } from "@/types/governance-resolution";

export default function GovernanceResolutionPage() {
  /** Same Execute floor as Policy packs / Workflow writes — shapes “Change related controls” emphasis only (GET refresh stays allowed). */
  const canMutateEnterprisePolicySurfaces = useEnterpriseMutationCapability();
  const [data, setData] = useState<EffectiveGovernanceResolutionResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setFailure(null);
    try {
      const r = await getGovernanceResolution();
      setData(r);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <main style={{ maxWidth: 1100 }}>
      <LayerHeader pageKey="governance-resolution" />
      <h2 style={{ marginTop: 0 }}>Governance resolution</h2>
      <p className="mb-1 max-w-prose text-xs text-neutral-500 dark:text-neutral-400">
        {governanceResolutionPageSubline}
      </p>
      <GovernanceResolutionRankCue className="mb-3" />
      {failure !== null ? (
        <div role="alert">
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
        </div>
      ) : null}

      <section style={{ marginBottom: 28 }} aria-labelledby="governance-effective-heading">
        <h3 id="governance-effective-heading">Effective policy</h3>
        <h4 style={{ marginTop: 8, marginBottom: 8, fontSize: "1rem" }}>Summary notes</h4>
        <ul style={{ fontSize: 14 }}>
          {(data?.notes ?? []).length === 0 ? (
            <li style={{ color: "#666" }}>—</li>
          ) : (
            data!.notes.map((n) => <li key={n}>{n}</li>)
          )}
        </ul>
        <h4 style={{ marginTop: 20, marginBottom: 8, fontSize: "1rem" }}>Effective content</h4>
        <pre
          style={{
            background: "#f5f5f5",
            padding: 12,
            overflow: "auto",
            fontSize: 12,
            maxHeight: 400,
          }}
        >
          {data ? JSON.stringify(data.effectiveContent, null, 2) : "—"}
        </pre>
        <details style={{ marginTop: 20, marginBottom: 0, maxWidth: "52rem" }}>
          <summary style={{ cursor: "pointer", color: "#444", fontSize: 14, fontWeight: 600 }}>
            How resolution orders packs (overrides, pins, ties, conflicts)
          </summary>
          <p style={{ color: "#444", fontSize: 14, marginTop: 8 }}>
            For the current scope: <strong>Project</strong> overrides <strong>Workspace</strong> overrides{" "}
            <strong>Tenant</strong>. Pinned assignments rank above unpinned within the same tier; newer assignments break
            ties. Conflicts are flagged when multiple packs define the same item or disagree on a value.
          </p>
        </details>
      </section>

      <section style={{ marginBottom: 28 }} aria-labelledby="governance-resolution-details-heading">
        <h3 id="governance-resolution-details-heading">Resolution details</h3>
        <h4 style={{ marginTop: 0, marginBottom: 8, fontSize: "1rem" }}>Conflicts ({data?.conflicts.length ?? 0})</h4>
        {(data?.conflicts ?? []).length === 0 ? (
          <p style={{ color: "#666", fontSize: 14 }}>No conflicts detected.</p>
        ) : (
          <ul style={{ listStyle: "none", padding: 0, display: "grid", gap: 12 }}>
            {data!.conflicts.map((c, i) => (
              <li
                key={`${c.itemType}-${c.itemKey}-${i}`}
                style={{ border: "1px solid #e0c4c4", borderRadius: 8, padding: 12, background: "#fff8f8" }}
              >
                <div>
                  <strong>{c.conflictType}</strong> — {c.itemType} <code>{c.itemKey}</code>
                </div>
                <div style={{ fontSize: 13, color: "#555", marginTop: 6 }}>{c.description}</div>
                <details style={{ marginTop: 8, fontSize: 12 }}>
                  <summary>Candidates</summary>
                  <pre style={{ overflow: "auto", maxHeight: 200 }}>{JSON.stringify(c.candidates, null, 2)}</pre>
                </details>
              </li>
            ))}
          </ul>
        )}
        <h4 style={{ marginTop: 24, marginBottom: 8, fontSize: "1rem" }}>Resolution decisions ({data?.decisions.length ?? 0})</h4>
        <div style={{ display: "grid", gap: 10 }}>
          {(data?.decisions ?? []).map((d, i) => (
            <article
              key={`${d.itemType}-${d.itemKey}-${i}`}
              style={{ border: "1px solid #ddd", borderRadius: 8, padding: 12, background: "#fafafa" }}
            >
              <div style={{ fontSize: 15 }}>
                <strong>{d.itemType}</strong> <code>{d.itemKey}</code>
              </div>
              <div style={{ fontSize: 13, marginTop: 6 }}>
                Winner: <strong>{d.winningPolicyPackName}</strong> ({d.winningVersion}) — scope{" "}
                <code>{d.winningScopeLevel}</code>
              </div>
              <div style={{ fontSize: 13, color: "#333", marginTop: 6 }}>{d.resolutionReason}</div>
              <details style={{ marginTop: 8, fontSize: 12 }}>
                <summary>All candidates</summary>
                <pre style={{ overflow: "auto", maxHeight: 220 }}>{JSON.stringify(d.candidates, null, 2)}</pre>
              </details>
            </article>
          ))}
        </div>
      </section>

      <section
        aria-labelledby="governance-change-controls-heading"
        className={cn(
          !canMutateEnterprisePolicySurfaces &&
            "rounded-md border border-neutral-200/80 bg-neutral-50/60 p-3 dark:border-neutral-700/60 dark:bg-neutral-900/35",
        )}
      >
        <h3 id="governance-change-controls-heading">Change related controls</h3>
        <p style={{ color: "#64748b", fontSize: 13, maxWidth: "42rem", marginTop: 0, marginBottom: 10 }}>
          {governanceResolutionChangeRelatedControlsLead}
        </p>
        {!canMutateEnterprisePolicySurfaces ? (
          <p className="mb-2 max-w-prose text-xs text-neutral-500 dark:text-neutral-400" role="note">
            {governanceResolutionChangeRelatedControlsReaderSupplement}
          </p>
        ) : null}
        <p style={{ marginBottom: 0 }}>
          <button type="button" onClick={() => void load()} disabled={loading}>
            {loading ? "Loading…" : "Refresh"}
          </button>
        </p>
      </section>
    </main>
  );
}

"use client";

import { useCallback, useEffect, useState } from "react";
import { GlossaryTooltip } from "@/components/GlossaryTooltip";
import { OperatorPageHeader } from "@/components/OperatorPageHeader";
import { GovernanceResolutionRankCue } from "@/components/EnterpriseControlsContextHints";
import { LayerHeader } from "@/components/LayerHeader";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
import { useEnterpriseMutationCapability } from "@/hooks/use-enterprise-mutation-capability";
import {
  governanceResolutionChangeRelatedControlsLead,
  governanceResolutionChangeRelatedControlsReaderSupplement,
  governanceResolutionEffectivePolicyHeadingOperator,
  governanceResolutionEffectivePolicyHeadingReader,
  governanceResolutionPageLeadOperator,
  governanceResolutionPageLeadReader,
  governanceResolutionRefreshButtonTitle,
  governanceResolutionResolutionDetailsHeadingOperator,
  governanceResolutionResolutionDetailsHeadingReader,
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
    <main className="max-w-6xl">
      <LayerHeader pageKey="governance-resolution" density="compact" />
      <OperatorPageHeader
        title="Governance resolution"
        subtitle={canMutateEnterprisePolicySurfaces ? governanceResolutionPageLeadOperator : governanceResolutionPageLeadReader}
      />
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

      <section className="mb-7" aria-labelledby="governance-effective-heading">
        <h3 id="governance-effective-heading">
          <GlossaryTooltip termKey="effective_governance">
            {canMutateEnterprisePolicySurfaces
              ? governanceResolutionEffectivePolicyHeadingOperator
              : governanceResolutionEffectivePolicyHeadingReader}
          </GlossaryTooltip>
        </h3>
        <h4 className="mt-2 mb-2 text-base">Summary notes</h4>
        <ul className="text-sm">
          {(data?.notes ?? []).length === 0 ? (
            <li className="text-neutral-500 dark:text-neutral-400">—</li>
          ) : (
            data!.notes.map((n) => <li key={n}>{n}</li>)
          )}
        </ul>
        <h4 className="mt-5 mb-2 text-base">Effective content</h4>
        <pre
          className="bg-neutral-100 dark:bg-neutral-800 p-3 overflow-auto text-xs max-h-[400px]"
        >
          {data ? JSON.stringify(data.effectiveContent, null, 2) : "—"}
        </pre>
        <details className="mt-5 mb-0 max-w-3xl">
          <summary className="cursor-pointer text-neutral-600 dark:text-neutral-400 text-sm font-semibold">
            How packs are ordered (scope, pins, ties)
          </summary>
          <p className="text-neutral-600 dark:text-neutral-400 text-sm mt-2">
            <strong>Project</strong> wins over <strong>Workspace</strong> over <strong>Tenant</strong>. Pinned beats
            unpinned at the same tier; newest assignment breaks ties. Conflicts surface when definitions disagree.
          </p>
        </details>
      </section>

      <section className="mb-7" aria-labelledby="governance-resolution-details-heading">
        <h3 id="governance-resolution-details-heading">
          {canMutateEnterprisePolicySurfaces
            ? governanceResolutionResolutionDetailsHeadingOperator
            : governanceResolutionResolutionDetailsHeadingReader}
        </h3>
        <h4 className="mt-0 mb-2 text-base">Conflicts ({data?.conflicts.length ?? 0})</h4>
        {(data?.conflicts ?? []).length === 0 ? (
          <p className="text-neutral-500 dark:text-neutral-400 text-sm">No conflicts detected.</p>
        ) : (
          <ul className="list-none p-0 grid gap-3">
            {data!.conflicts.map((c, i) => (
              <li
                key={`${c.itemType}-${c.itemKey}-${i}`}
                className="border border-red-200 dark:border-red-900 rounded-lg p-3 bg-red-50/60 dark:bg-red-950/20"
              >
                <div>
                  <strong>{c.conflictType}</strong> — {c.itemType} <code>{c.itemKey}</code>
                </div>
                <div className="text-[13px] text-neutral-600 dark:text-neutral-400 mt-1.5">{c.description}</div>
                <details className="mt-2 text-xs">
                  <summary>Candidates</summary>
                  <pre className="overflow-auto max-h-[200px]">{JSON.stringify(c.candidates, null, 2)}</pre>
                </details>
              </li>
            ))}
          </ul>
        )}
        <h4 className="mt-6 mb-2 text-base">Resolution decisions ({data?.decisions.length ?? 0})</h4>
        <div className="grid gap-2.5">
          {(data?.decisions ?? []).map((d, i) => (
            <article
              key={`${d.itemType}-${d.itemKey}-${i}`}
              className="border border-neutral-200 dark:border-neutral-700 rounded-lg p-3 bg-neutral-50 dark:bg-neutral-950"
            >
              <div className="text-[15px]">
                <strong>{d.itemType}</strong> <code>{d.itemKey}</code>
              </div>
              <div className="text-[13px] mt-1.5">
                Winner: <strong>{d.winningPolicyPackName}</strong> ({d.winningVersion}) — scope{" "}
                <code>{d.winningScopeLevel}</code>
              </div>
              <div className="text-[13px] text-neutral-700 dark:text-neutral-300 mt-1.5">{d.resolutionReason}</div>
              <details className="mt-2 text-xs">
                <summary>All candidates</summary>
                <pre className="overflow-auto max-h-[220px]">{JSON.stringify(d.candidates, null, 2)}</pre>
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
        <p className="text-neutral-500 dark:text-neutral-400 text-[13px] max-w-2xl mt-0 mb-2.5">
          {governanceResolutionChangeRelatedControlsLead}
        </p>
        {!canMutateEnterprisePolicySurfaces ? (
          <p className="mb-2 max-w-prose text-xs text-neutral-500 dark:text-neutral-400" role="note">
            {governanceResolutionChangeRelatedControlsReaderSupplement}
          </p>
        ) : null}
        <div className="mb-0">
          <Button
            type="button"
            variant="secondary"
            size="sm"
            title={governanceResolutionRefreshButtonTitle}
            onClick={() => void load()}
            disabled={loading}
          >
            {loading ? "Loading…" : "Refresh"}
          </Button>
        </div>
      </section>
    </main>
  );
}

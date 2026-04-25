"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

export type AgentExecutionCostPreviewPayload = {
  mode: string;
  maxCompletionTokens: number;
  estimatedCostUsd: number | null;
  deploymentName: string | null;
};

const DOCS_URL =
  "https://github.com/joefrancisGA/ArchLucid/blob/main/docs/library/PER_TENANT_COST_MODEL.md";

export type RunWizardCostPreviewCardProps = {
  /** Defaults to same-origin BFF proxy path. */
  previewUrl?: string;
};

/**
 * Host-level AOAI cost preview for the review step; hidden when API reports Simulator mode.
 */
export function RunWizardCostPreviewCard(props: RunWizardCostPreviewCardProps = {}) {
  const previewUrl = props.previewUrl ?? "/api/proxy/v1/agent-execution/cost-preview";
  const [data, setData] = useState<AgentExecutionCostPreviewPayload | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      try {
        const res = await fetch(previewUrl, { method: "GET", credentials: "include" });

        if (!res.ok) {
          if (!cancelled) {
            setError(`Preview unavailable (${res.status})`);
          }

          return;
        }

        const json = (await res.json()) as AgentExecutionCostPreviewPayload;

        if (!cancelled) {
          setData(json);
          setError(null);
        }
      } catch {
        if (!cancelled) {
          setError("Preview unavailable");
        }
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [previewUrl]);

  if (error !== null) {
    return (
      <div
        role="status"
        data-testid="run-cost-preview-error"
        className="rounded-md border border-amber-200 bg-amber-50/90 p-3 text-sm text-amber-950 dark:border-amber-900 dark:bg-amber-950/40 dark:text-amber-100"
      >
        {error}
      </div>
    );
  }

  if (data === null) {
    return (
      <p className="m-0 text-sm text-neutral-500 dark:text-neutral-400" data-testid="run-cost-preview-loading">
        Loading cost preview…
      </p>
    );
  }

  if (data.mode !== "Real") {
    return null;
  }

  const dollars =
    data.estimatedCostUsd === null || Number.isNaN(data.estimatedCostUsd)
      ? null
      : data.estimatedCostUsd.toLocaleString(undefined, {
          style: "currency",
          currency: "USD",
          minimumFractionDigits: 2,
          maximumFractionDigits: 2,
        });

  return (
    <div
      data-testid="run-cost-preview-card"
      className="rounded-md border border-amber-300/90 bg-amber-50/95 p-4 text-sm text-amber-950 shadow-sm dark:border-amber-800 dark:bg-amber-950/50 dark:text-amber-50"
    >
      <p className="m-0 font-medium" data-testid="run-cost-preview-headline">
        Estimated Azure OpenAI cost for this run:{" "}
        {dollars !== null ? (
          <span data-testid="run-cost-preview-amount">{dollars}</span>
        ) : (
          <span data-testid="run-cost-preview-amount">—</span>
        )}{" "}
        (based on <code className="rounded bg-amber-100/80 px-1 text-xs dark:bg-amber-900/60">MaxCompletionTokens</code>
        ={data.maxCompletionTokens} cap)
        {data.deploymentName ? (
          <>
            {" "}
            · deployment <code className="rounded bg-amber-100/80 px-1 text-xs dark:bg-amber-900/60">{data.deploymentName}</code>
          </>
        ) : null}
      </p>
      <p className="mt-2 mb-0 text-xs text-amber-900/90 dark:text-amber-100/90">
        Methodology:{" "}
        <Link className="font-medium text-amber-950 underline dark:text-amber-50" href={DOCS_URL}>
          docs/library/PER_TENANT_COST_MODEL.md
        </Link>
      </p>
    </div>
  );
}

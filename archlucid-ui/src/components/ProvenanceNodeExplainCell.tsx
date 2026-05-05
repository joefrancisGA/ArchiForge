"use client";

import { useState } from "react";

import { Button } from "@/components/ui/button";

type Props = {
  runId: string;
  nodeId: string;
};

/** Fetches the reserved explanation route (501 until backend implements LLM summaries). */
export function ProvenanceNodeExplainCell({ runId, nodeId }: Props) {
  const [statusLine, setStatusLine] = useState<string>("");

  async function explain(): Promise<void> {
    setStatusLine("Requesting explanation…");

    const url =
      `/api/proxy/v1/architecture/runs/${encodeURIComponent(runId)}/provenance/${encodeURIComponent(nodeId)}/explanation`;

    try {
      const res = await fetch(url, {
        method: "GET",
        credentials: "include",
        headers: { Accept: "application/problem+json, application/json" },
      });

      const bodyUnknown: unknown = await res.json();

      const body = bodyUnknown as Record<string, unknown>;

      const detailRaw = typeof body.detail === "string" ? body.detail : null;

      const titleRaw = typeof body.title === "string" ? body.title : "";

      if (res.status === 501) {
        setStatusLine(detailRaw ?? titleRaw ?? "Not implemented yet.");

        return;
      }

      setStatusLine(detailRaw ?? titleRaw ?? `HTTP ${String(res.status)}`);
    }
    catch {
      setStatusLine("Could not reach the explanation endpoint.");
    }
  }

  return (
    <div className="flex flex-col gap-1">
      <Button type="button" variant="outline" size="sm" className="h-8 whitespace-nowrap" onClick={() => void explain()}>
        Explain node
      </Button>
      {statusLine ? (
        <p className="m-0 max-w-[280px] text-[11px] text-neutral-600 dark:text-neutral-400" aria-live="polite">
          {statusLine}
        </p>
      ) : null}
    </div>
  );
}

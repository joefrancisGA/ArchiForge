"use client";

import { useState } from "react";

import { DocumentLayout } from "@/components/DocumentLayout";
import { LayerHeader } from "@/components/LayerHeader";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
import { useEnterpriseMutationCapability } from "@/hooks/use-enterprise-mutation-capability";
import { downloadBoardPackPdf, downloadValueReportDocx } from "@/lib/api";
import type { ApiProblemDetails } from "@/lib/api-problem";
import { isApiRequestError } from "@/lib/api-request-error";
import { buildAuthMeProxyRequestInit } from "@/lib/current-principal";
import { DEFAULT_DEV_TENANT_ID } from "@/lib/scope-defaults";

const ME_PATH = "/api/proxy/api/auth/me";

async function resolveTenantIdFromMe(): Promise<string | null> {
  const init = await buildAuthMeProxyRequestInit();
  const res = await fetch(ME_PATH, init);

  if (!res.ok) return null;

  const body: unknown = await res.json();

  if (typeof body !== "object" || body === null || !("claims" in body)) return null;

  const claims = (body as { claims?: ReadonlyArray<{ type: string; value: string }> }).claims;
  const row = claims?.find((c) => c.type === "tenant_id");

  return row?.value?.trim() ?? null;
}

export default function ValueReportPage() {
  const canMutate = useEnterpriseMutationCapability();
  const [fromUtc, setFromUtc] = useState(() => {
    const d = new Date();

    d.setUTCDate(d.getUTCDate() - 30);

    return d.toISOString().slice(0, 16);
  });
  const [toUtc, setToUtc] = useState(() => new Date().toISOString().slice(0, 16));
  const [busy, setBusy] = useState(false);
  const [boardBusy, setBoardBusy] = useState(false);
  const [error, setError] = useState<{
    message: string;
    problem: ApiProblemDetails | null;
    correlationId: string | null;
  } | null>(null);

  async function onGenerate(): Promise<void> {
    setBusy(true);
    setError(null);

    try {
      const tenantId = (await resolveTenantIdFromMe()) ?? DEFAULT_DEV_TENANT_ID;
      const fromIso = new Date(fromUtc).toISOString();
      const toIso = new Date(toUtc).toISOString();

      await downloadValueReportDocx(tenantId, fromIso, toIso);
    } catch (e: unknown) {
      if (isApiRequestError(e)) {
        setError({
          message: e.message,
          problem: e.problem,
          correlationId: e.correlationId,
        });
      } else {
        setError({
          message: e instanceof Error ? e.message : "Could not generate value report.",
          problem: null,
          correlationId: null,
        });
      }
    } finally {
      setBusy(false);
    }
  }

  async function onBoardPack(): Promise<void> {
    setBoardBusy(true);
    setError(null);

    try {
      const now = new Date();
      const month = now.getUTCMonth() + 1;
      const year = now.getUTCFullYear();
      const quarter = Math.floor((month - 1) / 3) + 1;

      await downloadBoardPackPdf(year, quarter);
    } catch (e: unknown) {
      if (isApiRequestError(e)) {
        setError({
          message: e.message,
          problem: e.problem,
          correlationId: e.correlationId,
        });
      } else {
        setError({
          message: e instanceof Error ? e.message : "Could not generate board pack.",
          problem: null,
          correlationId: null,
        });
      }
    } finally {
      setBoardBusy(false);
    }
  }

  return (
    <main className="mx-auto space-y-4 p-4 print:w-full">
      <LayerHeader pageKey="value-report" />
      <DocumentLayout>
        <h1 className="m-0 text-2xl font-semibold text-neutral-900 dark:text-neutral-100">Value report</h1>
        <p className="doc-meta m-0 text-sm text-neutral-600 dark:text-neutral-400">
          Generates a stakeholder-grade DOCX from finalized runs, governance and drift audit counts, and ROI_MODEL-aligned
          estimates for the selected UTC window. Requires{" "}
          <strong className="font-medium text-neutral-800 dark:text-neutral-200">Standard</strong> commercial tier on the
          API.
        </p>
        <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
          <label className="flex flex-1 flex-col gap-1 text-sm">
            <span>From (UTC)</span>
            <input
              className="rounded border border-neutral-300 bg-white px-2 py-1 text-sm dark:border-neutral-700 dark:bg-neutral-900"
              type="datetime-local"
              value={fromUtc}
              onChange={(e) => setFromUtc(e.target.value)}
            />
          </label>
          <label className="flex flex-1 flex-col gap-1 text-sm">
            <span>To (UTC)</span>
            <input
              className="rounded border border-neutral-300 bg-white px-2 py-1 text-sm dark:border-neutral-700 dark:bg-neutral-900"
              type="datetime-local"
              value={toUtc}
              onChange={(e) => setToUtc(e.target.value)}
            />
          </label>
          <Button type="button" disabled={!canMutate || busy} onClick={() => void onGenerate()}>
            {busy ? "Generating…" : "Download DOCX"}
          </Button>
          <Button
            type="button"
            variant="outline"
            disabled={!canMutate || boardBusy}
            onClick={() => void onBoardPack()}
            title="Uses the current UTC calendar quarter"
          >
            {boardBusy ? "Board pack…" : "Quarterly board pack (PDF)"}
          </Button>
        </div>
        {!canMutate ? (
          <p className="text-sm text-neutral-600 dark:text-neutral-400">
            Operator or Administrator role required — the API enforces elevated permissions for this report.
          </p>
        ) : null}
        {error ? (
          <OperatorApiProblem
            problem={error.problem}
            fallbackMessage={error.message}
            correlationId={error.correlationId}
          />
        ) : null}
      </DocumentLayout>
    </main>
  );
}

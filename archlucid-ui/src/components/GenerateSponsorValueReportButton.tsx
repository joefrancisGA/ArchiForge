"use client";

import { useState } from "react";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
import { useEnterpriseMutationCapability } from "@/hooks/use-enterprise-mutation-capability";
import { downloadValueReportDocx } from "@/lib/api";
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

/** One-click sponsor DOCX for the current scope (last 30 days UTC). */
export function GenerateSponsorValueReportButton() {
  const canMutate = useEnterpriseMutationCapability();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<{
    message: string;
    problem: ApiProblemDetails | null;
    correlationId: string | null;
  } | null>(null);

  async function onClick(): Promise<void> {
    setBusy(true);
    setError(null);

    try {
      const tenantId = (await resolveTenantIdFromMe()) ?? DEFAULT_DEV_TENANT_ID;
      const to = new Date();
      const from = new Date(to);

      from.setUTCDate(from.getUTCDate() - 30);

      const fromIso = from.toISOString();
      const toIso = to.toISOString();

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

  return (
    <div className="space-y-2">
      <Button
        type="button"
        variant="outline"
        disabled={!canMutate || busy}
        title={
          canMutate
            ? "Generate a sponsor-ready DOCX for the current scope."
            : "Sign in as an operator with artifact export access to generate the sponsor DOCX."
        }
        onClick={() => void onClick()}
      >
        {busy ? "Generating…" : "Generate sponsor report"}
      </Button>
      {!canMutate ? (
        <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
          Available to operators with artifact export access. Sign in to enable.
        </p>
      ) : null}
      {error ? (
        <OperatorApiProblem
          problem={error.problem}
          fallbackMessage={error.message}
          correlationId={error.correlationId}
          variant="warning"
        />
      ) : null}
    </div>
  );
}

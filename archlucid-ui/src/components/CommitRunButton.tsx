"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { ConfirmationDialog } from "@/components/ConfirmationDialog";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { commitArchitectureRun } from "@/lib/api";
import { isApiRequestError } from "@/lib/api-request-error";
import type { ApiProblemDetails } from "@/lib/api-problem";
import { recordFirstTenantFunnelEvent } from "@/lib/first-tenant-funnel-telemetry";

export type CommitRunButtonProps = {
  runId: string;
  /** When true, the run already has a reviewed manifest — commit is not offered. */
  disabled: boolean;
};

/**
 * Commits the architecture run via POST /v1/architecture/run/{runId}/commit (ExecuteAuthority).
 */
export function CommitRunButton({ runId, disabled }: CommitRunButtonProps) {
  const router = useRouter();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [notifySponsor, setNotifySponsor] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<{
    message: string;
    problem: ApiProblemDetails | null;
    correlationId: string | null;
  } | null>(null);

  async function onConfirm(): Promise<void> {
    setBusy(true);
    setError(null);

    try {
      await commitArchitectureRun(runId, { notifySponsor });
      recordFirstTenantFunnelEvent("first_run_committed");
      setDialogOpen(false);
      // Land on the findings board so the architect sees risks immediately after finalizing.
      router.push("/governance/findings");
    } catch (e: unknown) {
      if (isApiRequestError(e)) {
        setError({
          message: e.message,
          problem: e.problem,
          correlationId: e.correlationId,
        });
      } else {
        setError({
          message: e instanceof Error ? e.message : "Finalization failed.",
          problem: null,
          correlationId: null,
        });
      }
    } finally {
      setBusy(false);
    }
  }

  if (disabled) {
    return (
      <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
        This run is already finalized (reviewed manifest present).
      </p>
    );
  }

  return (
    <div className="space-y-3">
      <div>
        <Button
          type="button"
          variant="primary"
          onClick={() => {
            setError(null);
            setNotifySponsor(false);
            setDialogOpen(true);
          }}
        >
          Finalize manifest
        </Button>
        <p className="mt-1.5 max-w-xl text-sm text-neutral-600 dark:text-neutral-400">
          Finalizes the reviewed manifest and decision traces when the run is ready. Requires permission to finalize manifests.
        </p>
      </div>

      {error !== null ? (
        <OperatorApiProblem
          problem={error.problem}
          fallbackMessage={error.message}
          correlationId={error.correlationId}
        />
      ) : null}

      <ConfirmationDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        title="Finalize this manifest?"
        description="Merges agent results for this run through the decision engine and persists the reviewed manifest. If the run is not ready, the API returns a conflict — adjust the run and try again."
        confirmLabel="Finalize manifest"
        cancelLabel="Cancel"
        variant="default"
        onConfirm={() => void onConfirm()}
        busy={busy}
        extraContent={
          <div className="flex items-start gap-2 px-1 py-1">
            <input
              id="commit-notify-sponsor"
              type="checkbox"
              className="mt-1 h-4 w-4 rounded border border-neutral-300 text-neutral-900 focus-visible:ring-2 focus-visible:ring-neutral-400 disabled:opacity-50 dark:border-neutral-600 dark:text-neutral-100 dark:focus-visible:ring-neutral-500"
              checked={notifySponsor}
              disabled={busy}
              onChange={(e) => {
                setNotifySponsor(e.target.checked);
              }}
            />
            <div className="min-w-0">
              <Label htmlFor="commit-notify-sponsor" className="font-medium text-neutral-900 dark:text-neutral-100">
                Email tenant admin contact
              </Label>
              <p className="mt-0.5 text-xs text-neutral-600 dark:text-neutral-400">
                Sends a short heads-up with a link to this run when the tenant admin mailbox is on file and outbound
                email is configured.
              </p>
            </div>
          </div>
        }
      />
    </div>
  );
}

"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { ConfirmationDialog } from "@/components/ConfirmationDialog";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
import { commitArchitectureRun } from "@/lib/api";
import { isApiRequestError } from "@/lib/api-request-error";
import type { ApiProblemDetails } from "@/lib/api-problem";
import { recordFirstTenantFunnelEvent } from "@/lib/first-tenant-funnel-telemetry";

export type CommitRunButtonProps = {
  runId: string;
  /** When true, the run already has a golden manifest — commit is not offered. */
  disabled: boolean;
};

/**
 * Commits the architecture run via POST /v1/architecture/run/{runId}/commit (ExecuteAuthority).
 */
export function CommitRunButton({ runId, disabled }: CommitRunButtonProps) {
  const router = useRouter();
  const [dialogOpen, setDialogOpen] = useState(false);
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
      await commitArchitectureRun(runId);
      recordFirstTenantFunnelEvent("first_run_committed");
      setDialogOpen(false);
      router.refresh();
    } catch (e: unknown) {
      if (isApiRequestError(e)) {
        setError({
          message: e.message,
          problem: e.problem,
          correlationId: e.correlationId,
        });
      } else {
        setError({
          message: e instanceof Error ? e.message : "Commit failed.",
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
        This run is already committed (golden manifest present).
      </p>
    );
  }

  return (
    <div className="space-y-3">
      <div>
        <Button
          type="button"
          variant="default"
          className="bg-teal-700 text-white hover:bg-teal-800 dark:bg-teal-600 dark:hover:bg-teal-500"
          onClick={() => {
            setError(null);
            setDialogOpen(true);
          }}
        >
          Commit run
        </Button>
        <p className="mt-1.5 max-w-xl text-sm text-neutral-600 dark:text-neutral-400">
          Produces the golden manifest and decision traces when the run is ready. Requires permission to commit runs.
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
        title="Commit this run?"
        description="Merges agent results through the decision engine and persists the golden manifest. If the run is not ready, the API returns a conflict — adjust the run and try again."
        confirmLabel="Commit run"
        cancelLabel="Cancel"
        variant="default"
        onConfirm={() => void onConfirm()}
        busy={busy}
      />
    </div>
  );
}

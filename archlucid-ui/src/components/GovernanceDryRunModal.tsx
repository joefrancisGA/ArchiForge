"use client";

import { useCallback, useState } from "react";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { dryRunPolicyPack } from "@/lib/api";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import {
  POLICY_PACK_DRY_RUN_DEFAULT_PAGE_SIZE,
  POLICY_PACK_DRY_RUN_MAX_PAGE_SIZE,
  type PolicyPackDryRunResponse,
} from "@/types/policy-pack-dry-run";

export interface GovernanceDryRunModalProps {
  policyPackId: string;
}

/**
 * Governance dry-run / what-if modal: lets an operator simulate proposed threshold changes
 * against a list of historic run ids without committing anything. The default page size is
 * fixed at 20 (owner Q38) and the API will clamp anything larger than 100 server-side.
 */
export function GovernanceDryRunModal({ policyPackId }: GovernanceDryRunModalProps) {
  const [open, setOpen] = useState(false);
  const [thresholdsJson, setThresholdsJson] = useState<string>(
    '{"maxCriticalFindings":"0","maxHighFindings":"3"}',
  );
  const [runIdsRaw, setRunIdsRaw] = useState<string>("");
  const [pageSize, setPageSize] = useState<number>(POLICY_PACK_DRY_RUN_DEFAULT_PAGE_SIZE);
  const [busy, setBusy] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [result, setResult] = useState<PolicyPackDryRunResponse | null>(null);

  const reset = useCallback(() => {
    setBusy(false);
    setErrorMessage(null);
    setResult(null);
  }, []);

  const onOpenChange = useCallback(
    (next: boolean) => {
      setOpen(next);

      if (!next) {
        reset();
      }
    },
    [reset],
  );

  const onSubmit = useCallback(async () => {
    setBusy(true);
    setErrorMessage(null);
    setResult(null);

    let parsedThresholds: Record<string, string>;

    try {
      const parsed: unknown = JSON.parse(thresholdsJson);

      if (parsed === null || typeof parsed !== "object" || Array.isArray(parsed)) {
        throw new Error("Proposed thresholds must be a JSON object of key/value strings.");
      }

      parsedThresholds = Object.fromEntries(
        Object.entries(parsed as Record<string, unknown>).map(([k, v]) => [k, String(v)]),
      );
    } catch (e) {
      const message = e instanceof Error ? e.message : "Invalid JSON.";
      setErrorMessage(`Could not parse proposed thresholds JSON: ${message}`);
      setBusy(false);

      return;
    }

    const runIds = runIdsRaw
      .split(/[\s,]+/g)
      .map((s) => s.trim())
      .filter((s) => s.length > 0);

    if (runIds.length === 0) {
      setErrorMessage("Provide at least one run id to evaluate.");
      setBusy(false);

      return;
    }

    try {
      const response = await dryRunPolicyPack(
        policyPackId,
        { proposedThresholds: parsedThresholds, evaluateAgainstRunIds: runIds },
        { pageSize, page: 1 },
      );
      setResult(response);
    } catch (e) {
      const failure = toApiLoadFailure(e);
      setErrorMessage(failure.message);
    } finally {
      setBusy(false);
    }
  }, [pageSize, policyPackId, runIdsRaw, thresholdsJson]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogTrigger asChild>
        <Button type="button" variant="default" data-testid="open-dry-run-modal">
          Run dry-run / what-if
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>Governance dry-run</DialogTitle>
          <DialogDescription>
            Simulate proposed threshold changes for this policy pack against historic runs without
            persisting changes. Default page size {POLICY_PACK_DRY_RUN_DEFAULT_PAGE_SIZE}, server cap{" "}
            {POLICY_PACK_DRY_RUN_MAX_PAGE_SIZE}.
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4">
          <div className="grid gap-2">
            <Label htmlFor="dry-run-thresholds-json">Proposed thresholds (JSON object)</Label>
            <textarea
              id="dry-run-thresholds-json"
              data-testid="dry-run-thresholds-json"
              className="min-h-[120px] rounded-md border border-neutral-300 bg-white p-2 font-mono text-xs dark:border-neutral-700 dark:bg-neutral-900"
              value={thresholdsJson}
              onChange={(e) => setThresholdsJson(e.target.value)}
              spellCheck={false}
            />
            <p className="text-xs text-neutral-500 dark:text-neutral-400">
              Values are sent through the LLM-prompt redaction pipeline before being persisted in
              the audit log (PENDING_QUESTIONS Q37).
            </p>
          </div>

          <div className="grid gap-2">
            <Label htmlFor="dry-run-run-ids">Run ids (comma or whitespace separated)</Label>
            <Input
              id="dry-run-run-ids"
              data-testid="dry-run-run-ids"
              value={runIdsRaw}
              onChange={(e) => setRunIdsRaw(e.target.value)}
              autoComplete="off"
              placeholder="run-001, run-002"
            />
          </div>

          <div className="grid gap-2">
            <Label htmlFor="dry-run-page-size">
              Page size (default {POLICY_PACK_DRY_RUN_DEFAULT_PAGE_SIZE}, max{" "}
              {POLICY_PACK_DRY_RUN_MAX_PAGE_SIZE})
            </Label>
            <Input
              id="dry-run-page-size"
              data-testid="dry-run-page-size"
              type="number"
              min={1}
              max={POLICY_PACK_DRY_RUN_MAX_PAGE_SIZE}
              value={pageSize}
              onChange={(e) => {
                const next = Number(e.target.value);
                setPageSize(Number.isFinite(next) && next > 0 ? next : POLICY_PACK_DRY_RUN_DEFAULT_PAGE_SIZE);
              }}
            />
          </div>

          {errorMessage !== null ? (
            <div
              role="alert"
              data-testid="dry-run-error"
              className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900 dark:bg-red-950/80 dark:text-red-100"
            >
              {errorMessage}
            </div>
          ) : null}

          {result !== null ? (
            <section
              data-testid="dry-run-result"
              className="grid gap-2 rounded-md border border-neutral-200 p-3 text-sm dark:border-neutral-700"
            >
              <div className="font-semibold">Result</div>
              <div>
                Evaluated {result.deltaCounts.evaluated} run(s) — would block{" "}
                <strong>{result.deltaCounts.wouldBlock}</strong> · would allow{" "}
                <strong>{result.deltaCounts.wouldAllow}</strong> · missing{" "}
                <strong>{result.deltaCounts.runMissing}</strong>
              </div>
              <div>
                Page {result.page} · page size{" "}
                <span data-testid="dry-run-result-page-size">{result.pageSize}</span> · returned{" "}
                {result.returnedRuns} of {result.totalRequestedRuns}
              </div>
              <div className="grid gap-1">
                <div className="text-xs text-neutral-500 dark:text-neutral-400">
                  Proposed thresholds (after redaction):
                </div>
                <pre
                  data-testid="dry-run-redacted-json"
                  className="whitespace-pre-wrap break-words rounded-md bg-neutral-100 p-2 font-mono text-xs dark:bg-neutral-800"
                >
                  {result.proposedThresholdsRedactedJson}
                </pre>
              </div>
            </section>
          ) : null}
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            Close
          </Button>
          <Button
            type="button"
            data-testid="dry-run-submit"
            onClick={() => void onSubmit()}
            disabled={busy}
          >
            {busy ? "Evaluating…" : "Evaluate"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

import Link from "next/link";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import type { FindingInspectPayload } from "@/types/finding-inspect";

import { FindingInspectFindingBody } from "./FindingInspectFindingBody";

/** Compares authority run ids from URL vs API (hyphenated vs `N` GUID, case). */
export function sameAuthorityRunId(a: string, b: string): boolean
{
  const norm = (s: string) => s.replace(/-/g, "").toLowerCase();

  return norm(String(a)) === norm(String(b));
}

export type FindingInspectViewProps = {
  runId: string;
  decodedFindingId: string;
  payload: FindingInspectPayload | null;
  failure: ApiLoadFailureState | null;
};

/**
 * Sync inspector UI (payload / rule / evidence / audit). The RSC page loads data and passes props;
 * Vitest targets this module so mocks do not fight Next async server entrypoints.
 */
export function FindingInspectView({
  runId,
  decodedFindingId,
  payload,
  failure,
}: FindingInspectViewProps) {
  if (failure || !payload) {
    return (
      <main className="mx-auto max-w-3xl space-y-4 p-6">
        <Link href={`/runs/${encodeURIComponent(runId)}`} className="text-sm text-sky-700 underline dark:text-sky-300">
          ← Back to run
        </Link>
        <h1 className="text-xl font-semibold text-neutral-900 dark:text-neutral-100">Why this finding?</h1>
        <OperatorApiProblem
          problem={failure?.problem ?? null}
          fallbackMessage={failure?.message ?? "Finding inspector unavailable."}
          correlationId={failure?.correlationId ?? null}
        />
      </main>
    );
  }

  if (!sameAuthorityRunId(payload.runId, runId)) {
    return (
      <main className="mx-auto max-w-3xl space-y-4 p-6">
        <p className="text-sm text-neutral-700 dark:text-neutral-300">
          This finding belongs to run <span className="font-mono">{payload.runId}</span>, not the run in this URL.
        </p>
        <Link
          href={`/runs/${encodeURIComponent(payload.runId)}/findings/${encodeURIComponent(decodedFindingId)}/inspect`}
          className="text-sky-700 underline dark:text-sky-300"
        >
          Open the correct inspector
        </Link>
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-3xl space-y-6 p-6">
      <div className="flex flex-wrap items-center gap-3 text-sm text-neutral-600 dark:text-neutral-400">
        <Link href={`/runs/${encodeURIComponent(runId)}`} className="text-sky-700 underline dark:text-sky-300">
          ← Back to run
        </Link>
        <span aria-hidden="true">·</span>
        <Link
          href={`/runs/${encodeURIComponent(runId)}/findings/${encodeURIComponent(decodedFindingId)}`}
          className="text-sky-700 underline dark:text-sky-300"
        >
          Explain page (LLM audit)
        </Link>
      </div>

      <header>
        <h1 className="text-xl font-semibold text-neutral-900 dark:text-neutral-100">Why this finding?</h1>
        <p className="m-0 mt-1 text-sm text-neutral-600 dark:text-neutral-400">
          Finding <span className="font-mono text-xs">{decodedFindingId}</span> — manifest{" "}
          <span className="font-mono text-xs">{payload.manifestVersion ?? "—"}</span>
        </p>
      </header>

      <FindingInspectFindingBody
        runId={runId}
        decodedFindingId={decodedFindingId}
        payload={payload}
        variant="inspect"
      />
    </main>
  );
}

import Link from "next/link";

import { FindingInspectJsonPayload } from "@/components/FindingInspectJsonPayload";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import type { FindingInspectPayload } from "@/types/finding-inspect";

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

      <section className="rounded-lg border border-neutral-200 bg-neutral-50/80 p-4 dark:border-neutral-700 dark:bg-neutral-900/40">
        <h2 className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Payload</h2>
        <div className="mt-2">
          <FindingInspectJsonPayload value={payload.typedPayload ?? null} />
        </div>
      </section>

      <section className="rounded-lg border border-neutral-200 bg-neutral-50/80 p-4 dark:border-neutral-700 dark:bg-neutral-900/40">
        <h2 className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Decision rule</h2>
        <dl className="mt-2 space-y-1 text-sm text-neutral-800 dark:text-neutral-200">
          <div>
            <dt className="inline font-medium text-neutral-600 dark:text-neutral-400">Rule id</dt>
            <dd className="ml-2 inline font-mono text-xs">{payload.decisionRuleId ?? "—"}</dd>
          </div>
          <div>
            <dt className="inline font-medium text-neutral-600 dark:text-neutral-400">Rule name</dt>
            <dd className="ml-2 inline">{payload.decisionRuleName ?? "—"}</dd>
          </div>
        </dl>
      </section>

      <section className="rounded-lg border border-neutral-200 bg-neutral-50/80 p-4 dark:border-neutral-700 dark:bg-neutral-900/40">
        <h2 className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Evidence</h2>
        {payload.evidence.length === 0 ? (
          <p className="m-0 mt-2 text-sm text-neutral-600 dark:text-neutral-400">No related graph citations on file.</p>
        ) : (
          <ul className="mt-2 list-disc space-y-2 pl-5 text-sm text-neutral-800 dark:text-neutral-200">
            {payload.evidence.map((row, idx) => (
              <li key={`${row.excerpt ?? "ev"}-${idx}`}>
                <span className="font-mono text-xs">{row.excerpt ?? row.artifactId ?? "(empty)"}</span>
                {row.lineRange ? (
                  <span className="ml-2 text-neutral-600 dark:text-neutral-400">({row.lineRange})</span>
                ) : null}
                <div className="mt-1">
                  <Link
                    href={`/runs/${encodeURIComponent(runId)}`}
                    className="text-xs text-sky-700 underline dark:text-sky-300"
                  >
                    Open run (artifacts and graph context)
                  </Link>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="rounded-lg border border-neutral-200 bg-neutral-50/80 p-4 dark:border-neutral-700 dark:bg-neutral-900/40">
        <h2 className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Audit</h2>
        {payload.auditRowId ? (
          <p className="m-0 mt-2 text-sm text-neutral-800 dark:text-neutral-200">
            Durable audit event id: <span className="font-mono text-xs">{payload.auditRowId}</span>
            <span className="ml-2">
              <Link href="/audit" className="text-sky-700 underline dark:text-sky-300">
                Search in audit log
              </Link>
            </span>
          </p>
        ) : (
          <p className="m-0 mt-2 text-sm text-neutral-600 dark:text-neutral-400">
            No matching <code className="rounded bg-neutral-200 px-1 text-[0.7rem] dark:bg-neutral-800">
              AuthorityCommittedChainPersisted
            </code>{" "}
            row (SQL audit may be disabled in this environment).
          </p>
        )}
      </section>
    </main>
  );
}

import Link from "next/link";

import { FindingExplainPanel } from "@/components/FindingExplainPanel";

/** Deep-linkable “Explain this finding” view: redacted LLM audit + evidence-chain pointers (ReadAuthority-gated inside the panel). */
export default async function RunFindingExplainPage({
  params,
}: {
  params: Promise<{ runId: string; findingId: string }>;
}) {
  const { runId, findingId } = await params;

  return (
    <div className="mx-auto max-w-3xl space-y-4 p-6">
      <div className="flex flex-wrap items-center gap-3 text-sm text-neutral-600 dark:text-neutral-400">
        <Link
          href={`/runs/${runId}`}
          className="text-teal-800 underline decoration-neutral-300 underline-offset-2 hover:text-teal-900 dark:text-teal-300 dark:decoration-neutral-600 dark:hover:text-teal-200"
        >
          ← Back to architecture run
        </Link>
      </div>
      <h1 className="text-xl font-semibold text-neutral-900 dark:text-neutral-100">Finding {findingId}</h1>
      <p className="m-0 text-sm leading-relaxed text-neutral-600 dark:text-neutral-400">
        Redacted LLM prompts/completions and persisted evidence-chain pointers for operator forensics.
      </p>
      <FindingExplainPanel runId={runId} findingId={findingId} />
    </div>
  );
}

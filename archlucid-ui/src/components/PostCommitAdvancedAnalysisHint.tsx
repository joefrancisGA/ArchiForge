import Link from "next/link";

import { NAV_DISCLOSURE } from "@/lib/nav-disclosure-copy";

type PostCommitAdvancedAnalysisHintProps = {
  runId: string;
};

/**
 * Shown on run detail only after a golden manifest exists. Suggests Advanced
 * Analysis surfaces without pulling operators off Core Pilot before commit.
 */
export function PostCommitAdvancedAnalysisHint({ runId }: PostCommitAdvancedAnalysisHintProps) {
  const encoded = encodeURIComponent(runId);

  return (
    <aside
      className="mb-6 max-w-3xl rounded-md border border-neutral-200 bg-neutral-50 px-3 py-2.5 dark:border-neutral-700 dark:bg-neutral-900/50"
      aria-label="Advanced Analysis — optional next steps after commit"
    >
      <p className="m-0 text-[11px] font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
        Advanced Analysis — optional
      </p>
      <p className="m-0 mt-1 text-sm text-neutral-800 dark:text-neutral-200">
        This run has a committed manifest. None of this is required to judge first-pilot value—only when you have a
        concrete question Core Pilot does not answer (diff two runs, re-validate the provenance chain, or explore a
        graph). Use the links below; enable <em>{NAV_DISCLOSURE.extended.show}</em> in the sidebar if needed.
      </p>
      <ul className="m-0 mt-2 flex list-none flex-wrap gap-x-3 gap-y-1 p-0 text-sm">
        <li>
          <Link
            className="text-teal-800 underline dark:text-teal-300"
            href={`/compare?leftRunId=${encoded}&rightRunId=`}
          >
            Compare
          </Link>
        </li>
        <li>
          <Link className="text-teal-800 underline dark:text-teal-300" href={`/replay?runId=${encoded}`}>
            Replay
          </Link>
        </li>
        <li>
          <Link className="text-teal-800 underline dark:text-teal-300" href="/graph">
            Graph
          </Link>
        </li>
      </ul>
    </aside>
  );
}

import type { Metadata } from "next";
import Link from "next/link";

import { OperatorFirstRunWorkflowPanel } from "@/components/OperatorFirstRunWorkflowPanel";
import { ShortcutHint } from "@/components/ShortcutHint";

export const metadata: Metadata = {
  title: "Operator home",
};

/** Landing page: first-run workflow panel plus compact links into operator areas. */
export default function HomePage() {
  return (
    <main>
      <h2 className="mb-2 text-xl font-semibold text-neutral-900 dark:text-neutral-100">Operator home</h2>
      <p className="mb-3 max-w-3xl leading-relaxed text-neutral-700 dark:text-neutral-300">
        <strong>Start here:</strong> new to this environment? Use the checklist below. Returning operators can hide it
        and jump straight to{" "}
        <Link href="/runs?projectId=default" className="text-teal-800 underline dark:text-teal-300">
          Runs
        </Link>{" "}
        or{" "}
        <Link href="/runs/new" className="text-teal-800 underline dark:text-teal-300">
          New run
        </Link>
        .
      </p>
      <p className="mb-4 max-w-3xl rounded-lg border border-neutral-200 bg-slate-50 p-3 text-sm leading-relaxed text-neutral-700 dark:border-neutral-700 dark:bg-neutral-900/50 dark:text-neutral-300">
        <strong className="text-neutral-900 dark:text-neutral-100">Typical V1 path:</strong>{" "}
        <Link href="/runs/new" className="text-teal-800 underline dark:text-teal-300">
          New run
        </Link>{" "}
        (or pick an existing row on{" "}
        <Link href="/runs?projectId=default" className="text-teal-800 underline dark:text-teal-300">
          Runs
        </Link>
        ) → wait for the pipeline → commit the golden manifest → review <strong>Artifacts</strong> on run detail →{" "}
        <Link href="/compare" className="text-teal-800 underline dark:text-teal-300">
          Compare two runs
        </Link>
        ,{" "}
        <Link href="/replay" className="text-teal-800 underline dark:text-teal-300">
          Replay a run
        </Link>
        , or{" "}
        <Link href="/graph" className="text-teal-800 underline dark:text-teal-300">
          open the graph
        </Link>
        .
      </p>

      <OperatorFirstRunWorkflowPanel />

      <section className="mt-2" aria-labelledby="quick-links-heading">
        <h3 id="quick-links-heading" className="mb-2 text-base font-semibold text-neutral-900 dark:text-neutral-100">
          Quick links
        </h3>
        <p className="mb-3 max-w-3xl text-sm leading-relaxed text-neutral-500 dark:text-neutral-400">
          Same destinations as the sidebar <strong>Runs &amp; review</strong>, plus everything under{" "}
          <strong>Q&amp;A &amp; advisory</strong> and <strong>Alerts &amp; governance</strong>.
        </p>
        <ul className="m-0 max-w-3xl list-disc space-y-1 pl-5 leading-relaxed text-neutral-700 dark:text-neutral-300">
          <li>
            <Link href="/runs/new" className="text-teal-800 underline dark:text-teal-300">
              New run (wizard)
            </Link>{" "}
            <ShortcutHint shortcut="Alt+N" className="ml-1 align-middle text-[0.75rem]" /> — guided create; then find the
            run on{" "}
            <Link href="/runs?projectId=default" className="text-teal-800 underline dark:text-teal-300">
              Runs
            </Link>
          </li>
          <li>
            <Link href="/runs?projectId=default" className="text-teal-800 underline dark:text-teal-300">
              Runs
            </Link>{" "}
            — list, open detail, manifest, artifacts, exports, compare/replay shortcuts
          </li>
          <li>
            <Link href="/graph" className="text-teal-800 underline dark:text-teal-300">
              Graph
            </Link>{" "}
            — provenance or architecture graph for a run ID
          </li>
          <li>
            <Link href="/compare" className="text-teal-800 underline dark:text-teal-300">
              Compare two runs
            </Link>{" "}
            ·{" "}
            <Link href="/replay" className="text-teal-800 underline dark:text-teal-300">
              Replay a run
            </Link>
          </li>
          <li>
            Ask, search, advisory,{" "}
            <Link href="/planning" className="text-teal-800 underline dark:text-teal-300">
              planning
            </Link>
            , pilot feedback, alerts, and policy tools — use the sidebar groups.
          </li>
        </ul>
      </section>
    </main>
  );
}

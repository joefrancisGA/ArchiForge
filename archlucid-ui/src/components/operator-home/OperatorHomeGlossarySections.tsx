"use client";

import Link from "next/link";

import { GlossaryTooltip } from "@/components/GlossaryTooltip";
import { ShortcutHint } from "@/components/ShortcutHint";

/**
 * Body copy for operator home with inline glossary (first use per term; max 5 per page here).
 * Server `page.tsx` defers to this client island for GlossaryTooltip.
 */
export function OperatorHomeGlossarySections() {
  return (
    <>
      <p className="mb-4 max-w-3xl text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
        ArchLucid groups capabilities into <strong>three product layers</strong>. <strong>First-pilot success is Core Pilot
        only</strong>—new <GlossaryTooltip termKey="run">run</GlossaryTooltip>, pipeline, commit, review{" "}
        <GlossaryTooltip termKey="artifact_bundle">artifacts</GlossaryTooltip> and surfaced{" "}
        <GlossaryTooltip termKey="findings">findings</GlossaryTooltip> (follow the checklist below).{" "}
        <strong>Advanced Analysis</strong> and <strong>Enterprise Controls</strong> are <strong>optional maturity</strong>
        : skip them until those four steps are done or sponsors explicitly expand scope (
        <code className="text-[0.85em]">docs/OPERATOR_DECISION_GUIDE.md</code>).
      </p>

      <section className="mt-2" aria-labelledby="core-pilot-heading">
        <h3 id="core-pilot-heading" className="mb-1 text-base font-semibold text-neutral-900 dark:text-neutral-100">
          Core Pilot path
        </h3>
        <p className="mb-3 max-w-3xl text-sm leading-relaxed text-neutral-500 dark:text-neutral-400">
          These four links cover the complete first-pilot journey.
        </p>
        <ul className="m-0 max-w-3xl list-disc space-y-1 pl-5 leading-relaxed text-neutral-700 dark:text-neutral-300">
          <li>
            <Link href="/runs/new" className="text-teal-800 underline dark:text-teal-300">
              New run (wizard)
            </Link>{" "}
            <ShortcutHint shortcut="Alt+N" className="ml-1 align-middle text-[0.75rem]" /> — guided seven-step
            create; submits the run and tracks the pipeline in real time.
          </li>
          <li>
            <Link href="/runs?projectId=default" className="text-teal-800 underline dark:text-teal-300">
              Runs
            </Link>{" "}
            — list all runs; open detail, commit, inspect manifest, download artifacts and exports.
          </li>
          <li>
            <strong>Commit</strong> — on run detail, use <em>Commit run</em> once the pipeline is complete to produce
            the <GlossaryTooltip termKey="golden_manifest">golden manifest</GlossaryTooltip> and artifacts. CLI/API
            alternative: <code>docs/OPERATOR_QUICKSTART.md</code>.
          </li>
          <li>
            <strong>Artifacts</strong> — after commit, open run detail and use the Artifacts table to review,
            preview, and download each artifact. Bundle ZIP also available.
          </li>
        </ul>
      </section>
    </>
  );
}

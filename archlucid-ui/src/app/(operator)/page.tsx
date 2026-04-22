import type { Metadata } from "next";
import Link from "next/link";

import { AfterCorePilotChecklistHint } from "@/components/AfterCorePilotChecklistHint";
import { PilotOutcomeCard } from "@/components/PilotOutcomeCard";
import { OperatorTaskSuccessTile } from "@/components/OperatorTaskSuccessTile";
import { BeforeAfterDeltaPanel } from "@/components/BeforeAfterDeltaPanel";
import { OperatorFirstRunWorkflowPanel } from "@/components/OperatorFirstRunWorkflowPanel";
import { OperatorHomeGate } from "@/components/OperatorHomeGate";
import { ShortcutHint } from "@/components/ShortcutHint";
import { TrialWelcomeRunDeepLink } from "@/components/TrialWelcomeRunDeepLink";
import { WelcomeBanner } from "@/components/WelcomeBanner";

export const metadata: Metadata = {
  title: "Operator home",
};

/** Landing page: optional welcome banner, first-run workflow panel, and quick links. */
export default function HomePage() {
  return (
    <OperatorHomeGate>
    <TrialWelcomeRunDeepLink />
    <main>
      <h2 className="mb-2 text-xl font-semibold text-neutral-900 dark:text-neutral-100">Operator home</h2>
      <p className="mb-4 max-w-3xl text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
        ArchLucid groups capabilities into <strong>three product layers</strong>. <strong>First-pilot success is Core Pilot
        only</strong>—new run, pipeline, commit, review artifacts (follow the checklist below).{" "}
        <strong>Advanced Analysis</strong> and <strong>Enterprise Controls</strong> are <strong>optional maturity</strong>
        : skip them until those four steps are done or sponsors explicitly expand scope (
        <code className="text-[0.85em]">docs/OPERATOR_DECISION_GUIDE.md</code>).
      </p>

      <WelcomeBanner />

      <PilotOutcomeCard />

      <OperatorTaskSuccessTile />

      <BeforeAfterDeltaPanel />

      <OperatorFirstRunWorkflowPanel />

      <AfterCorePilotChecklistHint />

      {/* Core Pilot quick links — the four steps every pilot needs. */}
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
            the golden manifest and artifacts. CLI/API alternative: <code>docs/OPERATOR_QUICKSTART.md</code>.
          </li>
          <li>
            <strong>Artifacts</strong> — after commit, open run detail and use the Artifacts table to review,
            preview, and download each artifact. Bundle ZIP also available.
          </li>
        </ul>
      </section>

      {/* Advanced Analysis — optional maturity; not first-pilot requirements */}
      <section className="mt-6" aria-labelledby="advanced-analysis-heading">
        <h3 id="advanced-analysis-heading" className="mb-1 text-base font-semibold text-neutral-900 dark:text-neutral-100">
          Advanced Analysis
        </h3>
        <p className="mb-2 max-w-3xl text-xs font-medium text-neutral-600 dark:text-neutral-400">
          Not required for first-pilot proof. Use when you have a committed run <em>and</em> a concrete analytical question.
          Sidebar: <em>Show more links</em>.
        </p>
        <ul className="m-0 max-w-3xl list-disc space-y-1 pl-5 text-sm leading-relaxed text-neutral-600 dark:text-neutral-400">
          <li>
            <Link href="/compare" className="text-teal-800 underline dark:text-teal-300">
              Compare two runs
            </Link>
            {" · "}
            <Link href="/replay" className="text-teal-800 underline dark:text-teal-300">
              Replay a run
            </Link>{" "}
            — structured manifest diff and authority-chain re-validation.
          </li>
          <li>
            <Link href="/graph" className="text-teal-800 underline dark:text-teal-300">
              Graph
            </Link>{" "}
            — visual provenance or architecture graph for a run ID.
          </li>
          <li>
            <Link href="/ask" className="text-teal-800 underline dark:text-teal-300">
              Ask
            </Link>
            {" · "}
            <Link href="/advisory" className="text-teal-800 underline dark:text-teal-300">
              Advisory
            </Link>
            {" · "}
            <Link href="/product-learning" className="text-teal-800 underline dark:text-teal-300">
              Pilot feedback
            </Link>{" "}
            — natural-language Q&amp;A, advisory scans, and product signal collection.
          </li>
        </ul>
      </section>

      {/* Enterprise Controls — optional maturity; not first-pilot requirements */}
      <section className="mt-5" aria-labelledby="enterprise-controls-heading">
        <h3 id="enterprise-controls-heading" className="mb-1 text-base font-semibold text-neutral-900 dark:text-neutral-100">
          Enterprise Controls
        </h3>
        <p className="mb-2 max-w-3xl text-xs font-medium text-neutral-600 dark:text-neutral-400">
          Not required for Core Pilot. Operator/admin layer when governance, audit, or alerts matter—typically governance
          or platform operators. Use extended and advanced sidebar links; the list can reflect your role (API enforces
          writes).
        </p>
        <ul className="m-0 max-w-3xl list-disc space-y-1 pl-5 text-sm leading-relaxed text-neutral-600 dark:text-neutral-400">
          <li>
            <Link href="/governance/dashboard" className="text-teal-800 underline dark:text-teal-300">
              Governance dashboard
            </Link>
            {" · "}
            <Link href="/policy-packs" className="text-teal-800 underline dark:text-teal-300">
              Policy packs
            </Link>
            {" · "}
            <Link href="/governance-resolution" className="text-teal-800 underline dark:text-teal-300">
              Governance resolution
            </Link>{" "}
            — approval workflows, segregation of duties, and effective policy.
          </li>
          <li>
            <Link href="/alerts" className="text-teal-800 underline dark:text-teal-300">
              Alerts
            </Link>
            {" · "}
            <Link href="/audit" className="text-teal-800 underline dark:text-teal-300">
              Audit log
            </Link>{" "}
            — compliance alerts and append-only audit trail export.
          </li>
        </ul>
      </section>
    </main>
    </OperatorHomeGate>
  );
}

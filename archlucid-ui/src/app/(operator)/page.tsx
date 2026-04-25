import type { Metadata } from "next";
import Link from "next/link";

import { AfterCorePilotChecklistHint } from "@/components/AfterCorePilotChecklistHint";
import { PilotOutcomeCard } from "@/components/PilotOutcomeCard";
import { OperatorTaskSuccessTile } from "@/components/OperatorTaskSuccessTile";
import { BeforeAfterDeltaPanel } from "@/components/BeforeAfterDeltaPanel";
import { OperatorFirstRunWorkflowPanel } from "@/components/OperatorFirstRunWorkflowPanel";
import { OperatorHomeGate } from "@/components/OperatorHomeGate";
import { OptInTourLauncher } from "@/components/tour/OptInTourLauncher";
import { TrialWelcomeRunDeepLink } from "@/components/TrialWelcomeRunDeepLink";
import { NAV_DISCLOSURE } from "@/lib/nav-disclosure-copy";
import { OperatorHomeGlossarySections } from "@/components/operator-home/OperatorHomeGlossarySections";
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
      <div className="mb-2 flex items-center justify-between gap-3">
        <h2 className="text-xl font-semibold text-neutral-900 dark:text-neutral-100">Operator home</h2>
        <OptInTourLauncher />
      </div>
      <OperatorHomeGlossarySections />

      <WelcomeBanner />

      <PilotOutcomeCard />

      <OperatorTaskSuccessTile />

      <BeforeAfterDeltaPanel />

      <OperatorFirstRunWorkflowPanel />

      <AfterCorePilotChecklistHint />

      {/* Advanced Analysis — optional maturity; not first-pilot requirements */}
      <section className="mt-6" aria-labelledby="advanced-analysis-heading">
        <h3 id="advanced-analysis-heading" className="mb-1 text-base font-semibold text-neutral-900 dark:text-neutral-100">
          Advanced Analysis
        </h3>
        <p className="mb-2 max-w-3xl text-xs font-medium text-neutral-600 dark:text-neutral-400">
          Not required for first-pilot proof. Use when you have a committed run <em>and</em> a concrete analytical question.
          Sidebar: <em>{NAV_DISCLOSURE.extended.show}</em>.
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
          <li>
            <Link href="/admin/support" className="text-teal-800 underline dark:text-teal-300">
              Support
            </Link>{" "}
            — need to file a support ticket? Download a redacted support bundle here (gated on operator access).
          </li>
        </ul>
      </section>
    </main>
    </OperatorHomeGate>
  );
}

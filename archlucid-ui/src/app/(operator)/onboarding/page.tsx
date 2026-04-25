import type { Metadata } from "next";
import Link from "next/link";

import { NAV_DISCLOSURE } from "@/lib/nav-disclosure-copy";

import { OnboardingWizardClient } from "@/components/OnboardingWizardClient";

export const metadata: Metadata = {
  title: "Onboarding",
  description: "Guided first steps for operators using the ArchLucid UI shell.",
};

/**
 * Short guided path for new operators: links mirror the shell groups and point to deeper docs.
 */
export default function OnboardingPage() {
  return (
    <main>
      <p className="mb-4 max-w-3xl text-sm leading-relaxed">
        <strong>Finish Core Pilot on </strong>
        <Link href="/" title="Home with first-run workflow">
          Home
        </Link>{" "}
        or{" "}
        <Link href="/getting-started" title="Getting started — same checklist">
          Getting started
        </Link>{" "}
        before treating Advanced Analysis or Enterprise Controls as in-scope. The sidebar lists three layers with
        short captions; Enterprise entries can shorten by role. <strong>Only Core Pilot</strong> is the default wedge—see{" "}
        <code className="text-[0.85em]">docs/OPERATOR_DECISION_GUIDE.md</code> for when to expand.
      </p>
      <div className="mb-10">
        <OnboardingWizardClient />
      </div>
      <h1 style={{ marginBottom: 8 }}>Operator onboarding</h1>
      <p style={{ maxWidth: 720, color: "#334155", lineHeight: 1.55, marginBottom: 24 }}>
        Follow these steps once per environment. Each item opens the matching area in the app or documentation.
      </p>

      {/* Core Pilot steps — complete these first. */}
      <p style={{ maxWidth: 720, fontSize: 13, fontWeight: 600, color: "#0369a1", marginBottom: 6 }}>
        Core Pilot path — complete these first:
      </p>
      <ol style={{ maxWidth: 720, lineHeight: 1.8, color: "#1e293b", paddingLeft: 22, marginBottom: 24 }}>
        <li style={{ marginBottom: 14 }}>
          <strong>Walk the in-app checklist.</strong> On{" "}
          <Link href="/" title="Home with Core Pilot checklist">
            Home
          </Link>
          , the <strong>Core Pilot checklist</strong> has four steps: create run → pipeline → commit → review
          artifacts.
        </li>
        <li style={{ marginBottom: 14 }}>
          <strong>Confirm API reachability.</strong> Ensure the UI proxy can reach the API (see{" "}
          <code>docs/TROUBLESHOOTING.md</code> in the repo). Run a health check from your deployment pipeline if
          applicable.
        </li>
        <li style={{ marginBottom: 14 }}>
          <strong>Open your first run.</strong>{" "}
          <Link
            href="/runs/new"
            title="Guided first-run wizard — system identity through pipeline tracking"
          >
            New run
          </Link>{" "}
          or{" "}
          <Link href="/runs?projectId=default" title="Browse runs for the default project">
            Runs
          </Link>
          . The seven-step wizard covers the full lifecycle from description and requirements through submit and live
          pipeline tracking; after commit, review manifest and artifacts from run detail.
        </li>
      </ol>

      {/* Explore further — not required for the core pilot but available once you have a committed run. */}
      <p style={{ maxWidth: 720, fontSize: 13, fontWeight: 600, color: "#475569", marginBottom: 6 }}>
        Explore further — once you have a committed run:
      </p>
      <ol style={{ maxWidth: 720, lineHeight: 1.8, color: "#334155", paddingLeft: 22 }}>
        <li style={{ marginBottom: 14 }}>
          <strong>Trace provenance.</strong> From a known run ID, use{" "}
          <Link href="/graph" title="Load architecture or provenance graph for a run">
            Graph
          </Link>{" "}
          to validate the graph matches expectations. Enable via <em>{NAV_DISCLOSURE.extended.show}</em> in the
          sidebar.
        </li>
        <li style={{ marginBottom: 14 }}>
          <strong>Compare or replay.</strong> Use{" "}
          <Link href="/compare" title="Compare two architecture runs">
            Compare two runs
          </Link>{" "}
          and{" "}
          <Link href="/replay" title="Re-validate the provenance chain for a run">
            Replay a run
          </Link>{" "}
          for regression analysis. Enable via <em>{NAV_DISCLOSURE.extended.show}</em>.
        </li>
        <li style={{ marginBottom: 14 }}>
          <strong>Alerts and governance.</strong> When ready, configure{" "}
          <Link href="/alerts" title="Alert inbox">
            Alerts
          </Link>
          ,{" "}
          <Link href="/policy-packs" title="Policy packs">
            Policy packs
          </Link>
          , and{" "}
          <Link href="/governance-resolution" title="Governance resolution">
            Governance resolution
          </Link>
          .
        </li>
      </ol>

      <p style={{ marginTop: 28, fontSize: 14, color: "#64748b", maxWidth: 720 }}>
        Deeper reading: <code>docs/PILOT_GUIDE.md</code> and <code>docs/OPERATOR_QUICKSTART.md</code> in the
        repository checkout. Full reference: <code>docs/CORE_PILOT.md</code>.
      </p>
    </main>
  );
}

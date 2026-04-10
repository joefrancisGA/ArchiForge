import type { Metadata } from "next";
import Link from "next/link";

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
      <h1 style={{ marginBottom: 8 }}>Operator onboarding</h1>
      <p style={{ maxWidth: 720, color: "#334155", lineHeight: 1.55, marginBottom: 24 }}>
        Follow these steps once per environment. Each item opens the matching area in the app or documentation.
      </p>

      <ol style={{ maxWidth: 720, lineHeight: 1.8, color: "#1e293b", paddingLeft: 22 }}>
        <li style={{ marginBottom: 14 }}>
          <strong>Walk the in-app checklist.</strong> On{" "}
          <Link href="/" title="Home with first-run workflow">
            Home
          </Link>
          , expand <strong>First-run workflow</strong> (V1 checklist) for step-by-step links (create run → commit →
          artifacts → compare/replay → export).
        </li>
        <li style={{ marginBottom: 14 }}>
          <strong>Confirm API reachability.</strong> Ensure the UI proxy can reach the API (see{" "}
          <code>docs/TROUBLESHOOTING.md</code> in the repo). Run a health check from your deployment pipeline if
          applicable.
        </li>
        <li style={{ marginBottom: 14 }}>
          <strong>Open your first run.</strong>{" "}
          <Link href="/runs/new" title="Create a run with the wizard">
            New run
          </Link>{" "}
          or{" "}
          <Link href="/runs?projectId=default" title="Browse runs for the default project">
            Runs
          </Link>
          , then review manifest and artifacts from run detail after commit.
        </li>
        <li style={{ marginBottom: 14 }}>
          <strong>Trace provenance.</strong> From a known run ID, use{" "}
          <Link href="/graph" title="Load architecture or provenance graph for a run">
            Graph
          </Link>{" "}
          to validate the graph matches expectations.
        </li>
        <li style={{ marginBottom: 14 }}>
          <strong>Compare or replay.</strong> Use{" "}
          <Link href="/compare" title="Compare two architecture runs">
            Compare two runs
          </Link>{" "}
          and{" "}
          <Link href="/replay" title="Replay authority chain for a run">
            Replay a run
          </Link>{" "}
          for regression analysis.
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
        repository checkout.
      </p>
    </main>
  );
}

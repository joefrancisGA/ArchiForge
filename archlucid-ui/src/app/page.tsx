import type { Metadata } from "next";
import Link from "next/link";

import { OperatorFirstRunWorkflowPanel } from "@/components/OperatorFirstRunWorkflowPanel";

export const metadata: Metadata = {
  title: "Home",
};

/** Landing page: first-run workflow panel plus compact links into operator areas. */
export default function HomePage() {
  return (
    <main>
      <h2 style={{ marginBottom: 8 }}>Start here</h2>
      <p style={{ maxWidth: 720, color: "#334155", lineHeight: 1.55, marginBottom: 16 }}>
        New to this environment? Use the checklist below. Returning operators can hide it and jump straight to{" "}
        <Link href="/runs?projectId=default">Runs</Link> or <Link href="/runs/new">New run</Link>.
      </p>

      <OperatorFirstRunWorkflowPanel />

      <section style={{ marginTop: 8 }} aria-labelledby="quick-links-heading">
        <h3 id="quick-links-heading" style={{ fontSize: 16, marginBottom: 12 }}>
          Quick links
        </h3>
        <ul style={{ lineHeight: 1.75, maxWidth: 720, margin: 0, paddingLeft: 20, color: "#334155" }}>
          <li>
            <Link href="/runs?projectId=default">Runs</Link> — list, open detail, manifest, artifacts, exports,
            compare/replay shortcuts
          </li>
          <li>
            <Link href="/graph">Graph</Link> — provenance or architecture graph for a run ID
          </li>
          <li>
            <Link href="/compare">Compare runs</Link> · <Link href="/replay">Replay run</Link>
          </li>
          <li>
            Ask, search, advisory, <Link href="/planning">planning</Link>, pilot feedback, alerts, and policy tools
            — header groups above
          </li>
        </ul>
      </section>
    </main>
  );
}

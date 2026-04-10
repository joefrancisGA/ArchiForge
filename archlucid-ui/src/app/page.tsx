import type { Metadata } from "next";
import Link from "next/link";

import { OperatorFirstRunWorkflowPanel } from "@/components/OperatorFirstRunWorkflowPanel";

export const metadata: Metadata = {
  title: "Operator home",
};

/** Landing page: first-run workflow panel plus compact links into operator areas. */
export default function HomePage() {
  return (
    <main>
      <h2 style={{ marginBottom: 8 }}>Operator home</h2>
      <p style={{ maxWidth: 720, color: "#334155", lineHeight: 1.55, marginBottom: 12 }}>
        <strong>Start here:</strong> new to this environment? Use the checklist below. Returning operators can hide it
        and jump straight to{" "}
        <Link href="/runs?projectId=default">Runs</Link> or <Link href="/runs/new">New run</Link>.
      </p>
      <p
        style={{
          maxWidth: 720,
          color: "#475569",
          lineHeight: 1.55,
          marginBottom: 16,
          padding: "10px 14px",
          background: "#f8fafc",
          border: "1px solid #e2e8f0",
          borderRadius: 8,
          fontSize: 14,
        }}
      >
        <strong style={{ color: "#0f172a" }}>Typical V1 path:</strong>{" "}
        <Link href="/runs/new">New run</Link> (or pick an existing row on{" "}
        <Link href="/runs?projectId=default">Runs</Link>) → wait for the pipeline → commit the golden manifest → review{" "}
        <strong>Artifacts</strong> on run detail →{" "}
        <Link href="/compare">Compare two runs</Link>, <Link href="/replay">Replay a run</Link>, or{" "}
        <Link href="/graph">open the graph</Link>.
      </p>

      <OperatorFirstRunWorkflowPanel />

      <section style={{ marginTop: 8 }} aria-labelledby="quick-links-heading">
        <h3 id="quick-links-heading" style={{ fontSize: 16, marginBottom: 8 }}>
          Quick links
        </h3>
        <p style={{ maxWidth: 720, margin: "0 0 12px", fontSize: 13, color: "#64748b", lineHeight: 1.5 }}>
          Same destinations as the header row <strong>Start here · runs &amp; review</strong>, plus everything under{" "}
          <strong>Q&amp;A &amp; advisory</strong> and <strong>Alerts &amp; governance</strong>.
        </p>
        <ul style={{ lineHeight: 1.75, maxWidth: 720, margin: 0, paddingLeft: 20, color: "#334155" }}>
          <li>
            <Link href="/runs/new">New run (wizard)</Link> — guided create; then find the run on{" "}
            <Link href="/runs?projectId=default">Runs</Link>
          </li>
          <li>
            <Link href="/runs?projectId=default">Runs</Link> — list, open detail, manifest, artifacts, exports,
            compare/replay shortcuts
          </li>
          <li>
            <Link href="/graph">Graph</Link> — provenance or architecture graph for a run ID
          </li>
          <li>
            <Link href="/compare">Compare two runs</Link> · <Link href="/replay">Replay a run</Link>
          </li>
          <li>
            Ask, search, advisory, <Link href="/planning">planning</Link>, pilot feedback, alerts, and policy tools — use
            the header groups above.
          </li>
        </ul>
      </section>
    </main>
  );
}

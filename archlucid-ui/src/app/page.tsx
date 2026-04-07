import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Home",
};

/** Landing page: short orientation and links into the main operator workflows. */
export default function HomePage() {
  return (
    <main>
      <h2 style={{ marginBottom: 8 }}>Start here</h2>
      <p style={{ maxWidth: 720, color: "#334155", lineHeight: 1.55 }}>
        Use <Link href="/runs?projectId=default">Runs</Link> to open a run, then review the manifest and
        artifacts, download exports, or jump to <strong>Compare</strong> and <strong>Replay</strong> from run
        detail. Use <Link href="/graph">Graph</Link> when you already have a run ID and need a visual view
        of provenance or architecture.
      </p>

      <section style={{ marginTop: 28 }} aria-labelledby="workflows-heading">
        <h3 id="workflows-heading" style={{ fontSize: 16, marginBottom: 12 }}>
          Main workflows
        </h3>
        <ul style={{ lineHeight: 1.75, maxWidth: 720, margin: 0, paddingLeft: 20, color: "#334155" }}>
          <li>
            <Link href="/runs?projectId=default">Runs</Link> — list runs, open detail, artifacts, compare /
            replay shortcuts, downloads
          </li>
          <li>
            <Link href="/graph">Graph</Link> — load provenance or architecture graph for a run
          </li>
          <li>
            <Link href="/compare">Compare runs</Link> — two-run structured and legacy diff
          </li>
          <li>
            <Link href="/replay">Replay run</Link> — replay authority chain and validation
          </li>
        </ul>
      </section>

      <p style={{ marginTop: 24, fontSize: 14, color: "#64748b", maxWidth: 720 }}>
        Ask, search, advisory, <Link href="/planning">planning</Link> (59R themes and plans), pilot feedback, alerts, and
        policy tools live in the header groups above.
      </p>
    </main>
  );
}

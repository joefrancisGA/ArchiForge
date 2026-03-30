import Link from "next/link";

import { OperatorEmptyState } from "@/components/OperatorShellMessage";

/** Operator shell home page: static quick links to runs, graph, compare, and replay. */
export default function HomePage() {
  return (
    <main>
      <h2>Operator Shell</h2>
      <p>
        List runs, open run detail (manifest summary + artifacts), compare runs, replay authority
        chain, and download bundles or run exports.
      </p>

      <OperatorEmptyState title="No live data on this page">
        <p style={{ margin: 0 }}>
          This landing view is static. Loading, empty, error, and malformed-response states appear after
          you open Runs, run detail, manifest, artifact review, graph, compare, or replay.
        </p>
        <p style={{ margin: "12px 0 0", fontSize: 14, color: "#525252" }}>
          Artifact review: open a run with a golden manifest → use <strong>Review</strong> on an artifact,
          or go through the manifest page.
        </p>
      </OperatorEmptyState>

      <div style={{ marginTop: 24 }}>
        <p>Quick links:</p>
        <ul>
          <li>
            <Link href="/runs?projectId=default">Runs</Link>
          </li>
          <li>
            <Link href="/graph">Graph viewer</Link>
          </li>
          <li>
            <Link href="/compare">Compare runs</Link>
          </li>
          <li>
            <Link href="/replay">Replay run</Link>
          </li>
        </ul>
      </div>
    </main>
  );
}

import Link from "next/link";
import type { CSSProperties } from "react";

const groupLabel: CSSProperties = {
  fontSize: 12,
  color: "#64748b",
  marginBottom: 6,
  fontWeight: 600,
};

const navRow: CSSProperties = {
  display: "flex",
  gap: 14,
  flexWrap: "wrap",
  alignItems: "center",
};

const sep: CSSProperties = {
  width: "100%",
  border: 0,
  borderTop: "1px solid #e2e8f0",
  margin: "12px 0 10px",
};

/**
 * Primary navigation: grouped links so main operator workflows stay visible above alerts and governance.
 */
export function ShellNav() {
  return (
    <>
      <div style={{ marginBottom: 4 }}>
        <div style={groupLabel}>Start &amp; review</div>
        <nav aria-label="Start and review workflows" style={navRow}>
          <Link href="/" title="Home — first-run workflow checklist and quick links">
            Home
          </Link>
          <Link href="/onboarding" title="Guided operator onboarding checklist">
            Onboarding
          </Link>
          <Link href="/runs?projectId=default" title="Runs, manifests, artifacts, exports">
            Runs
          </Link>
          <Link href="/graph" title="Provenance and architecture graph for a run">
            Graph
          </Link>
          <Link href="/compare" title="Compare two runs">
            Compare runs
          </Link>
          <Link href="/replay" title="Replay authority chain for a run">
            Replay run
          </Link>
        </nav>
      </div>

      <hr style={sep} />

      <div style={{ marginBottom: 4 }}>
        <div style={groupLabel}>Q&amp;A &amp; advisory</div>
        <nav aria-label="Question answering and advisory" style={navRow}>
          <Link href="/ask" title="Natural language ask against architecture context">
            Ask
          </Link>
          <Link href="/search" title="Search indexed architecture content">
            Search
          </Link>
          <Link href="/advisory" title="Advisory scans and architecture digests">
            Advisory
          </Link>
          <Link href="/recommendation-learning" title="Recommendation learning profiles">
            Learning
          </Link>
          <Link href="/product-learning" title="Pilot feedback rollups and triage (58R)">
            Pilot feedback
          </Link>
          <Link href="/planning" title="Improvement themes and prioritized plans (59R)">
            Planning
          </Link>
          <Link href="/evolution-review" title="60R candidate simulations and before/after review">
            Simulation review
          </Link>
          <Link href="/advisory-scheduling">Schedules</Link>
          <Link href="/digests">Digests</Link>
          <Link href="/digest-subscriptions">Subscriptions</Link>
        </nav>
      </div>

      <hr style={sep} />

      <div>
        <div style={groupLabel}>Alerts &amp; governance</div>
        <nav aria-label="Alerts and governance" style={navRow}>
          <Link href="/alerts" title="Open and acknowledged alerts">
            Alerts
          </Link>
          <Link href="/alert-rules" title="Configure alert rules">
            Alert rules
          </Link>
          <Link href="/alert-routing" title="Alert routing subscriptions">
            Alert routing
          </Link>
          <Link href="/composite-alert-rules" title="Composite alert rules">
            Composite rules
          </Link>
          <Link href="/alert-simulation" title="Simulate alert evaluation">
            Alert simulation
          </Link>
          <Link href="/alert-tuning" title="Alert noise and threshold tuning">
            Alert tuning
          </Link>
          <Link href="/policy-packs" title="Policy packs and versions">
            Policy packs
          </Link>
          <Link href="/governance-resolution" title="Effective governance resolution">
            Governance resolution
          </Link>
          <Link href="/audit" title="Search and filter audit events">
            Audit log
          </Link>
        </nav>
      </div>
    </>
  );
}

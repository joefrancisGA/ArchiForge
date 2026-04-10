import Link from "next/link";
import type { CSSProperties } from "react";

const groupLabel: CSSProperties = {
  fontSize: 12,
  color: "#475569",
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
        <div style={groupLabel}>Start here · runs &amp; review</div>
        <nav aria-label="Primary operator workflows" style={navRow}>
          <Link className="shell-nav-link" href="/" title="Home — V1 checklist and quick links">
            Home
          </Link>
          <Link className="shell-nav-link" href="/onboarding" title="Guided operator onboarding checklist">
            Onboarding
          </Link>
          <Link className="shell-nav-link" href="/runs/new" title="Create a run with the guided wizard (typical first step)">
            New run
          </Link>
          <Link
            className="shell-nav-link"
            href="/runs?projectId=default"
            title="Runs list — open detail, manifest, artifacts, exports"
          >
            Runs
          </Link>
          <Link className="shell-nav-link" href="/graph" title="Provenance or architecture graph for one run ID">
            Graph
          </Link>
          <Link className="shell-nav-link" href="/compare" title="Diff two runs (base vs target)">
            Compare two runs
          </Link>
          <Link className="shell-nav-link" href="/replay" title="Re-validate authority chain for one run">
            Replay a run
          </Link>
        </nav>
      </div>

      <hr style={sep} />

      <div style={{ marginBottom: 4 }}>
        <div style={groupLabel}>Q&amp;A &amp; advisory</div>
        <nav aria-label="Question answering and advisory" style={navRow}>
          <Link className="shell-nav-link" href="/ask" title="Natural language ask against architecture context">
            Ask
          </Link>
          <Link className="shell-nav-link" href="/search" title="Search indexed architecture content">
            Search
          </Link>
          <Link className="shell-nav-link" href="/advisory" title="Advisory scans and architecture digests">
            Advisory
          </Link>
          <Link
            className="shell-nav-link"
            href="/recommendation-learning"
            title="Recommendation learning profiles"
          >
            Learning
          </Link>
          <Link className="shell-nav-link" href="/product-learning" title="Pilot feedback rollups and triage (58R)">
            Pilot feedback
          </Link>
          <Link className="shell-nav-link" href="/planning" title="Improvement themes and prioritized plans (59R)">
            Planning
          </Link>
          <Link
            className="shell-nav-link"
            href="/evolution-review"
            title="60R candidate simulations and before/after review"
          >
            Simulation review
          </Link>
          <Link className="shell-nav-link" href="/advisory-scheduling" title="Advisory scan schedules">
            Schedules
          </Link>
          <Link className="shell-nav-link" href="/digests" title="Architecture digests">
            Digests
          </Link>
          <Link className="shell-nav-link" href="/digest-subscriptions" title="Digest email subscriptions">
            Subscriptions
          </Link>
        </nav>
      </div>

      <hr style={sep} />

      <div>
        <div style={groupLabel}>Alerts &amp; governance</div>
        <nav aria-label="Alerts and governance" style={navRow}>
          <Link className="shell-nav-link" href="/alerts" title="Open and acknowledged alerts">
            Alerts
          </Link>
          <Link className="shell-nav-link" href="/alert-rules" title="Configure alert rules">
            Alert rules
          </Link>
          <Link className="shell-nav-link" href="/alert-routing" title="Alert routing subscriptions">
            Alert routing
          </Link>
          <Link className="shell-nav-link" href="/composite-alert-rules" title="Composite alert rules">
            Composite rules
          </Link>
          <Link className="shell-nav-link" href="/alert-simulation" title="Simulate alert evaluation">
            Alert simulation
          </Link>
          <Link className="shell-nav-link" href="/alert-tuning" title="Alert noise and threshold tuning">
            Alert tuning
          </Link>
          <Link className="shell-nav-link" href="/policy-packs" title="Policy packs and versions">
            Policy packs
          </Link>
          <Link className="shell-nav-link" href="/governance-resolution" title="Effective governance resolution">
            Governance resolution
          </Link>
          <Link className="shell-nav-link" href="/audit" title="Search and filter audit events">
            Audit log
          </Link>
        </nav>
      </div>
    </>
  );
}

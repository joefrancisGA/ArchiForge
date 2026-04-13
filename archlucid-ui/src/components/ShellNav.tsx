import Link from "next/link";
import type { CSSProperties } from "react";

import { registryKeyToAriaKeyShortcuts } from "@/lib/shortcut-registry";

function navTitleWithShortcut(baseTitle: string, registryCombo: string): string {
  const aria = registryKeyToAriaKeyShortcuts(registryCombo);

  return `${baseTitle} (${aria})`;
}

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
          <Link
            className="shell-nav-link"
            href="/"
            title={navTitleWithShortcut("Home — V1 checklist and quick links", "alt+h")}
            aria-keyshortcuts={registryKeyToAriaKeyShortcuts("alt+h")}
          >
            Home
          </Link>
          <Link className="shell-nav-link" href="/getting-started" title="Short path into runs, governance, compare">
            Getting started
          </Link>
          <Link className="shell-nav-link" href="/onboarding" title="Guided operator onboarding checklist">
            Onboarding
          </Link>
          <Link
            className="shell-nav-link"
            href="/runs/new"
            title={navTitleWithShortcut(
              "Guided first-run wizard — system identity through pipeline tracking",
              "alt+n",
            )}
            aria-keyshortcuts={registryKeyToAriaKeyShortcuts("alt+n")}
          >
            New run
          </Link>
          <Link
            className="shell-nav-link"
            href="/runs?projectId=default"
            title={navTitleWithShortcut("Runs list — open detail, manifest, artifacts, exports", "alt+r")}
            aria-keyshortcuts={registryKeyToAriaKeyShortcuts("alt+r")}
          >
            Runs
          </Link>
          <Link
            className="shell-nav-link"
            href="/graph"
            title={navTitleWithShortcut("Provenance or architecture graph for one run ID", "alt+y")}
            aria-keyshortcuts={registryKeyToAriaKeyShortcuts("alt+y")}
          >
            Graph
          </Link>
          <Link
            className="shell-nav-link"
            href="/compare"
            title={navTitleWithShortcut("Diff two runs (base vs target)", "alt+c")}
            aria-keyshortcuts={registryKeyToAriaKeyShortcuts("alt+c")}
          >
            Compare two runs
          </Link>
          <Link
            className="shell-nav-link"
            href="/replay"
            title={navTitleWithShortcut("Re-validate authority chain for one run", "alt+p")}
            aria-keyshortcuts={registryKeyToAriaKeyShortcuts("alt+p")}
          >
            Replay a run
          </Link>
        </nav>
      </div>

      <hr style={sep} />

      <div style={{ marginBottom: 4 }}>
        <div style={groupLabel}>Q&amp;A &amp; advisory</div>
        <nav aria-label="Question answering and advisory" style={navRow}>
          <Link
            className="shell-nav-link"
            href="/ask"
            title={navTitleWithShortcut("Natural language ask against architecture context", "alt+a")}
            aria-keyshortcuts={registryKeyToAriaKeyShortcuts("alt+a")}
          >
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
          <Link
            className="shell-nav-link"
            href="/alerts"
            title={navTitleWithShortcut("Open and acknowledged alerts", "alt+l")}
            aria-keyshortcuts={registryKeyToAriaKeyShortcuts("alt+l")}
          >
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
          <Link
            className="shell-nav-link"
            href="/governance/dashboard"
            title={navTitleWithShortcut("Cross-run governance dashboard — pending approvals and policy changes", "alt+g")}
            aria-keyshortcuts={registryKeyToAriaKeyShortcuts("alt+g")}
          >
            Dashboard
          </Link>
          <Link className="shell-nav-link" href="/governance" title="Approval, promotion, and activation workflow">
            Governance workflow
          </Link>
          <Link className="shell-nav-link" href="/audit" title="Search and filter audit events">
            Audit log
          </Link>
        </nav>
      </div>

      <span
        className="mt-2 block text-xs text-neutral-600"
        aria-keyshortcuts="Shift+?"
      >
        Press Shift+? for keyboard shortcuts
      </span>
    </>
  );
}

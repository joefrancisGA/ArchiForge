"use client";

import Link from "next/link";
import type { CSSProperties, ReactNode } from "react";
import { useEffect, useState } from "react";

const storageKey = "archlucid_operator_workflow_guide_v1";

const panelWrap: CSSProperties = {
  border: "1px solid #bae6fd",
  background: "#f0f9ff",
  borderRadius: 10,
  padding: "18px 20px",
  marginBottom: 28,
  maxWidth: 820,
};

const primaryLink: CSSProperties = {
  display: "inline-block",
  marginTop: 10,
  padding: "10px 18px",
  background: "#0f766e",
  color: "#fff",
  borderRadius: 8,
  fontWeight: 600,
  textDecoration: "none",
  fontSize: 14,
};

const secondaryLine: CSSProperties = {
  marginTop: 8,
  fontSize: 13,
  color: "#475569",
  lineHeight: 1.5,
};

const stepTitle: CSSProperties = { display: "block", marginBottom: 6 };

type WorkflowStep = {
  title: string;
  body: string;
  primaryHref: string;
  primaryLabel: string;
  secondary?: ReactNode;
};

const steps: WorkflowStep[] = [
  {
    title: "Create an architecture request",
    body: "Use the wizard to post a new run to the API. You will get a run ID to track.",
    primaryHref: "/runs/new",
    primaryLabel: "Start new run wizard",
    secondary: (
      <>
        Or browse existing runs on the{" "}
        <Link href="/runs?projectId=default" style={{ color: "#0f766e" }}>
          Runs list
        </Link>
        .
      </>
    ),
  },
  {
    title: "Let the pipeline run, then open the run",
    body: "After creation, the coordinator fills snapshots and authority steps. Watch progress on the wizard’s last step or open run detail anytime.",
    primaryHref: "/runs?projectId=default",
    primaryLabel: "Go to runs list",
    secondary: (
      <>
        Tip: from the wizard (step 4), use <strong>Open run detail</strong> for the new ID.
      </>
    ),
  },
  {
    title: "Commit the golden manifest",
    body: "Until commit, there is no manifest link or artifact exports. Commit through the API or CLI when the run is ready.",
    primaryHref: "/runs?projectId=default",
    primaryLabel: "Pick your run → open detail",
    secondary: (
      <>
        See <code>docs/OPERATOR_QUICKSTART.md</code> in the repo for commit examples.
      </>
    ),
  },
  {
    title: "Inspect manifest & artifacts",
    body: "After commit, run detail shows manifest summary, the artifact table, and links into each artifact.",
    primaryHref: "/runs?projectId=default",
    primaryLabel: "Open a committed run",
    secondary: (
      <>
        Full manifest page: open the <strong>Golden manifest</strong> link from run detail.
      </>
    ),
  },
  {
    title: "Compare or replay",
    body: "Diff two runs structurally, or replay authority validation for one run. Run detail has shortcuts prefilled for this run.",
    primaryHref: "/compare",
    primaryLabel: "Compare runs",
    secondary: (
      <>
        <Link href="/replay" style={{ color: "#0f766e" }}>
          Replay run
        </Link>{" "}
        ·{" "}
        <Link href="/graph" style={{ color: "#0f766e" }}>
          Graph (visual)
        </Link>
      </>
    ),
  },
  {
    title: "Export a package",
    body: "On run detail (with a manifest), use Download bundle (ZIP) and Download run export (ZIP) under Artifacts.",
    primaryHref: "/runs?projectId=default",
    primaryLabel: "Runs → open run → Artifacts",
  },
];

/**
 * Collapsible first-run checklist on Home. Persists “minimized” in localStorage so returning operators can hide it.
 */
export function OperatorFirstRunWorkflowPanel() {
  const [hydrated, setHydrated] = useState(false);
  const [minimized, setMinimized] = useState(false);

  useEffect(() => {
    try {
      if (typeof window !== "undefined" && window.localStorage.getItem(storageKey) === "1") {
        setMinimized(true);
      }
    } catch {
      /* private mode / SSR guard */
    }

    setHydrated(true);
  }, []);

  function minimize() {
    setMinimized(true);

    try {
      window.localStorage.setItem(storageKey, "1");
    } catch {
      /* ignore */
    }
  }

  function expand() {
    setMinimized(false);

    try {
      window.localStorage.removeItem(storageKey);
    } catch {
      /* ignore */
    }
  }

  if (!hydrated) {
    return <div style={{ minHeight: 140, marginBottom: 28 }} aria-hidden />;
  }

  if (minimized) {
    return (
      <div style={{ marginBottom: 20 }}>
        <button
          type="button"
          onClick={expand}
          style={{
            fontSize: 14,
            padding: "8px 14px",
            borderRadius: 8,
            border: "1px solid #cbd5e1",
            background: "#fff",
            cursor: "pointer",
            color: "#0f172a",
          }}
        >
          Show first-run workflow
        </button>
      </div>
    );
  }

  return (
    <section style={panelWrap} aria-labelledby="first-run-workflow-heading">
      <div
        style={{
          display: "flex",
          flexWrap: "wrap",
          alignItems: "flex-start",
          justifyContent: "space-between",
          gap: 12,
          marginBottom: 14,
        }}
      >
        <h2 id="first-run-workflow-heading" style={{ margin: 0, fontSize: 18, color: "#0c4a6e" }}>
          First-run workflow
        </h2>
        <button
          type="button"
          onClick={minimize}
          style={{
            fontSize: 13,
            padding: "6px 12px",
            borderRadius: 6,
            border: "1px solid #7dd3fc",
            background: "#fff",
            cursor: "pointer",
            color: "#0369a1",
          }}
        >
          Hide guide
        </button>
      </div>
      <p style={{ margin: "0 0 16px", fontSize: 14, color: "#334155", lineHeight: 1.55, maxWidth: 760 }}>
        Follow these steps once to go from an empty tenant to a reviewed, exportable architecture run. Your next
        action is highlighted on each step.
      </p>
      <ol style={{ margin: 0, paddingLeft: 22, color: "#1e293b", lineHeight: 1.5 }}>
        {steps.map((step, index) => (
          <li key={step.title} style={{ marginBottom: 22 }}>
            <strong style={stepTitle}>
              {index + 1}. {step.title}
            </strong>
            <span style={{ fontSize: 14, color: "#475569" }}>{step.body}</span>
            <div>
              <Link href={step.primaryHref} style={primaryLink}>
                {step.primaryLabel}
              </Link>
            </div>
            {step.secondary ? <div style={secondaryLine}>{step.secondary}</div> : null}
          </li>
        ))}
      </ol>
      <p style={{ margin: "18px 0 0", fontSize: 13, color: "#64748b" }}>
        More orientation: <Link href="/onboarding">Onboarding</Link> ·{" "}
        <Link href="/">Home overview</Link>
      </p>
    </section>
  );
}

/**
 * In-product copy for the three **product packaging** layers (**docs/PRODUCT_PACKAGING.md**,
 * **docs/OPERATOR_DECISION_GUIDE.md**). Consumed by **`LayerHeader`** (`LayerGuidancePageKey` per route family).
 *
 * **UI shaping only:** explains layer / when-to-use; does not grant access. **`[Authorize(Policy = …)]`** on **ArchLucid.Api** is
 * **authoritative** (**401/403**). This file does not implement **nav** (**`nav-config.ts`** + **`nav-shell-visibility.ts`**) or
 * **Execute+ mutation soft-disable** (**`enterprise-mutation-capability.ts`** / **`useEnterpriseMutationCapability()`**).
 *
 * **Enterprise vs Advanced rows:** blocks with **`layerBadge === "Enterprise Controls"`** must define **`enterpriseFootnote`**
 * (plus **`useWhen`** and **`firstPilotNote`**) — **`LayerHeader`** uses **`enterpriseFootnote`** to pick Enterprise typography and
 * the rank cue strip. **Advanced Analysis** rows must **not** set **`enterpriseFootnote`** (same component renders both badges).
 * **`authority-seam-regression.test.ts`** locks that contract so packaging and **`LayerHeader`** logic cannot drift silently.
 *
 * **`enterpriseFootnote`** on Enterprise keys complements **`nav-config.ts`** group **captions** — same buyer story, different surface.
 *
 * **Drift guard:** adding a key requires wiring **`LayerHeader`** on the page and, if the capability is listed for
 * buyers, updating **PRODUCT_PACKAGING.md** — see §3 *Contributor drift guard* (*Guidance strip* step).
 */

export type LayerGuidancePageKey =
  | "compare"
  | "replay"
  | "graph"
  | "governance-dashboard"
  | "governance-resolution"
  | "governance-workflow"
  | "policy-packs"
  | "alert-rules"
  | "alert-routing"
  | "alert-simulation"
  | "alert-tuning"
  | "composite-alert-rules"
  | "alerts"
  | "audit";

export type LayerGuidanceBlock = {
  /** Short badge, e.g. "Advanced Analysis" */
  layerBadge: string;
  /** One line: what question this surface answers */
  headline: string;
  /** When to use it (one sentence) */
  useWhen: string;
  /** Optional reminder for first pilots */
  firstPilotNote: string | null;
  /**
   * Optional one line for Enterprise Controls pages: who usually owns the surface vs Core Pilot default.
   * See docs/OPERATOR_DECISION_GUIDE.md §2.
   */
  enterpriseFootnote?: string | null;
};

export const LAYER_PAGE_GUIDANCE: Record<LayerGuidancePageKey, LayerGuidanceBlock> = {
  compare: {
    layerBadge: "Advanced Analysis",
    headline: "Answers: what changed between two committed runs?",
    useWhen: "Use after you have two runs with golden manifests when you need a structured diff or narrative.",
    firstPilotNote: "Not needed for your first pilot unless you are explicitly comparing two outcomes.",
  },
  replay: {
    layerBadge: "Advanced Analysis",
    headline: "Answers: does the stored authority chain still validate for this run?",
    useWhen: "Use when you need drift or integrity checks on a single run, not a visual diff.",
    firstPilotNote: "Optional until you need to prove or debug chain validation.",
  },
  graph: {
    layerBadge: "Advanced Analysis",
    headline: "Answers: how does provenance or architecture look for one run?",
    useWhen: "Use when tables and compare are not enough and you need a visual exploration.",
    firstPilotNote: "Skip until you have a committed run and a concrete graph question.",
  },
  "governance-dashboard": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: which cross-run approvals and governance signals need attention?",
    useWhen: "~30s queue snapshot; a row opens workflow for that run.",
    firstPilotNote: "Defer until cross-run triage is routine.",
    enterpriseFootnote: "Cross-run evidence; operator/admin writes.",
  },
  alerts: {
    layerBadge: "Enterprise Controls",
    headline: "Answers: what risk or compliance signals fired and need triage?",
    useWhen: "Inbox triage; thresholds and delivery live under Alert tooling.",
    firstPilotNote: "Defer rules work until the inbox is live.",
    enterpriseFootnote: "Inbox first; rules and routing are follow-on.",
  },
  audit: {
    layerBadge: "Enterprise Controls",
    headline: "Answers: tenant audit trail—who did what, when?",
    useWhen: "Search here first; CSV reuses From/To (Auditor/Admin API).",
    firstPilotNote: "Defer CSV until window and export roles are clear.",
    enterpriseFootnote: "Evidence search and bounded export.",
  },
  "governance-resolution": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: which policy content is in effect for this scope after pack ordering?",
    useWhen: "Read the ordered stack before changing packs or workflow elsewhere.",
    firstPilotNote: "Defer until ordering is a real question.",
    enterpriseFootnote: "Effective stack here; edits in policy packs or workflow.",
  },
  "governance-workflow": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: run-scoped submit, approve/reject, promote, and activate?",
    useWhen: "One run at a time; Core Pilot stays request → commit → artifacts.",
    firstPilotNote: "Defer until SoD or promotions apply.",
    enterpriseFootnote: "Run-scoped approvals; API role gates.",
  },
  "policy-packs": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: what packs exist, what is published, and what applies in this scope?",
    useWhen: "Inventory and effective JSON before lifecycle.",
    firstPilotNote: "Defer until you own pack lifecycle.",
    enterpriseFootnote: "Read/compare first; lifecycle writes are API configuration.",
  },
  "alert-rules": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: which metric thresholds should raise alerts after advisory scans?",
    useWhen: "Thresholds on scan outcomes; triage stays on Alerts.",
    firstPilotNote: "Defer until limits are operational.",
    enterpriseFootnote: "Metric thresholds on scan outcomes.",
  },
  "alert-routing": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: where should fired alerts be delivered when severity thresholds are met?",
    useWhen: "Delivery targets for fired alerts; not digest mail.",
    firstPilotNote: "Defer until live delivery matters.",
    enterpriseFootnote: "Delivery targets for fired alerts.",
  },
  "alert-simulation": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: how would rules behave against recent runs before changing production thresholds?",
    useWhen: "Dry-run on history; live triage on Alerts.",
    firstPilotNote: "Defer until you have a concrete what-if.",
    enterpriseFootnote: "Dry-run before changing production thresholds.",
  },
  "alert-tuning": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: which threshold values balance coverage and noise for a chosen rule?",
    useWhen: "Tune from simulation when noise or coverage is measurable.",
    firstPilotNote: "Defer until simulation shows a tradeoff.",
    enterpriseFootnote: "Threshold tuning from simulation evidence.",
  },
  "composite-alert-rules": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: how do we combine multiple scan metrics with AND/OR before firing an alert?",
    useWhen: "AND/OR plus cooldown when one threshold is not enough.",
    firstPilotNote: "Defer until composite firing is in scope.",
    enterpriseFootnote: "Composite AND/OR and cooldown configuration.",
  },
};

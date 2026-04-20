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
  | "audit"
  | "value-report";

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
    headline: "Cross-run approvals and governance signals.",
    useWhen: "Queue snapshot; open a row to continue in workflow for that run.",
    firstPilotNote: "Defer until cross-run triage is routine.",
    enterpriseFootnote: "Signals here; writes in workflow.",
  },
  alerts: {
    layerBadge: "Enterprise Controls",
    headline: "Risk and compliance signals that need triage.",
    useWhen: "Work the inbox; thresholds and routing live under Alert tooling.",
    firstPilotNote: "Defer rule tuning until volume justifies it.",
    enterpriseFootnote: "Inbox first; tooling for config.",
  },
  audit: {
    layerBadge: "Enterprise Controls",
    headline: "Tenant audit trail—who did what, when.",
    useWhen: "Search first; CSV reuses the same From/To window (Auditor/Admin on API).",
    firstPilotNote: "Defer export until the window and roles are settled.",
    enterpriseFootnote: "Search + bounded CSV export.",
  },
  "value-report": {
    layerBadge: "Enterprise Controls",
    headline: "Sponsor-facing value DOCX for a UTC window.",
    useWhen: "After you have committed runs; pairs with ROI_MODEL for CFO-ready narrative.",
    firstPilotNote: "Defer until the tenant is on Standard tier and operators need sponsor collateral.",
    enterpriseFootnote: "Execute + Standard tier on API; LLM line is estimated per ROI_MODEL when SQL token ledger absent.",
  },
  "governance-resolution": {
    layerBadge: "Enterprise Controls",
    headline: "Effective policy stack for this scope.",
    useWhen: "Read ordering here; change content on Policy packs or Workflow.",
    firstPilotNote: "Defer until merge order or conflicts matter.",
    enterpriseFootnote: "Read-only stack; edits on Packs or Workflow.",
  },
  "governance-workflow": {
    layerBadge: "Enterprise Controls",
    headline: "Run-scoped submit, review, promote, activate.",
    useWhen: "One run ID; follow status top to bottom.",
    firstPilotNote: "Defer until SoD or promotions apply.",
    enterpriseFootnote: "Run workflow; API-gated writes.",
  },
  "policy-packs": {
    layerBadge: "Enterprise Controls",
    headline: "Packs in scope, published versions, and effective JSON.",
    useWhen: "Inspect inventory and diff; lifecycle actions last.",
    firstPilotNote: "Defer lifecycle until you own publishing.",
    enterpriseFootnote: "Inspect/compare first; lifecycle on API.",
  },
  "alert-rules": {
    layerBadge: "Enterprise Controls",
    headline: "Metric thresholds that raise alerts after scans.",
    useWhen: "Define thresholds here; triage fired alerts on Alerts.",
    firstPilotNote: "Skip until limits are operational.",
    enterpriseFootnote: "Thresholds on scan outcomes.",
  },
  "alert-routing": {
    layerBadge: "Enterprise Controls",
    headline: "Where fired alerts are delivered.",
    useWhen: "Targets for fired alerts—not digest mail.",
    firstPilotNote: "Skip until live delivery matters.",
    enterpriseFootnote: "Delivery targets for fired alerts.",
  },
  "alert-simulation": {
    layerBadge: "Enterprise Controls",
    headline: "Dry-run rules against recent runs.",
    useWhen: "What-if before changing production thresholds; triage on Alerts.",
    firstPilotNote: "Skip until you have a concrete scenario.",
    enterpriseFootnote: "Simulation before production change.",
  },
  "alert-tuning": {
    layerBadge: "Enterprise Controls",
    headline: "Balance coverage vs. noise for one rule.",
    useWhen: "After simulation shows a tradeoff worth fixing.",
    firstPilotNote: "Skip until simulation justifies a change.",
    enterpriseFootnote: "Tuning from simulation evidence.",
  },
  "composite-alert-rules": {
    layerBadge: "Enterprise Controls",
    headline: "Combine metrics with AND/OR before firing.",
    useWhen: "Use when one metric is not enough; add cooldown as needed.",
    firstPilotNote: "Skip until composite firing is in scope.",
    enterpriseFootnote: "AND/OR and cooldown configuration.",
  },
};

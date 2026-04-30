/**
 * In-product copy for **Pilot** (Layer A) and **Operate** (Layer B) — **`docs/library/PRODUCT_PACKAGING.md`**
 * ("Layer A — Pilot", "Layer B — Operate", **Operate · analysis** vs **Operate · governance**).
 * **`docs/OPERATOR_DECISION_GUIDE.md`**. Consumed by **`LayerHeader`** (`LayerGuidancePageKey` per route family).
 *
 * **UI shaping only:** explains layer / when-to-use; does not grant access. **`[Authorize(Policy = …)]`** on **ArchLucid.Api** is
 * **authoritative** (**401/403**). This file does not implement **nav** (**`nav-config.ts`** + **`nav-shell-visibility.ts`**) or
 * **Execute+ mutation soft-disable** (**`operate-capability.ts`** / **`useOperateCapability()`**).
 *
 * **Operate slices:** blocks with **`layerBadge === "Operate"`** and a non-null **`enterpriseFootnote`** are the **governance /
 * trust** slice — **`LayerHeader`** uses that footnote for typography and the **Execute+** rank cue strip. **Operate** rows
 * without **`enterpriseFootnote`** are the **analysis** slice (compare / replay / graph). **`authority-seam-regression.test.ts`**
 * locks that contract.
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
  | "governance-findings"
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
  | "value-report"
  | "security-trust"
  | "teams-notifications";

export type LayerGuidanceBlock = {
  /** Short badge — buyer layers use **Operate** (analysis vs governance slice distinguished by `enterpriseFootnote`). */
  layerBadge: string;
  /** One line: what question this surface answers */
  headline: string;
  /** When to use it (one sentence) */
  useWhen: string;
  /** Optional framing relative to Pilot (first proof)—see PRODUCT_PACKAGING "Not required for first Pilot proof" sections. */
  firstPilotNote: string | null;
  /**
   * Optional one line for **Operate · governance** pages: who usually owns the surface vs Pilot default.
   * See docs/OPERATOR_DECISION_GUIDE.md §2.
   */
  enterpriseFootnote?: string | null;
};

export const LAYER_PAGE_GUIDANCE: Record<LayerGuidancePageKey, LayerGuidanceBlock> = {
  compare: {
    layerBadge: "Operate",
    headline: "Answers: what changed between two finalized runs?",
    useWhen: "Use after you have two runs with reviewed manifests when you need a structured diff or narrative.",
    firstPilotNote:
      "Operate · analysis — optional until first Pilot proof unless you deliberately compare two committed outcomes.",
  },
  replay: {
    layerBadge: "Operate",
    headline: "Answers: does stored pipeline output still validate for this run on replay?",
    useWhen: "Use when you need drift or integrity checks on a single run, not a visual diff.",
    firstPilotNote: "Operate · analysis — typically after Pilot proof when you replay or validate chains.",
  },
  graph: {
    layerBadge: "Operate",
    headline: "Answers: how does provenance or architecture look for one run?",
    useWhen: "Use when tables and compare are not enough and you need a visual exploration.",
    firstPilotNote: "Operate · analysis — defer until after Pilot proof when a graph answers the question.",
  },
  "governance-dashboard": {
    layerBadge: "Operate",
    headline: "Cross-run approvals and governance signals.",
    useWhen: "Queue snapshot; open a row to continue in workflow for that run.",
    firstPilotNote: "Operate · governance — after Pilot proof when cross-run triage—not first-session work.",
    enterpriseFootnote: "Signals here; writes in workflow.",
  },
  "governance-findings": {
    layerBadge: "Operate",
    headline: "Findings from architecture runs and governance scans.",
    useWhen: "Open a run for snapshot and explainability; use governance dashboard for cross-run queue context.",
    firstPilotNote:
      "Operate · governance — after Pilot proof use run detail for drill-down; dashboard queues cross-run findings.",
    enterpriseFootnote: "Run-scoped detail; cross-run queue on governance dashboard.",
  },
  alerts: {
    layerBadge: "Operate",
    headline: "Risk and compliance signals that need triage.",
    useWhen: "Work the inbox; rules, routing, composite, and simulation & tuning are tabs on the same Alerts page.",
    firstPilotNote: "Operate · governance — inbox first; rule tooling after Pilot proof when volume warrants it.",
    enterpriseFootnote: "Inbox first; tooling for config.",
  },
  audit: {
    layerBadge: "Operate",
    headline: "Tenant audit trail—who did what, when.",
    useWhen: "Search first; CSV reuses the same From/To window (Auditor/Admin on API).",
    firstPilotNote: "Operate · governance — bounded export after Pilot proof when audit window and roles are clear.",
    enterpriseFootnote: "Search + bounded CSV export.",
  },
  "security-trust": {
    layerBadge: "Operate",
    headline: "Procurement-facing security posture and NDA-gated pen-test summaries.",
    useWhen: "Use when buyers need CAIQ/SIG pointers, Trust Center links, and the NDA path for redacted pen-test excerpts.",
    firstPilotNote:
      "Operate · governance — procurement/CCI, not Pilot scope. Redacted pen-test excerpts NDA-only; contact security@.",
    enterpriseFootnote: "Read-oriented; Admin API may still emit SecurityAssessmentPublished for audit/SIEM without implying public publication.",
  },
  "teams-notifications": {
    layerBadge: "Operate",
    headline: "Microsoft Teams channel wiring for integration-event fan-out.",
    useWhen: "After Service Bus topics are live and operators want run / governance / alert cards in Teams.",
    firstPilotNote:
      "Operate · governance — after Pilot proof when Teams routing matters; store only a Key Vault secret id here.",
    enterpriseFootnote: "Read vs Execute matches API; Logic Apps resolves the secret at delivery time.",
  },
  "value-report": {
    layerBadge: "Operate",
    headline: "Sponsor-facing value DOCX for a UTC window.",
    useWhen: "After you have finalized runs; pairs with ROI_MODEL for CFO-ready narrative.",
    firstPilotNote: "Operate · governance — after Pilot proof with Standard tier when sponsor DOCX is needed.",
    enterpriseFootnote: "Execute + Standard tier on API; LLM line is estimated per ROI_MODEL when SQL token ledger absent.",
  },
  "governance-resolution": {
    layerBadge: "Operate",
    headline: "Effective policy stack for this scope.",
    useWhen: "Read ordering here; change content on Policy packs or Workflow.",
    firstPilotNote: "Operate · governance — after Pilot proof when merge order or conflicts need resolution.",
    enterpriseFootnote: "Read-only stack; edits on Packs or Workflow.",
  },
  "governance-workflow": {
    layerBadge: "Operate",
    headline: "Submit finalized architecture outputs for governance review and promotion.",
    useWhen: "Pick one run and move from submission through approval, promotion, and activation.",
    firstPilotNote:
      "Operate · governance — after Pilot proof when your team promotes finalized manifests through governed stages.",
    enterpriseFootnote: "Approvals and promotions follow the governance stages configured for your tenant.",
  },
  "policy-packs": {
    layerBadge: "Operate",
    headline: "Packs in scope, published versions, and effective JSON.",
    useWhen: "Inspect inventory and diff; lifecycle actions last.",
    firstPilotNote: "Operate · governance — after Pilot proof when you steward pack publish and assignment.",
    enterpriseFootnote: "Inspect/compare first; lifecycle on API.",
  },
  "alert-rules": {
    layerBadge: "Operate",
    headline: "Metric thresholds that raise alerts after scans.",
    useWhen: "Define thresholds here; triage fired alerts on Alerts.",
    firstPilotNote: "Operate · governance — threshold tuning after Pilot proof when scans drive production signals.",
    enterpriseFootnote: "Thresholds on scan outcomes.",
  },
  "alert-routing": {
    layerBadge: "Operate",
    headline: "Where fired alerts are delivered.",
    useWhen: "Targets for fired alerts—not digest mail.",
    firstPilotNote: "Operate · governance — destinations after Pilot proof when fired alerts need routing.",
    enterpriseFootnote: "Delivery targets for fired alerts.",
  },
  "alert-simulation": {
    layerBadge: "Operate",
    headline: "Dry-run rules against recent runs.",
    useWhen: "What-if before changing production thresholds; triage on Alerts.",
    firstPilotNote: "Operate · governance — what-if after Pilot proof before changing live thresholds.",
    enterpriseFootnote: "Simulation before production change.",
  },
  "alert-tuning": {
    layerBadge: "Operate",
    headline: "Balance coverage vs. noise for one rule.",
    useWhen: "After simulation shows a tradeoff worth fixing.",
    firstPilotNote: "Operate · governance — after Pilot proof when simulation evidence backs a live change.",
    enterpriseFootnote: "Tuning from simulation evidence.",
  },
  "composite-alert-rules": {
    layerBadge: "Operate",
    headline: "Combine metrics with AND/OR before firing.",
    useWhen: "Use when one metric is not enough; add cooldown as needed.",
    firstPilotNote: "Operate · governance — composite rules after Pilot proof when AND/OR firing is in scope.",
    enterpriseFootnote: "AND/OR and cooldown configuration.",
  },
};

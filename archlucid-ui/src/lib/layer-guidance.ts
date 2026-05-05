/**
 * In-product copy for **Pilot** (Layer A) and **Analysis / Governance** surfaces (Layer B) — **`docs/library/PRODUCT_PACKAGING.md`**
 * ("Layer A — Pilot", "Layer B — Operate" in docs; **Analysis** = deep-dive, **Governance** = approvals, audit, alerts, policy).
 * **`docs/OPERATOR_DECISION_GUIDE.md`**. Consumed by **`LayerHeader`** (`LayerGuidancePageKey` per route family).
 *
 * **UI shaping only:** explains layer / when-to-use; does not grant access. **`[Authorize(Policy = …)]`** on **ArchLucid.Api** is
 * **authoritative** (**401/403**). This file does not implement **nav** (**`nav-config.ts`** + **`nav-shell-visibility.ts`**) or
 * **Execute+ mutation soft-disable** (**`operate-capability.ts`** / **`useOperateCapability()`**).
 *
 * **Governance strip:** blocks with a non-null **`enterpriseFootnote`** are the **governance / trust** slice — **`LayerHeader`**
 * uses that footnote for typography and the **Execute+** rank cue strip. **Analysis** rows omit **`enterpriseFootnote`**.
 * **`authority-seam-regression.test.ts`** locks that contract.
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
  | "value-report-pilot"
  | "value-report-roi"
  | "security-trust"
  | "teams-notifications";

export type LayerGuidanceBlock = {
  /** Short badge — **Analysis** (deep-dive) vs **Governance** (approvals, audit, alerts, policy); governance rows set `enterpriseFootnote`. */
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
    layerBadge: "Analysis",
    headline: "Answers: what changed between two finalized reviews?",
    useWhen: "Use after you have two reviews with reviewed manifests when you need a structured diff or narrative.",
    firstPilotNote:
      "Optional until first Pilot proof unless you deliberately compare two committed outcomes.",
  },
  replay: {
    layerBadge: "Analysis",
    headline: "Answers: does stored pipeline output still validate for this review on replay?",
    useWhen: "Use when you need drift or integrity checks on a single review, not a visual diff.",
    firstPilotNote: "Typically after Pilot proof when you replay or validate chains.",
  },
  graph: {
    layerBadge: "Analysis",
    headline: "Answers: how does provenance or architecture look for this review?",
    useWhen: "Use when tables and compare are not enough and you need a visual exploration.",
    firstPilotNote:
      "Best once you have a committed review—a graph complements manifest and finding tables when stakeholders need visuals.",
  },
  "governance-dashboard": {
    layerBadge: "Governance",
    headline: "Workspace health — governance signals in your current scope.",
    useWhen:
      "Use after Pilot proof when sponsors need pre-commit outcomes, severity exposure, compliance drift, SLA posture, and a hours-first value proxy.",
    firstPilotNote: "Optional until first Pilot proof; data is scoped to the active tenant/workspace/project.",
    enterpriseFootnote: "Read-only tiles; writes stay in workflow, findings queue, and audit.",
  },
  "governance-findings": {
    layerBadge: "Governance",
    headline: "Findings from architecture reviews and governance scans.",
    useWhen: "Open a review for snapshot and explainability; use governance dashboard for cross-review queue context.",
    firstPilotNote:
      "After Pilot proof, use review detail for drill-down; dashboard queues cross-review findings.",
    enterpriseFootnote: "Review-scoped detail; cross-review queue on governance dashboard.",
  },
  alerts: {
    layerBadge: "Governance",
    headline: "Risk and compliance signals that need triage.",
    useWhen: "Work the inbox; rules, routing, composite, and simulation & tuning are tabs on the same Alerts page.",
    firstPilotNote: "Inbox first; rule tooling after Pilot proof when volume warrants it.",
    enterpriseFootnote: "Inbox first; configuration tabs when your role allows.",
  },
  audit: {
    layerBadge: "Governance",
    headline: "Tenant audit trail—who did what, when.",
    useWhen: "Search and filter audit events; export requires Auditor or Admin access.",
    firstPilotNote: "Bounded export after Pilot proof when audit window and roles are clear.",
    enterpriseFootnote: "Search first; CSV export for auditors and admins.",
  },
  "security-trust": {
    layerBadge: "Governance",
    headline: "Procurement-facing security posture and NDA-gated pen-test summaries.",
    useWhen: "Use when buyers need CAIQ/SIG pointers, Trust Center links, and the NDA path for redacted pen-test excerpts.",
    firstPilotNote:
      "Procurement/CCI, not Pilot scope. Redacted pen-test excerpts NDA-only; contact security@.",
    enterpriseFootnote: "Read-oriented; Admin API may still emit SecurityAssessmentPublished for audit/SIEM without implying public publication.",
  },
  "teams-notifications": {
    layerBadge: "Governance",
    headline: "Microsoft Teams channel wiring for integration-event fan-out.",
    useWhen: "After Service Bus topics are live and operators want run / governance / alert cards in Teams.",
    firstPilotNote:
      "After Pilot proof when Teams routing matters; store only a Key Vault secret id here.",
    enterpriseFootnote: "Read vs Execute matches API; Logic Apps resolves the secret at delivery time.",
  },
  "value-report-pilot": {
    layerBadge: "Analysis",
    headline: "Sponsor-ready proof snapshot without generating a DOCX.",
    useWhen:
      "When executives need totals, severities, governance signals, and a Markdown handoff aligned to a UTC measurement window.",
    firstPilotNote:
      "Complements the in-product scorecard; Read-tier API; optional during Pilot for executive visibility.",
  },
  "value-report-roi": {
    layerBadge: "Analysis",
    headline: "Sponsor-facing hours estimate from severities and pre-commit blocks.",
    useWhen:
      "When champions need a defensible hours story before negotiating loaded $/hour internally; pairs with Workspace health.",
    firstPilotNote: "Read-tier data pulls; Admin-only optional USD line uses local browser override.",
  },
  "value-report": {
    layerBadge: "Governance",
    headline: "Sponsor-facing value DOCX for a UTC window.",
    useWhen: "After you have finalized reviews; pairs with ROI_MODEL for CFO-ready narrative.",
    firstPilotNote: "After Pilot proof with Standard tier when sponsor DOCX is needed.",
    enterpriseFootnote: "Execute + Standard tier on API; LLM line is estimated per ROI_MODEL when SQL token ledger absent.",
  },
  "governance-resolution": {
    layerBadge: "Governance",
    headline: "Effective policy stack for this scope.",
    useWhen: "Read ordering here; change content on Policy packs or Workflow.",
    firstPilotNote: "After Pilot proof when merge order or conflicts need resolution.",
    enterpriseFootnote: "Read-only stack; edits on Packs or Workflow.",
  },
  "governance-workflow": {
    layerBadge: "Governance",
    headline: "Submit finalized architecture outputs for governance review and promotion.",
    useWhen: "Pick one review and move from submission through approval, promotion, and activation.",
    firstPilotNote:
      "After Pilot proof when your team promotes finalized manifests through governed stages.",
    enterpriseFootnote: "Approvals and promotions follow the governance stages configured for your tenant.",
  },
  "policy-packs": {
    layerBadge: "Governance",
    headline: "Packs in scope, published versions, and effective JSON.",
    useWhen: "Start by reviewing inventory and diffs; publish or assign when your role allows.",
    firstPilotNote: "After Pilot proof when you steward pack publish and assignment.",
    enterpriseFootnote: "Inspect/compare first; lifecycle on API.",
  },
  "alert-rules": {
    layerBadge: "Governance",
    headline: "Metric thresholds that raise alerts after scans.",
    useWhen: "Define thresholds here; triage fired alerts on Alerts.",
    firstPilotNote: "Threshold tuning after Pilot proof when scans drive production signals.",
    enterpriseFootnote: "Thresholds on scan outcomes.",
  },
  "alert-routing": {
    layerBadge: "Governance",
    headline: "Where fired alerts are delivered.",
    useWhen: "Targets for fired alerts—not digest mail.",
    firstPilotNote: "Destinations after Pilot proof when fired alerts need routing.",
    enterpriseFootnote: "Delivery targets for fired alerts.",
  },
  "alert-simulation": {
    layerBadge: "Governance",
    headline: "Dry-run rules against recent reviews.",
    useWhen: "What-if before changing production thresholds; triage on Alerts.",
    firstPilotNote: "What-if after Pilot proof before changing live thresholds.",
    enterpriseFootnote: "Simulation before production change.",
  },
  "alert-tuning": {
    layerBadge: "Governance",
    headline: "Balance coverage vs. noise for one rule.",
    useWhen: "After simulation shows a tradeoff worth fixing.",
    firstPilotNote: "After Pilot proof when simulation evidence backs a live change.",
    enterpriseFootnote: "Tuning from simulation evidence.",
  },
  "composite-alert-rules": {
    layerBadge: "Governance",
    headline: "Combine metrics with AND/OR before firing.",
    useWhen: "Use when one metric is not enough; add cooldown as needed.",
    firstPilotNote: "Composite rules after Pilot proof when AND/OR firing is in scope.",
    enterpriseFootnote: "AND/OR and cooldown configuration.",
  },
};

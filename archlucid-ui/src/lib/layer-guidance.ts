/**
 * In-product copy for the **Pilot** and **Operate** buyer layers (**docs/PRODUCT_PACKAGING.md**,
 * **docs/OPERATOR_DECISION_GUIDE.md**). Consumed by **`LayerHeader`** (`LayerGuidancePageKey` per route family).
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
  /** Optional reminder for first pilots */
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
    headline: "Answers: what changed between two committed runs?",
    useWhen: "Use after you have two runs with golden manifests when you need a structured diff or narrative.",
    firstPilotNote: "Not needed for your first pilot unless you are explicitly comparing two outcomes.",
  },
  replay: {
    layerBadge: "Operate",
    headline: "Answers: does the stored provenance chain still validate for this run?",
    useWhen: "Use when you need drift or integrity checks on a single run, not a visual diff.",
    firstPilotNote: "Optional until you need to prove or debug chain validation.",
  },
  graph: {
    layerBadge: "Operate",
    headline: "Answers: how does provenance or architecture look for one run?",
    useWhen: "Use when tables and compare are not enough and you need a visual exploration.",
    firstPilotNote: "Skip until you have a committed run and a concrete graph question.",
  },
  "governance-dashboard": {
    layerBadge: "Operate",
    headline: "Cross-run approvals and governance signals.",
    useWhen: "Queue snapshot; open a row to continue in workflow for that run.",
    firstPilotNote: "Defer until cross-run triage is routine.",
    enterpriseFootnote: "Signals here; writes in workflow.",
  },
  alerts: {
    layerBadge: "Operate",
    headline: "Risk and compliance signals that need triage.",
    useWhen: "Work the inbox; thresholds and routing live under Alert tooling.",
    firstPilotNote: "Defer rule tuning until volume justifies it.",
    enterpriseFootnote: "Inbox first; tooling for config.",
  },
  audit: {
    layerBadge: "Operate",
    headline: "Tenant audit trail—who did what, when.",
    useWhen: "Search first; CSV reuses the same From/To window (Auditor/Admin on API).",
    firstPilotNote: "Defer export until the window and roles are settled.",
    enterpriseFootnote: "Search + bounded CSV export.",
  },
  "security-trust": {
    layerBadge: "Operate",
    headline: "Procurement-facing security posture and NDA-gated pen-test summaries.",
    useWhen: "Use when buyers need CAIQ/SIG pointers, Trust Center links, and the NDA path for redacted pen-test excerpts.",
    firstPilotNote:
      "Redacted third-party summaries are NDA-only — contact security@ from this page; no public pen-test body is hosted here.",
    enterpriseFootnote: "Read-oriented; Admin API may still emit SecurityAssessmentPublished for audit/SIEM without implying public publication.",
  },
  "teams-notifications": {
    layerBadge: "Operate",
    headline: "Microsoft Teams channel wiring for integration-event fan-out.",
    useWhen: "After Service Bus topics are live and operators want run / governance / alert cards in Teams.",
    firstPilotNote: "Store only a Key Vault secret name here — the webhook URL stays in Key Vault.",
    enterpriseFootnote: "Read vs Execute matches API; Logic Apps resolves the secret at delivery time.",
  },
  "value-report": {
    layerBadge: "Operate",
    headline: "Sponsor-facing value DOCX for a UTC window.",
    useWhen: "After you have committed runs; pairs with ROI_MODEL for CFO-ready narrative.",
    firstPilotNote: "Defer until the tenant is on Standard tier and operators need sponsor collateral.",
    enterpriseFootnote: "Execute + Standard tier on API; LLM line is estimated per ROI_MODEL when SQL token ledger absent.",
  },
  "governance-resolution": {
    layerBadge: "Operate",
    headline: "Effective policy stack for this scope.",
    useWhen: "Read ordering here; change content on Policy packs or Workflow.",
    firstPilotNote: "Defer until merge order or conflicts matter.",
    enterpriseFootnote: "Read-only stack; edits on Packs or Workflow.",
  },
  "governance-workflow": {
    layerBadge: "Operate",
    headline: "Run-scoped submit, review, promote, activate.",
    useWhen: "One run ID; follow status top to bottom.",
    firstPilotNote: "Defer until SoD or promotions apply.",
    enterpriseFootnote: "Run workflow; API-gated writes.",
  },
  "policy-packs": {
    layerBadge: "Operate",
    headline: "Packs in scope, published versions, and effective JSON.",
    useWhen: "Inspect inventory and diff; lifecycle actions last.",
    firstPilotNote: "Defer lifecycle until you own publishing.",
    enterpriseFootnote: "Inspect/compare first; lifecycle on API.",
  },
  "alert-rules": {
    layerBadge: "Operate",
    headline: "Metric thresholds that raise alerts after scans.",
    useWhen: "Define thresholds here; triage fired alerts on Alerts.",
    firstPilotNote: "Skip until limits are operational.",
    enterpriseFootnote: "Thresholds on scan outcomes.",
  },
  "alert-routing": {
    layerBadge: "Operate",
    headline: "Where fired alerts are delivered.",
    useWhen: "Targets for fired alerts—not digest mail.",
    firstPilotNote: "Skip until live delivery matters.",
    enterpriseFootnote: "Delivery targets for fired alerts.",
  },
  "alert-simulation": {
    layerBadge: "Operate",
    headline: "Dry-run rules against recent runs.",
    useWhen: "What-if before changing production thresholds; triage on Alerts.",
    firstPilotNote: "Skip until you have a concrete scenario.",
    enterpriseFootnote: "Simulation before production change.",
  },
  "alert-tuning": {
    layerBadge: "Operate",
    headline: "Balance coverage vs. noise for one rule.",
    useWhen: "After simulation shows a tradeoff worth fixing.",
    firstPilotNote: "Skip until simulation justifies a change.",
    enterpriseFootnote: "Tuning from simulation evidence.",
  },
  "composite-alert-rules": {
    layerBadge: "Operate",
    headline: "Combine metrics with AND/OR before firing.",
    useWhen: "Use when one metric is not enough; add cooldown as needed.",
    firstPilotNote: "Skip until composite firing is in scope.",
    enterpriseFootnote: "AND/OR and cooldown configuration.",
  },
};

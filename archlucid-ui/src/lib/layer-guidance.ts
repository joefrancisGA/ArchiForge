/**
 * In-product copy for the three **product packaging** layers (**docs/PRODUCT_PACKAGING.md**,
 * **docs/OPERATOR_DECISION_GUIDE.md**). Consumed by **`LayerHeader`** (`LayerGuidancePageKey` per route family).
 *
 * **Drift guard:** adding a key requires wiring **`LayerHeader`** on the page and, if the capability is listed for
 * buyers, updating **PRODUCT_PACKAGING.md** — see §3 *Contributor drift guard*.
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
    useWhen: "Queue with refresh—not run-by-run artifact review.",
    firstPilotNote: "Skip until cross-run signals are in scope.",
    enterpriseFootnote:
      "Cross-run evidence; writes are operator/admin. Not required for Core Pilot.",
  },
  alerts: {
    layerBadge: "Enterprise Controls",
    headline: "Answers: what risk or compliance signals fired and need triage?",
    useWhen: "Read and filter the inbox first; changing how signals fire is a different surface.",
    firstPilotNote: "Defer deeper config until alerts are a pilot requirement.",
    enterpriseFootnote: "Inbox first; rules, routing, tuning are follow-on depth.",
  },
  audit: {
    layerBadge: "Enterprise Controls",
    headline: "Answers: tenant audit trail—who did what, when?",
    useWhen: "Search in scope; export needs a bounded From/To window.",
    firstPilotNote: "Skip until audit evidence is a pilot requirement.",
    enterpriseFootnote: "Evidence search and bounded export. Not required for Core Pilot.",
  },
  "governance-resolution": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: which policy content is in effect for this scope after pack ordering?",
    useWhen: "Inspect the effective stack before changing assignments or ordering elsewhere.",
    firstPilotNote: "Skip until cross-pack ordering matters to the pilot.",
    enterpriseFootnote:
      "Effective policy here; changes live in policy packs or workflow. Not required for Core Pilot.",
  },
  "governance-workflow": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: run-scoped submit, approve/reject, promote, and activate?",
    useWhen: "One run at a time—not day-one artifact review.",
    firstPilotNote: "Skip until promotions and segregation of duties apply.",
    enterpriseFootnote:
      "Operator/admin surface for approvals and activation. API enforces writes. Not required for Core Pilot.",
  },
  "policy-packs": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: what packs exist, what is published, and what applies in this scope?",
    useWhen: "Inspect inventory and effective JSON first; lifecycle when governance owns changes.",
    firstPilotNote: "Skip until policy control is explicit.",
    enterpriseFootnote:
      "Read/compare first; create, publish, assign are configuration (API-enforced). Not required for Core Pilot.",
  },
  "alert-rules": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: which metric thresholds should raise alerts after advisory scans?",
    useWhen: "Operational thresholds on scan outcomes—not inbox triage.",
    firstPilotNote: "Skip until thresholds are part of how you operate.",
    enterpriseFootnote:
      "Configuration surface for metric thresholds. Not required for Core Pilot.",
  },
  "alert-routing": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: where should fired alerts be delivered when severity thresholds are met?",
    useWhen: "Live delivery targets when alerts fire—not digest mail.",
    firstPilotNote: "Skip until real-time routing matters.",
    enterpriseFootnote:
      "Configuration surface for alert delivery. Not required for Core Pilot.",
  },
  "alert-simulation": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: how would rules behave against recent runs before changing production thresholds?",
    useWhen: "Dry-run on history; live triage stays on Alerts.",
    firstPilotNote: "Optional until tuning with real scan windows.",
    enterpriseFootnote:
      "Read-focused simulation before production rule changes. Not required for Core Pilot.",
  },
  "alert-tuning": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: which threshold values balance coverage and noise for a chosen rule?",
    useWhen: "Apply scores from simulation when noise or coverage is measurable.",
    firstPilotNote: "Defer until alert noise or coverage is measurable.",
    enterpriseFootnote:
      "Configuration surface for threshold tuning. Not required for Core Pilot.",
  },
  "composite-alert-rules": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: how do we combine multiple scan metrics with AND/OR before firing an alert?",
    useWhen: "AND/OR with cooldown when a single threshold is not enough.",
    firstPilotNote: "Defer until composite firing logic is in scope.",
    enterpriseFootnote:
      "Configuration surface for composite alert rules. Not required for Core Pilot.",
  },
};

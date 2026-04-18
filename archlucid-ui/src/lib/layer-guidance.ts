/**
 * In-product copy for the three product layers (see docs/PRODUCT_PACKAGING.md,
 * docs/OPERATOR_DECISION_GUIDE.md). Keep strings short — long-form stays in docs.
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
    headline: "Answers: what approvals, policy, and compliance signals need attention across runs?",
    useWhen: "Use when you need a cross-run readout—not a substitute for run-scoped workflow.",
    firstPilotNote: "Skip until cross-run approvals or policy signals are part of the pilot.",
    enterpriseFootnote:
      "Cross-run readout—typically governance or platform operators. Not required for Core Pilot.",
  },
  alerts: {
    layerBadge: "Enterprise Controls",
    headline: "Answers: what risk or compliance signals fired and need triage?",
    useWhen: "Use when triage or routing is part of how you operate on scan findings.",
    firstPilotNote: "Inbox for triage anytime; rules and routing depth can follow later.",
    enterpriseFootnote:
      "Deeper routing, rules, and tuning are operator/admin surfaces when you need them—not required for Core Pilot.",
  },
  audit: {
    layerBadge: "Enterprise Controls",
    headline: "Answers: who did what, when, with which correlation id, for audit evidence?",
    useWhen: "Use when you need tenant-scoped search or CSV export beyond a single run timeline.",
    firstPilotNote: "Defer until audit evidence is in scope for the pilot.",
    enterpriseFootnote: "Evidence for sponsors and audit—still not required for Core Pilot.",
  },
  "governance-resolution": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: which governance policy content is in effect for this scope after pack ordering?",
    useWhen: "Use when you need a readout of effective rules, conflicts, and resolution decisions before changing assignments.",
    firstPilotNote: "Defer until cross-pack resolution is part of the pilot.",
    enterpriseFootnote:
      "Read-oriented governance evidence—usually reviewed by governance or platform leads. Not required for Core Pilot.",
  },
  "governance-workflow": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: how do we submit, approve, promote, and activate manifests for one run?",
    useWhen: "Use when governance workflow or environment activation is in scope—not for day-one artifact review alone.",
    firstPilotNote: "Skip until promotion paths and segregation of duties are part of the pilot.",
    enterpriseFootnote:
      "Operator/admin governance control surface—the API enforces who may approve, promote, and activate. Not required for Core Pilot.",
  },
  "policy-packs": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: which policy packs exist, what is published, and what applies to this scope?",
    useWhen: "Use when pack lifecycle, effective JSON, or assignments are in scope for governance or platform operators.",
    firstPilotNote: "Defer until policy control is an explicit pilot requirement.",
    enterpriseFootnote:
      "Mixed read and pack lifecycle writes—stricter steps may require admin on the API. Not required for Core Pilot.",
  },
  "alert-rules": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: which metric thresholds should raise alerts after advisory scans?",
    useWhen: "Use when operational alerting on scan outcomes is in scope—not for first-pilot proof alone.",
    firstPilotNote: "Defer until alert thresholds are part of how you operate.",
    enterpriseFootnote:
      "Operational alert configuration—used when governance or operational control on findings is needed. Not required for Core Pilot.",
  },
  "alert-routing": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: where should fired alerts be delivered when severity thresholds are met?",
    useWhen: "Use when subscriptions or delivery attempts need review alongside the alerts inbox.",
    firstPilotNote: "Defer until routing is part of your operating model.",
    enterpriseFootnote:
      "Operational routing control—used when governance or operational control on alert delivery is needed. Not required for Core Pilot.",
  },
  "alert-simulation": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: how would rules behave against recent runs before changing production thresholds?",
    useWhen: "Use for what-if evaluation on historical scan signals—not a substitute for the live alerts inbox.",
    firstPilotNote: "Optional until you are tuning thresholds with real scan history.",
    enterpriseFootnote:
      "Read-leaning what-if with operator-class evaluation calls—still not required for Core Pilot.",
  },
  "alert-tuning": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: which threshold values balance coverage and noise for a chosen rule?",
    useWhen: "Use when you want scored recommendations from simulation—not for first-pilot essentials.",
    firstPilotNote: "Defer until alert noise or coverage is a measured problem.",
    enterpriseFootnote:
      "Operational tuning support—used when governance or operational control on alert thresholds is needed. Not required for Core Pilot.",
  },
  "composite-alert-rules": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: how do we combine multiple scan metrics with AND/OR before firing an alert?",
    useWhen: "Use when composite conditions, cooldown, or suppression are required for operational signal quality.",
    firstPilotNote: "Defer until composite firing logic is in scope.",
    enterpriseFootnote:
      "Operational alert configuration—used when governance or operational control on composite signals is needed. Not required for Core Pilot.",
  },
};

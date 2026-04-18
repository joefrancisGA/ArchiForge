/**
 * In-product copy for the three product layers (see docs/PRODUCT_PACKAGING.md,
 * docs/OPERATOR_DECISION_GUIDE.md). Keep strings short — long-form stays in docs.
 */

export type LayerGuidancePageKey =
  | "compare"
  | "replay"
  | "graph"
  | "governance-dashboard"
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
    useWhen: "Use when segregation of duties, promotions, or cross-run governance is in scope.",
    firstPilotNote: "Usually not required to judge first-pilot value from Core Pilot alone.",
    enterpriseFootnote:
      "Cross-run readout—typically governance or platform operators. Not required for Core Pilot.",
  },
  alerts: {
    layerBadge: "Enterprise Controls",
    headline: "Answers: what risk or compliance signals fired and need triage?",
    useWhen: "Use when alert routing, acknowledgment, or investigation is part of your operating model.",
    firstPilotNote: "Inbox is available early; deep rule tuning can wait until governance needs it.",
    enterpriseFootnote:
      "Deeper routing, rules, and tuning are operator/admin surfaces when you need them—not required for Core Pilot.",
  },
  audit: {
    layerBadge: "Enterprise Controls",
    headline: "Answers: who did what, when, with which correlation id, for audit evidence?",
    useWhen: "Use when you need exportable evidence or investigations beyond run-scoped timeline.",
    firstPilotNote: "Not required until sponsors or compliance ask for durable audit trails.",
    enterpriseFootnote: "Evidence for sponsors and audit—still not required for Core Pilot.",
  },
};

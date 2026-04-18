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
    headline: "Answers: what approvals, policy, and compliance signals need attention across runs?",
    useWhen: "Cross-run queue—not the default run-by-run pilot path.",
    firstPilotNote: "Skip until cross-run signals are in scope.",
    enterpriseFootnote:
      "Evidence surface for cross-run signals; write actions are operator/admin surface. Not required for Core Pilot.",
  },
  alerts: {
    layerBadge: "Enterprise Controls",
    headline: "Answers: what risk or compliance signals fired and need triage?",
    useWhen: "Triage first; rules, routing, and tuning are configuration surfaces—not the default pilot path.",
    firstPilotNote: "Inbox when needed; deeper config can wait.",
    enterpriseFootnote:
      "Start with the inbox. Deeper routing, rules, and tuning are operator/admin surfaces when needed.",
  },
  audit: {
    layerBadge: "Enterprise Controls",
    headline: "Answers: who did what, when, with which correlation id, for audit evidence?",
    useWhen: "Tenant-scoped trail beyond run detail; export needs a bounded date range.",
    firstPilotNote: "Skip until audit evidence is a pilot requirement.",
    enterpriseFootnote: "Evidence surface for search and export. Not required for Core Pilot.",
  },
  "governance-resolution": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: which governance policy content is in effect for this scope after pack ordering?",
    useWhen: "Read the effective stack before changing pack order or assignments.",
    firstPilotNote: "Skip until cross-pack ordering matters to the pilot.",
    enterpriseFootnote:
      "Read-focused evidence surface for effective policy in this scope. Not required for Core Pilot.",
  },
  "governance-workflow": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: how do we submit, approve, promote, and activate manifests for one run?",
    useWhen: "Run-scoped approvals and activation—not day-one artifact review.",
    firstPilotNote: "Skip until promotions and segregation of duties apply.",
    enterpriseFootnote:
      "Operator/admin surface for approvals and activation. API enforces writes. Not required for Core Pilot.",
  },
  "policy-packs": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: which policy packs exist, what is published, and what applies to this scope?",
    useWhen: "Inventory, effective JSON, and lifecycle when governance owns packs.",
    firstPilotNote: "Skip until policy control is explicit.",
    enterpriseFootnote:
      "Read-focused pack review and comparison; lifecycle actions are configuration surface (API-enforced). Not required for Core Pilot.",
  },
  "alert-rules": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: which metric thresholds should raise alerts after advisory scans?",
    useWhen: "Operational alerting on scan outcomes—not first-pilot proof.",
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
    useWhen: "Dry-run on history—does not replace the live inbox.",
    firstPilotNote: "Optional until tuning with real scan windows.",
    enterpriseFootnote:
      "Read-focused simulation before production rule changes. Not required for Core Pilot.",
  },
  "alert-tuning": {
    layerBadge: "Enterprise Controls",
    headline: "Answers: which threshold values balance coverage and noise for a chosen rule?",
    useWhen: "Scored candidates from simulation—not first-pilot essentials.",
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

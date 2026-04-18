/**
 * Short, sober copy for Enterprise Controls context (nav + key pages).
 * Aligned with docs/OPERATOR_DECISION_GUIDE.md (default rule, §2 “Move to Enterprise Controls”) and
 * docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md (Stage 1 — role clarity without commercializing the wedge).
 * Keep wording responsibility-based, not permission-jargon.
 */

/** Sidebar / mobile: reader sees fewer links in this group */
export const enterpriseNavHintReaderRank =
  "Some destinations need operator or admin access and stay out of the list for your role—not required for Core Pilot.";

/** Sidebar / mobile: operator+ still reminded this layer is optional vs Core Pilot */
export const enterpriseNavHintOperatorRank =
  "Operator/admin surfaces—typically governance or platform operators. Not required for Core Pilot.";

/** Deep execute tooling: only when resolved rank is below Execute (e.g. Reader deep-linked) */
export const enterpriseExecutePageHintReaderRank =
  "Operator/admin surface. The API still enforces writes—not required for Core Pilot.";

/** Governance resolution (no LayerHeader page key): read-only effective policy view */
export const governanceResolutionContextLine =
  "Effective policy for the current scope—usually reviewed by governance or platform leads. Not required for Core Pilot.";

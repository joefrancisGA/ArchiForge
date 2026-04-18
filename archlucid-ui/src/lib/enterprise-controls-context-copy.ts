/**
 * Short, sober copy for Enterprise Controls context (nav + key pages).
 * Aligned with docs/OPERATOR_DECISION_GUIDE.md (default rule, §2 “Move to Enterprise Controls”) and
 * docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md (Stage 1 — role clarity without commercializing the wedge).
 * Keep wording responsibility-based, not permission-jargon.
 */

/** `title` on mutation controls the UI soft-disables for Reader-tier principals (API remains authoritative). */
export const enterpriseMutationControlDisabledTitle =
  "Requires operator-level access in this shell; the API still enforces every write.";

/** Sidebar / mobile: reader sees fewer links in this group */
export const enterpriseNavHintReaderRank =
  "Some operator/admin destinations stay out of the list for your role. Not required for Core Pilot.";

/** Sidebar / mobile: operator+ still reminded this layer is optional vs Core Pilot */
export const enterpriseNavHintOperatorRank =
  "Governance, audit, policy, and alert tooling—mainly for governance and platform operators. Not required for Core Pilot.";

/** Deep execute tooling: only when resolved rank is below Execute (e.g. Reader deep-linked) */
export const enterpriseExecutePageHintReaderRank =
  "Operator/admin surface. Not required for Core Pilot; the API still enforces writes.";

/** Second line on governance resolution — readers vs operators (see `GovernanceResolutionRankCue`). */
export const governanceResolutionRankReaderLine =
  "Read-focused evidence of what is currently in effect for this scope.";

export const governanceResolutionRankOperatorLine =
  "Read-focused view; use policy packs or governance workflow when you need to change assignments.";

/** Governance dashboard: readers can consume signals; in-product actions still need execute on the API. */
export const governanceDashboardReaderActionLine =
  "Read-focused dashboard. Approval and promotion actions follow operator-level API policy where configured.";

/** Governance workflow page — shown when resolved rank is operator+ (Reader already gets `EnterpriseControlsExecutePageHint`). */
export const enterpriseGovernanceWorkflowOperatorPlusLine =
  "Workflow surface for approvals, promotions, and activation. The API enforces who may write.";

/** Policy packs — operator+ reminder (Readers see write hint via `EnterpriseControlsExecutePageHint`). */
export const enterprisePolicyPacksOperatorPlusLine =
  "Read, compare, and review pack content here; stricter lifecycle actions remain API-enforced.";

/**
 * Alert rules / routing / simulation / tuning / composite — single rank-aware cue (`AlertOperatorToolingRankCue`).
 * Stacked Execute hint + operator line were consolidated into this pair.
 */
export const alertOperatorToolingReaderRankLine =
  "Read-focused inspection and what-if here; changing rules, routing, or subscriptions uses operator-level API policy where configured. Not required for Core Pilot.";

export const alertOperatorToolingOperatorRankLine =
  "Operator/admin operational-control surface—writes remain API-enforced by role. Not required for Core Pilot.";

/** Alerts inbox — readers vs operators (see `AlertsInboxRankCue`). */
export const alertsInboxRankReaderLine =
  "Read-focused inbox view; triage actions follow operator-level API policy where configured.";

export const alertsInboxRankOperatorLine =
  "Operational inbox for triage and follow-up; actions remain API-enforced by role.";

/** Audit log — readers vs operators (see `AuditLogRankCue`). */
export const auditLogRankReaderLine =
  "Read-focused evidence search; export and deeper fields follow API policy for your role.";

export const auditLogRankOperatorLine =
  "Investigation surface for search and export; actions remain API-enforced by role.";

/** Governance dashboard — operator+ when `GovernanceDashboardReaderActionCue` is hidden */
export const governanceDashboardOperatorPlusLine =
  "Cross-run oversight for approvals, decisions, and policy signals; write actions remain API-enforced.";

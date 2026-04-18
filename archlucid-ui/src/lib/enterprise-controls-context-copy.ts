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
  "Some destinations need operator or admin access and stay out of the list for your role—not required for Core Pilot.";

/** Sidebar / mobile: operator+ still reminded this layer is optional vs Core Pilot */
export const enterpriseNavHintOperatorRank =
  "Governance, audit, policy, and alert tooling—mainly for governance and platform operators. Not required for Core Pilot.";

/** Deep execute tooling: only when resolved rank is below Execute (e.g. Reader deep-linked) */
export const enterpriseExecutePageHintReaderRank =
  "Operator/admin surface. The API still enforces writes—not required for Core Pilot.";

/** Second line on governance resolution — readers vs operators (see `GovernanceResolutionRankCue`). */
export const governanceResolutionRankReaderLine =
  "Reader-oriented evidence of what is in effect for this scope—not required for Core Pilot.";

export const governanceResolutionRankOperatorLine =
  "Changing assignments uses policy packs or governance workflow; this page stays read-focused. Not required for Core Pilot.";

/** Governance dashboard: readers can consume signals; in-product actions still need execute on the API. */
export const governanceDashboardReaderActionLine =
  "Reader-oriented signals here; approve, reject, batch, and promote actions require operator-level API access where your tenant expects it—not required for Core Pilot.";

/** Governance workflow page — shown when resolved rank is operator+ (Reader already gets `EnterpriseControlsExecutePageHint`). */
export const enterpriseGovernanceWorkflowOperatorPlusLine =
  "Operator/admin workflow surface—the API enforces who may approve, promote, and activate. Not required for Core Pilot.";

/** Policy packs — operator+ reminder (Readers see write hint via `EnterpriseControlsExecutePageHint`). */
export const enterprisePolicyPacksOperatorPlusLine =
  "Pack lifecycle mixes reads with publish, assign, and create; stricter steps require admin on the API. Not required for Core Pilot.";

/**
 * Alert operator tooling pages — rank-aware single line (replaces stacked Execute hint + operator cue on those routes).
 * Readers: evidence / what-if framing; operators: control surface framing (see `AlertOperatorToolingRankCue`).
 */
export const alertOperatorToolingReaderRankLine =
  "Read-oriented inspection and what-if here; changing rules, routing, or subscriptions uses operator-level API access where your tenant expects it—not required for Core Pilot.";

export const alertOperatorToolingOperatorRankLine =
  "Operator/admin operational-control surface—writes remain API-enforced by role. Not required for Core Pilot.";

/** Alerts inbox — readers vs operators (see `AlertsInboxRankCue`). */
export const alertsInboxRankReaderLine =
  "Reader-oriented inbox view—acknowledge, resolve, and suppress still follow operator-level API policy where your tenant expects it—not required for Core Pilot.";

export const alertsInboxRankOperatorLine =
  "Operational triage surface—inbox actions remain API-enforced by role. Not required for Core Pilot.";

/** Audit log — readers vs operators (see `AuditLogRankCue`). */
export const auditLogRankReaderLine =
  "Reader-oriented evidence search; export and deeper fields follow API policy for your role—not required for Core Pilot.";

export const auditLogRankOperatorLine =
  "Audit investigation surface—search and export remain API-enforced by role. Not required for Core Pilot.";

/** Governance dashboard — operator+ when `GovernanceDashboardReaderActionCue` is hidden */
export const governanceDashboardOperatorPlusLine =
  "Operator oversight—approve, reject, batch, and promote controls are API-enforced (including segregation of duties where configured). Not required for Core Pilot.";

/**
 * Short, sober copy for Enterprise Controls context (nav, key pages, and selected empty-state / card-description strings).
 * Aligned with docs/OPERATOR_DECISION_GUIDE.md (default rule, §2 “Move to Enterprise Controls”) and
 * docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md (Stage 1 — role clarity without commercializing the wedge).
 * Keep wording responsibility-based, not permission-jargon.
 */

/** `title` on mutation controls the UI soft-disables for Reader-tier principals (API remains authoritative). */
export const enterpriseMutationControlDisabledTitle =
  "Requires operator-level access in this shell; the API still enforces every write.";

/** Sidebar / mobile: reader sees fewer links in this group */
export const enterpriseNavHintReaderRank =
  "Some operator/admin destinations stay out of your list for this role. Not required for Core Pilot.";

/** Sidebar / mobile: operator+ still reminded this layer is optional vs Core Pilot */
export const enterpriseNavHintOperatorRank =
  "Typically used by governance or platform operators. Not required for Core Pilot.";

/**
 * `LayerHeader` rank-aware line under `enterpriseFootnote` on Enterprise Controls pages (same threshold as nav hints:
 * below Execute → reader framing).
 */
export const layerHeaderEnterpriseReaderRankLine =
  "Typically used by governance or platform operators for operational changes; read views still help. Not required for Core Pilot.";

export const layerHeaderEnterpriseOperatorRankLine =
  "Operator/admin surface when your operating model needs it—not required for Core Pilot.";

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

/** Extra line under the pending-approvals empty state when rank is below Execute (batch/review CTAs are disabled). */
export const governanceDashboardPendingClearReaderSupplement =
  "Read-tier view: when items appear here, approve, reject, and batch actions stay disabled in this shell until operator-level access applies to you—the API remains authoritative.";

/** Governance workflow — “Approval requests for a run” card description by rank. */
export const governanceWorkflowQueryCardDescriptionReader =
  "Load one run to inspect approvals, promotions, and activations. Approve, reject, promote, and activate follow operator-level API policy where configured.";

export const governanceWorkflowQueryCardDescriptionOperator =
  "Load rows for one run, then approve, reject, or promote according to status.";

/** No rows returned for the loaded run — reader copy references submit section position when inspect-first layout is used. */
export const governanceWorkflowNoApprovalsReaderHint =
  "Nothing in Submitted/Draft for this run. Pick another run ID, or ask an operator to submit from the Submit section.";

export const governanceWorkflowNoApprovalsOperatorHint =
  "Submit a request above or pick another run ID.";

/** Policy packs — empty list under “Packs in scope”. */
export const policyPacksEmptyScopeReaderLine =
  "No packs in this scope yet. Inspect effective packs and JSON below; create, publish, and assign require operator-level access where configured.";

export const policyPacksEmptyScopeOperatorLine = "No packs yet.";

/** Alert rules — empty “Defined rules” list. */
export const alertRulesDefinedListEmptyReaderLine =
  "No rules in this scope yet. Review thresholds under Configure new rule (read-only here); operators enable writes on the API.";

export const alertRulesDefinedListEmptyOperatorLine = "None yet.";

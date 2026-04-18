/**
 * Short, sober copy for Enterprise Controls context (nav, key pages, and selected empty-state / card-description strings).
 * Aligned with docs/OPERATOR_DECISION_GUIDE.md (default rule, §2 “Move to Enterprise Controls”) and
 * docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md (Stage 1 — role clarity without commercializing the wedge).
 * Keep wording responsibility-based, not permission-jargon.
 *
 * **Rank pairing:** several `*Reader*` / `*Operator*` pairs are chosen in pages via `useEnterpriseMutationCapability()` or
 * `useNavCallerAuthorityRank()` vs `AUTHORITY_RANK.ExecuteAuthority` — keep thresholds aligned with `nav-authority.ts`.
 */

/** `title` on mutation controls the UI soft-disables for Reader-tier principals (API remains authoritative). */
export const enterpriseMutationControlDisabledTitle =
  "Requires operator-level access in this shell; the API still enforces every write.";

/**
 * Audit CSV export uses **`RequireAuditor`** on the API (Auditor **or** Admin)—stricter than Execute-tier pack
 * mutations; align the Export button with **`/me` role claims**, not `useEnterpriseMutationCapability`.
 */
export const auditExportControlDisabledTitle =
  "CSV export requires Auditor or Admin on the API for this tenant; search above still works for your role.";

/** Sidebar / mobile: reader sees fewer links in this group */
export const enterpriseNavHintReaderRank =
  "Read-focused nav: operator/admin surfaces stay off your list for this role. Not required for Core Pilot.";

/** Sidebar / mobile: operator+ still reminded this layer is optional vs Core Pilot */
export const enterpriseNavHintOperatorRank =
  "Operator/admin surface for governance and platform work. Not required for Core Pilot.";

/**
 * `LayerHeader` rank-aware line under `enterpriseFootnote` on Enterprise Controls pages (same threshold as nav hints:
 * below Execute → reader framing).
 */
export const layerHeaderEnterpriseReaderRankLine =
  "Read-focused views here; operational changes are operator/admin surface. Not required for Core Pilot.";

export const layerHeaderEnterpriseOperatorRankLine =
  "Operator/admin surface when your operating model needs it. Not required for Core Pilot.";

/** Deep execute tooling: only when resolved rank is below Execute (e.g. Reader deep-linked) */
export const enterpriseExecutePageHintReaderRank =
  "Operator/admin surface. Not required for Core Pilot. The API still enforces writes.";

/** Second line on governance resolution — readers vs operators (see `GovernanceResolutionRankCue`). */
export const governanceResolutionRankReaderLine =
  "Read-focused evidence surface for effective policy in this scope.";

export const governanceResolutionRankOperatorLine =
  "Read-focused evidence surface; change assignments via policy packs or governance workflow.";

/** Governance dashboard: readers can consume signals; in-product actions still need execute on the API. */
export const governanceDashboardReaderActionLine =
  "Read-focused evidence surface; approvals and promotions are operator/admin surface (API policy).";

/** Governance workflow page — shown when resolved rank is operator+ (Reader already gets `EnterpriseControlsExecutePageHint`). */
export const enterpriseGovernanceWorkflowOperatorPlusLine =
  "Operator/admin surface for approvals, promotions, and activation. The API enforces who may write.";

/** Policy packs — operator+ reminder (Readers see write hint via `EnterpriseControlsExecutePageHint`). */
export const enterprisePolicyPacksOperatorPlusLine =
  "Read/compare first; lifecycle (create, publish, assign) is configuration—API-enforced.";

/**
 * Alert rules / routing / simulation / tuning / composite — single rank-aware cue (`AlertOperatorToolingRankCue`).
 * Stacked Execute hint + operator line were consolidated into this pair.
 */
export const alertOperatorToolingReaderRankLine =
  "Read-focused inspection and simulation; rule and routing changes are operator/admin surface (API policy). Not required for Core Pilot.";

export const alertOperatorToolingOperatorRankLine =
  "Operator/admin configuration surface; writes API-enforced by role. Not required for Core Pilot.";

/** Alerts inbox — readers vs operators (see `AlertsInboxRankCue`). */
export const alertsInboxRankReaderLine =
  "Read-focused inbox; triage is operator/admin surface (API policy).";

export const alertsInboxRankOperatorLine =
  "Operator/admin surface for triage; writes API-enforced by role.";

/** Audit log — readers vs operators (see `AuditLogRankCue`). */
export const auditLogRankReaderLine =
  "Read-focused evidence surface; CSV export is operator/admin surface (API policy for your role).";

export const auditLogRankOperatorLine =
  "Evidence surface for search and export; actions API-enforced by role.";

/** Governance dashboard — operator+ when `GovernanceDashboardReaderActionCue` is hidden */
export const governanceDashboardOperatorPlusLine =
  "Operator/admin surface for cross-run oversight; writes API-enforced.";

/** Extra line under the pending-approvals empty state when rank is below Execute (batch/review CTAs are disabled). */
export const governanceDashboardPendingClearReaderSupplement =
  "Read-focused: approve, reject, and batch actions stay disabled here until operator-level access applies. API is authoritative.";

/** Governance workflow — “Approval requests for a run” card description by rank. */
export const governanceWorkflowQueryCardDescriptionReader =
  "Load one run for read-focused inspection; approve, reject, promote, and activate are operator/admin surface (API policy).";

export const governanceWorkflowQueryCardDescriptionOperator =
  "Load rows for one run, then approve, reject, or promote according to status.";

/** No rows returned for the loaded run — reader copy references submit section position when inspect-first layout is used. */
export const governanceWorkflowNoApprovalsReaderHint =
  "Nothing in Submitted/Draft for this run. Pick another run ID, or ask an operator to submit from the Submit section.";

export const governanceWorkflowNoApprovalsOperatorHint =
  "Submit a request above or pick another run ID.";

/** Policy packs — empty list under “Packs in scope”. */
export const policyPacksEmptyScopeReaderLine =
  "No packs in this scope yet. Read-focused review below; create, publish, and assign are operator/admin surface (API policy).";

export const policyPacksEmptyScopeOperatorLine = "No packs yet.";

/** Alert rules — empty “Defined rules” list. */
export const alertRulesDefinedListEmptyReaderLine =
  "No rules in this scope yet. Read-focused threshold review; writes are operator/admin surface (API policy).";

export const alertRulesDefinedListEmptyOperatorLine = "None yet.";

/** Alert routing — empty “Current routing” list (mirrors alert rules empty pattern). */
export const alertRoutingSubscriptionsEmptyReaderLine =
  "No routing subscriptions in this scope yet. Read-focused delivery history below; create, enable, and disable are operator/admin surface (API policy).";

export const alertRoutingSubscriptionsEmptyOperatorLine = "None yet.";

/** Governance workflow — promotions timeline empty (after a run is loaded). */
export const governanceWorkflowPromotionsEmptyReaderHint =
  "No promotion rows yet for this run. When an operator approves and promotes, evidence appears here for read-focused review.";

export const governanceWorkflowPromotionsEmptyOperatorHint = "Promote an approved request to see rows here.";

/** Governance workflow — activations list empty. */
export const governanceWorkflowActivationsEmptyReaderHint =
  "No activation rows yet. Activations appear after an operator runs Activate on a promotion; read-focused inspection only at your rank.";

export const governanceWorkflowActivationsEmptyOperatorHint =
  "Use Activate on a promotion card after promotions exist.";

/** Alerts inbox — filtered empty state (Reader: deemphasize triage/configure as primary path). */
export const alertsFilteredEmptyDescriptionReader =
  "Nothing in this inbox matches the status filter and page above. Read-focused view: adjust filters or refresh—state-changing triage needs operator-level access in this shell (API still authoritative).";

export const alertsFilteredEmptyDescriptionOperator =
  "Nothing in this inbox matches the status filter and page above. Try All or another status, refresh, or adjust paging—an empty list here means no rows matched, not a silent scan failure.";

/** Audit log — zero rows after a successful search. */
export const auditSearchNoResultsReaderLine =
  "No audit events match these filters. Read-focused evidence search above; CSV export stays Auditor/Admin-gated on the API.";

export const auditSearchNoResultsOperatorLine = "No audit events match your filters.";

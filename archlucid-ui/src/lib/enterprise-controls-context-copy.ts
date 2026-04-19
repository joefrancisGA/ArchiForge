/**
 * Short, sober copy for Enterprise Controls context (nav, key pages, and selected empty-state / card-description strings).
 * Aligned with docs/OPERATOR_DECISION_GUIDE.md (default rule, §2 “Move to Enterprise Controls”) and
 * docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md (Stage 1 — role clarity without commercializing the wedge).
 * Keep wording responsibility-based, not permission-jargon.
 *
 * **Rank pairing:** several `*Reader*` / `*Operator*` pairs are chosen in pages via `useEnterpriseMutationCapability()` or
 * `useNavCallerAuthorityRank()` vs `AUTHORITY_RANK.ExecuteAuthority` — keep thresholds aligned with `nav-authority.ts`.
 */

/**
 * Shared one-liner under alert-tooling “Change configuration” sections — replaces repeating “Configuration surface…”
 * on every page (`alert-rules`, `alert-routing`, `alert-tuning`, `composite-alert-rules`).
 */
export const alertToolingConfigureSectionSubline =
  "Below: operator configuration (API-gated). First pilot stays inbox-first.";

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
  "Pack and assignment changes stay in policy packs or workflow (API policy).";

export const governanceResolutionRankOperatorLine =
  "Ordering and assignments live in policy packs or governance workflow—not this page.";

/** Governance dashboard: readers can consume signals; in-product actions still need execute on the API. */
export const governanceDashboardReaderActionLine =
  "Read-focused queue; approvals and promotions need Execute+ on the API.";

/** Governance workflow — lead under page title when caller can mutate (Execute+ in shell). */
export const governanceWorkflowPageLeadOperator =
  "One run: load ID below, then submit → approve/reject → promote → activate (per environment).";

/** Governance workflow — lead under page title for read tier (inspect-first layout already elevates Load). */
export const governanceWorkflowPageLeadReader =
  "Load a run ID to inspect approvals, promotions, and activations; submit and workflow writes stay operator/admin on the API (matches your rank in this shell).";

/** Governance workflow page — shown when resolved rank is operator+ (Reader already gets `EnterpriseControlsExecutePageHint`). */
export const enterpriseGovernanceWorkflowOperatorPlusLine =
  "Submit, approve/reject, promote, activate—API enforces who may write.";

/**
 * Governance workflow — inline review card when rank is below Execute (defense if UI state still shows the form;
 * Approve/Reject entry points are normally disabled for Reader).
 */
export const governanceWorkflowPendingReviewReaderNote =
  "Read-only preview at this rank in the shell; submit still requires operator-level access (API gate).";

/** Policy packs — operator+ reminder (Readers see write hint via `EnterpriseControlsExecutePageHint`). */
export const enterprisePolicyPacksOperatorPlusLine =
  "Compare and read first; lifecycle writes are API-enforced.";

/**
 * Alert rules / routing / simulation / tuning / composite — single rank-aware cue (`AlertOperatorToolingRankCue`).
 * Stacked Execute hint + operator line were consolidated into this pair.
 */
export const alertOperatorToolingReaderRankLine =
  "Inspect and simulate first; writes stay operator/admin (API). Not required for Core Pilot.";

export const alertOperatorToolingOperatorRankLine =
  "Operator/admin writes; API-enforced by role. Not required for Core Pilot.";

/** Alerts inbox — lead paragraph under page title (Execute+). */
export const alertsPageLeadOperator =
  "Inbox: filter first; triage changes state. Shortcuts mirror Ack / Resolve / Suppress below.";

/** Alerts inbox — lead paragraph under page title (read tier). */
export const alertsPageLeadReader =
  "Filter and refresh to inspect signals. Triage writes and keyboard shortcuts stay off at your rank until operator-level access applies (API authoritative).";

/** Alerts inbox — readers vs operators (see `AlertsInboxRankCue`). */
export const alertsInboxRankReaderLine =
  "Read-focused inbox; triage is operator/admin surface (API policy).";

export const alertsInboxRankOperatorLine =
  "Operator/admin surface for triage; writes API-enforced by role.";

/** Alerts triage confirmation dialog — extra copy when rank is below Execute (`alerts/page.tsx`). */
export const alertsTriageDialogReaderNote =
  "Confirm requires operator-level access in this shell; the API enforces every write.";

/** Audit log — readers vs operators (see `AuditLogRankCue`). */
export const auditLogRankReaderLine =
  "Inspect/search here; CSV export is Auditor or Admin on the API for your role.";

export const auditLogRankOperatorLine =
  "Evidence surface for search and export; actions API-enforced by role.";

/** Governance dashboard — operator+ when `GovernanceDashboardReaderActionCue` is hidden */
export const governanceDashboardOperatorPlusLine =
  "Operator/admin surface for cross-run oversight; writes API-enforced.";

/** Extra line under the pending-approvals empty state when rank is below Execute (batch/review CTAs are disabled). */
export const governanceDashboardPendingClearReaderSupplement =
  "Read-focused: batch and row actions stay disabled until operator-level access applies (API authoritative).";

/** Governance workflow — “Approval requests for a run” card description by rank. */
export const governanceWorkflowQueryCardDescriptionReader =
  "Load one run to inspect; approve, reject, promote, and activate need operator/admin on the API.";

export const governanceWorkflowQueryCardDescriptionOperator =
  "Load rows for one run, then approve, reject, or promote according to status.";

/** No rows returned for the loaded run — reader copy references submit section position when inspect-first layout is used. */
export const governanceWorkflowNoApprovalsReaderHint =
  "Nothing in Submitted/Draft for this run. Pick another run ID, or ask an operator to submit from the Submit section.";

export const governanceWorkflowNoApprovalsOperatorHint =
  "Submit a request above or pick another run ID.";

/** Policy packs — empty list under “Packs in scope”. */
export const policyPacksEmptyScopeReaderLine =
  "None in scope yet. Inspect effective data when present; create and lifecycle need operator on the API.";

export const policyPacksEmptyScopeOperatorLine = "No packs yet.";

/** Policy packs — “Published versions” empty when a pack is selected but no rows returned. */
export const policyPacksPublishedVersionsEmptyReaderLine =
  "No published versions in the response yet. Inspect here; publish is operator/admin on the API.";

export const policyPacksPublishedVersionsEmptyOperatorLine =
  "No published versions loaded for this pack yet.";

/** Policy packs — one line under Lifecycle heading for read tier (forms below stay soft-disabled). */
export const policyPacksLifecycleLeadReaderLine =
  "Create, publish, and assign below stay operator/admin surface at your rank; API still enforces writes.";

/** Governance workflow — Submit card description for read tier (operator copy stays inline on the page with API path). */
export const governanceWorkflowSubmitCardDescriptionReader =
  "Same API contract as operators; submit stays disabled at your rank in this shell until Execute-level access applies.";

/** Composite alert rules — empty “Current composite rules” list. */
export const compositeRulesDefinedListEmptyReaderLine =
  "No composite rules in this scope yet. Read-focused AND/OR review; writes are operator/admin surface (API policy).";

export const compositeRulesDefinedListEmptyOperatorLine = "None yet.";

/** Composite rules — under “Change configuration” for read tier. */
export const compositeRulesChangeConfigurationLeadReaderLine =
  "AND/OR form below stays operator/admin at your rank; API enforces writes.";

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
  "Nothing matches this filter and page. Adjust filters or refresh; triage writes need operator-level access here (API authoritative).";

export const alertsFilteredEmptyDescriptionOperator =
  "Nothing in this inbox matches the status filter and page above. Try All or another status, refresh, or adjust paging—an empty list here means no rows matched, not a silent scan failure.";

/** Audit log — zero rows after a successful search. */
export const auditSearchNoResultsReaderLine =
  "No audit events match these filters. Read-focused evidence search above; CSV export stays Auditor/Admin-gated on the API.";

export const auditSearchNoResultsOperatorLine = "No audit events match your filters.";

/** Audit log — under “Search audit events” for read tier (Execute floor; CSV export still Auditor/Admin on API). */
export const auditSearchSectionLeadReaderLine =
  "Search below is inspect; export is last and reuses From/To (CSV: Auditor or Admin on the API).";

/** Alert rules — one line under `AlertOperatorToolingRankCue` for read tier (list + refresh stay usable). */
export const alertRulesPageIntroReaderLine =
  "Rules list and refresh are inspect; Create rule and threshold edits stay operator/admin at your rank.";

/** Alert routing — one line under `AlertOperatorToolingRankCue` for read tier (delivery attempts stay read-only). */
export const alertRoutingPageIntroReaderLine =
  "Subscriptions, delivery attempts, and refresh are inspect; enable/disable and new routes stay operator/admin at your rank.";

/** Alert rules — under “Change configuration” for read tier. */
export const alertRulesChangeConfigurationLeadReaderLine =
  "Thresholds and Create below stay operator/admin at your rank; API enforces writes.";

/** Alert routing — under “Change configuration” for read tier. */
export const alertRoutingChangeConfigurationLeadReaderLine =
  "New subscriptions and toggles below stay operator/admin at your rank; API enforces writes.";

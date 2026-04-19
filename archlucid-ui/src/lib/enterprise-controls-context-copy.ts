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
export const alertToolingConfigureSectionSubline = "Inspect above · configure below (Execute+, API).";

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
export const enterpriseNavHintReaderRank = "Read-tier nav; operator links omitted.";

/** Sidebar / mobile: operator+ framing for Enterprise group */
export const enterpriseNavHintOperatorRank = "Governance and platform depth beyond Core Pilot.";

/**
 * `LayerHeader` rank-aware line under `enterpriseFootnote` on Enterprise Controls pages (same threshold as nav hints:
 * below Execute → reader framing).
 */
export const layerHeaderEnterpriseReaderRankLine = "Inspect first; writes need Execute+ (API).";

export const layerHeaderEnterpriseOperatorRankLine = "Execute+ writes (API-enforced).";

/** Deep execute tooling: only when resolved rank is below Execute (e.g. Reader deep-linked) */
export const enterpriseExecutePageHintReaderRank = "Writes need Execute+ here (API).";

/** Second line on governance resolution — readers vs operators (see `GovernanceResolutionRankCue`). */
export const governanceResolutionRankReaderLine = "Edits: policy packs or workflow (API).";

export const governanceResolutionRankOperatorLine = "Ordering lives in packs or workflow—not here.";

/** Governance resolution — one line under the page title (LayerHeader carries when-to-use). */
export const governanceResolutionPageSubline = "Effective stack; edits on Policy packs or Workflow.";

/** Governance resolution — “Change related controls” strip (LayerHeader + subline already frame read-only). */
export const governanceResolutionChangeRelatedControlsLead =
  "Refresh: GET only. Scope changes on Policy packs or Workflow.";

/**
 * Governance resolution — extra line under **Change related controls** when **`useEnterpriseMutationCapability()`** is
 * false (writes live elsewhere; **Refresh** stays a safe GET).
 */
export const governanceResolutionChangeRelatedControlsReaderSupplement =
  "Writes need Execute+; read-only evidence at this rank.";

/** Governance dashboard: readers can consume signals; in-product actions still need execute on the API. */
export const governanceDashboardReaderActionLine = "Read-only queue; row actions need Execute+ (API).";

/** Governance workflow — lead under page title when caller can mutate (Execute+ in shell). */
export const governanceWorkflowPageLeadOperator = "Load a run; work top to bottom by status.";

/** Governance workflow — lead under page title for read tier (inspect-first layout already elevates Load). */
export const governanceWorkflowPageLeadReader = "Load a run to inspect.";

/**
 * Governance workflow — inline review card when rank is below Execute (defense if UI state still shows the form;
 * Approve/Reject entry points are normally disabled for Reader).
 */
export const governanceWorkflowPendingReviewReaderNote =
  "Read-rank preview; submit is operator-gated (API).";

/**
 * Alert rules / routing / simulation / tuning / composite — rank-aware cue (`AlertOperatorToolingRankCue`) for tests
 * or routes that mount a second strip below **`LayerHeader`**.
 */
export const alertOperatorToolingReaderRankLine = "Inspect above · below: Execute+ config (API).";

export const alertOperatorToolingOperatorRankLine = "Writes below: API-enforced.";

/** Alerts inbox — lead under title (Execute+); rank cue hidden — see `LayerHeader`. */
export const alertsPageLeadOperator = "Filter · page · triage per card.";

/** Alerts inbox — lead under title (read tier); `AlertsInboxRankCue` carries write boundary. */
export const alertsPageLeadReader = "Filter and page.";

/** Alerts inbox — readers vs operators (see `AlertsInboxRankCue`). */
export const alertsInboxRankReaderLine = "Preview only here; Confirm needs Execute+ (API).";

export const alertsInboxRankOperatorLine = "Triage writes: API-enforced.";

/** Alerts triage confirmation dialog — extra copy when rank is below Execute (`alerts/page.tsx`). */
export const alertsTriageDialogReaderNote =
  "Confirm off at read rank; API enforces writes.";

/** Title on triage action buttons when rank can open the dialog but cannot Confirm (`alerts/page.tsx`). */
export const alertsTriageOpenPreviewReaderTitle =
  "Open triage preview; Confirm needs Execute+ on the API.";

/** Alerts inbox — triage button visible names when Confirm/write is off at this shell rank (preview-only path). */
export const alertsTriageAcknowledgeButtonLabelReaderInbox = "Acknowledge (preview)";

export const alertsTriageResolveButtonLabelReaderInbox = "Resolve (preview)";

export const alertsTriageSuppressButtonLabelReaderInbox = "Suppress (preview)";

/** Audit log — readers vs operators (see `AuditLogRankCue`). */
export const auditLogRankReaderLine = "CSV export: Auditor or Admin on the API (same From/To).";

export const auditLogRankOperatorLine = "Export is role-gated on the API.";

/** Extra line under the pending-approvals empty state when rank is below Execute (batch/review CTAs are disabled). */
export const governanceDashboardPendingClearReaderSupplement =
  "Batch and row actions stay disabled here until operator-level access applies (API).";

/** Governance workflow — “Approval requests for a run” card description by rank. */
export const governanceWorkflowQueryCardDescriptionReader =
  "Load to inspect; approve→activate are operator-only (API).";

export const governanceWorkflowQueryCardDescriptionOperator =
  "Load one run, then approve, reject, or promote by status.";

/** No rows returned for the loaded run — reader copy references submit section position when inspect-first layout is used. */
export const governanceWorkflowNoApprovalsReaderHint =
  "Nothing in Submitted/Draft for this run. Pick another run ID, or ask an operator to submit from the Submit section.";

export const governanceWorkflowNoApprovalsOperatorHint =
  "Submit a request above or pick another run ID.";

/** Governance workflow — Submit for approval when rank cannot mutate (shell soft-disable; API authoritative). */
export const governanceWorkflowSubmitForApprovalButtonLabelReaderRank = "Submit for approval (Execute+)";

/** Governance workflow — inline review Submit when rank cannot mutate. */
export const governanceWorkflowReviewSubmitButtonLabelReaderRank = "Submit (Execute+)";

/** Governance workflow — row actions when rank cannot mutate (buttons stay disabled; label clarifies floor). */
export const governanceWorkflowApproveButtonLabelReaderRank = "Approve (Execute+)";

export const governanceWorkflowRejectButtonLabelReaderRank = "Reject (Execute+)";

export const governanceWorkflowPromoteButtonLabelReaderRank = "Promote (Execute+)";

export const governanceWorkflowActivateButtonLabelReaderRank = "Activate (Execute+)";

/** Governance workflow — under “Promotions & activations” for Execute+ (timeline + actions). */
export const governanceWorkflowPromotionsActivationsSectionLeadOperator =
  "Promote approved rows, then activate for the target environment.";

/** Governance workflow — same section for read tier (Activate/Promote buttons stay disabled in the shell). */
export const governanceWorkflowPromotionsActivationsSectionLeadReader =
  "Read-only timeline; Promote/Activate need operator access (API).";

/** Policy packs — lead under title (Execute+); link to Governance resolution for stack semantics. */
export const policyPacksPageLeadOperator = "Inventory and JSON first; then compare and lifecycle.";

/** Policy packs — lead under title (read tier). */
export const policyPacksPageLeadReader = "Inspect above; lifecycle needs Execute+ (API).";

/** Policy packs — empty list under “Packs in scope”. */
export const policyPacksEmptyScopeReaderLine =
  "None in scope yet. Inspect when data exists; create and lifecycle need operator on the API.";

export const policyPacksEmptyScopeOperatorLine = "No packs yet.";

/** Policy packs — “Published versions” empty when a pack is selected but no rows returned. */
export const policyPacksPublishedVersionsEmptyReaderLine =
  "No published versions yet. Inspect here; publish needs operator on the API.";

export const policyPacksPublishedVersionsEmptyOperatorLine =
  "No published versions loaded for this pack yet.";

/** Policy packs — one line under Lifecycle heading for read tier (forms below stay soft-disabled). */
export const policyPacksLifecycleLeadReaderLine = "Lifecycle: Execute+ (API).";

/** Policy packs — primary lifecycle buttons when mutation capability is false (shell soft-disable; API authoritative). */
export const policyPacksCreatePackButtonLabelReaderRank = "Create pack (Execute+)";

export const policyPacksPublishButtonLabelReaderRank = "Publish (Execute+)";

export const policyPacksAssignButtonLabelReaderRank = "Assign (Execute+)";

/** Governance workflow — Submit card description for read tier (operator copy stays inline on the page with API path). */
export const governanceWorkflowSubmitCardDescriptionReader =
  "Submit disabled at your rank in this shell; API contract unchanged.";

/** Composite alert rules — empty “Current composite rules” list. */
export const compositeRulesDefinedListEmptyReaderLine =
  "No composite rules yet. Inspect definitions; writes need operator on the API.";

export const compositeRulesDefinedListEmptyOperatorLine = "None yet.";

/** Alert rules — empty “Defined rules” list. */
export const alertRulesDefinedListEmptyReaderLine =
  "No rules yet. Inspect thresholds; writes need operator on the API.";

export const alertRulesDefinedListEmptyOperatorLine = "None yet.";

/** Alert rules — list block above **Change configuration** (read tier: inspect-first label). */
export const alertRulesCurrentRulesHeadingOperator = "Current rules";
export const alertRulesCurrentRulesHeadingReader = "Current rules (inspect)";

/** Alert routing — subscriptions block above **Change configuration** (read tier: inspect-first label). */
export const alertRoutingCurrentRoutingHeadingOperator = "Current routing";
export const alertRoutingCurrentRoutingHeadingReader = "Current routing (inspect)";

/** Alert routing — empty “Current routing” list (mirrors alert rules empty pattern). */
export const alertRoutingSubscriptionsEmptyReaderLine =
  "No subscriptions yet. Inspect below; create, enable, and disable need operator on the API.";

export const alertRoutingSubscriptionsEmptyOperatorLine = "None yet.";

/** Governance workflow — promotions timeline empty (after a run is loaded). */
export const governanceWorkflowPromotionsEmptyReaderHint =
  "None yet. Rows appear after an operator promotes an approved request.";

export const governanceWorkflowPromotionsEmptyOperatorHint = "Promote an approved request to see rows here.";

/** Governance workflow — activations list empty. */
export const governanceWorkflowActivationsEmptyReaderHint =
  "None yet. Appear after an operator activates a promotion; inspect-only at your rank.";

export const governanceWorkflowActivationsEmptyOperatorHint =
  "Use Activate on a promotion card after promotions exist.";

/** Alerts inbox — filtered empty state (Reader: deemphasize triage/configure as primary path). */
export const alertsFilteredEmptyDescriptionReader =
  "No rows for this filter. Adjust or refresh; triage writes need operator on the API.";

export const alertsFilteredEmptyDescriptionOperator =
  "No rows. Change filter or refresh—empty means no match.";

/** Audit log — zero rows after a successful search. */
export const auditSearchNoResultsReaderLine = "No matches. CSV stays Auditor/Admin on the API.";

export const auditSearchNoResultsOperatorLine = "No audit events match your filters.";

/** Audit log — under “Search audit events” for read tier (LayerHeader already frames export roles). */
export const auditSearchSectionLeadReaderLine = "CSV: same From/To; Auditor/Admin on API.";

/** Audit log — short line above the CSV button (LayerHeader + search strip carry the rest). */
export const auditExportSectionSupportingLine = "Same From/To as search; Auditor or Admin on the API.";

/** Audit CSV — button label when From/To are incomplete (export disabled before role checks). */
export const auditExportCsvButtonLabelWindowIncomplete = "Export CSV (set From/To)";

/** Audit CSV — button label when window is valid but principal lacks Auditor/Admin for bulk export (API). */
export const auditExportCsvButtonLabelRoleRestricted = "Export CSV (Auditor/Admin)";

/** Policy packs — intro under “Compare versions” when caller can mutate (Execute+ in shell). */
export const policyPacksCompareVersionsIntroOperator =
  "Two versions → JSON path diff (add / remove / change).";

/** Policy packs — same block for read tier (diff only; lifecycle writes below). */
export const policyPacksCompareVersionsIntroReader =
  "Two versions → read-only diff; publish/assign in Lifecycle (Execute+).";

/** Policy packs — under “Compare versions” when rank cannot mutate (diff is still read-only inspection). */
export const policyPacksCompareVersionsReaderSubline =
  "Diff read-only; writes in Lifecycle (Execute+).";

/** Policy packs — title on “Show diff” when rank cannot mutate (diff stays inspection-only; lifecycle on API). */
export const policyPacksShowDiffButtonReaderTitle =
  "Read-only diff between versions; publish and assign need Execute+ in Lifecycle (API).";

/** Policy packs — pack selector when lifecycle writes are soft-disabled at read rank in the shell. */
export const policyPacksPackSelectReaderTitle =
  "Switch pack to inspect versions and JSON; publish, assign, and create need Execute+ below (API).";

/** Audit — Execute+ caller without Auditor/Admin claims (CSV export remains API-role-gated). */
export const auditExportExecuteRankAuditorRoleNote =
  "CSV still needs Auditor or Admin on the API—Execute rank alone is not enough for export.";

/** Alert rules — Create button label when mutation capability is false (same Execute+ floor as the hook). */
export const alertRulesCreateButtonLabelReaderRank = "Create rule (Execute+)";

/** Alert routing — Create subscription button label when mutation capability is false. */
export const alertRoutingCreateSubscriptionButtonLabelReaderRank =
  "Create alert routing subscription (Execute+)";

/** Alert routing — Enable toggle label at read rank (control disabled; API authoritative). */
export const alertRoutingToggleToEnabledReaderRank = "Enable (Execute+)";

/** Alert routing — Disable toggle label at read rank. */
export const alertRoutingToggleToDisabledReaderRank = "Disable (Execute+)";

/** Alerts triage dialog — appended to title when Confirm is disabled at read rank. */
export const alertsTriageDialogTitleReaderSuffix = " (read-only)";

/** Under-card shortcut hint for read tier (`useAlertCardShortcuts` skips Alt+1–3 unless Execute+ in shell). */
export const alertsPageShortcutsLineReader =
  "Alt+J/K navigate between cards; Alt+1–3 triage shortcuts register at Execute+ in this shell.";

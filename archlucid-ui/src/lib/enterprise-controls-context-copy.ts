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
export const alertToolingConfigureSectionSubline = "Configure alert rules and routing — write actions require elevated permissions.";

/** Alert tooling — configure block **`h3`** (`alert-rules`, `alert-routing`, `composite-alert-rules`; tuning uses operator string only). */
export const alertToolingChangeConfigurationHeadingOperator = "Change configuration";

export const alertToolingChangeConfigurationHeadingReader = "Change configuration (elevated permissions)";

/** `title` on mutation controls the UI soft-disables for Reader-tier principals (API remains authoritative). */
export const enterpriseMutationControlDisabledTitle =
  "Requires elevated permissions in this workspace; the API still enforces every write.";

/**
 * Audit CSV export uses **`RequireAuditor`** on the API (Auditor **or** Admin)—stricter than Execute-tier pack
 * mutations; align the Export button with **`/me` role claims**, not `useEnterpriseMutationCapability`.
 */
export const auditExportControlDisabledTitle =
  "CSV export requires Auditor or Admin on the API for this tenant; search above still works for your role.";

/** Sidebar / mobile: optional micro-copy when Reader-ranked shell shows fewer nav items (empty = omit line). */
export const enterpriseNavHintReaderRank = "";

/** Sidebar / mobile: operator+ framing for Enterprise group (`operate-governance`; aligns with PRODUCT_PACKAGING §Layer B — not the first-session Pilot wedge). */
export const enterpriseNavHintOperatorRank =
  "Governance controls: policies, approvals, alerts, audit (after Pilot proves value).";

/**
 * `LayerHeader` rank-aware line under `enterpriseFootnote` on Enterprise Controls pages (same threshold as nav hints:
 * below Execute → reader framing).
 */
export const layerHeaderEnterpriseReaderRankLine = "Governance controls — inspect view.";

export const layerHeaderEnterpriseOperatorRankLine = "Governance controls — write actions are permission-gated.";

/** Deep execute tooling: only when resolved rank is below Execute (e.g. Reader deep-linked) */
export const enterpriseExecutePageHintReaderRank = "Writes need Execute+ here (API).";

/** Second line on governance resolution — readers vs operators (see `GovernanceResolutionRankCue`). */
export const governanceResolutionRankReaderLine = "Edits: policy packs or workflow (API).";

export const governanceResolutionRankOperatorLine = "Ordering lives in packs or workflow—not here.";

/** Governance resolution — lead under page title (`governance-resolution/page.tsx`), before rank cue. */
export const governanceResolutionPageLeadOperator =
  "Read effective policy and conflicts; change packs or workflow when bindings must move (API).";

export const governanceResolutionPageLeadReader =
  "Inspect effective policy and decisions here; pack and workflow writes use Execute+ surfaces (API).";

/** Governance resolution — **Refresh** is GET-only (always enabled at any shell rank). */
export const governanceResolutionRefreshButtonTitle = "Reload effective governance resolution (GET only).";

/** Governance resolution — primary inspect sections (`governance-resolution/page.tsx`). */
export const governanceResolutionEffectivePolicyHeadingOperator = "Effective policy";

export const governanceResolutionEffectivePolicyHeadingReader = "Effective policy (inspect)";

export const governanceResolutionResolutionDetailsHeadingOperator = "Resolution details";

export const governanceResolutionResolutionDetailsHeadingReader = "Resolution details (inspect)";

/** Governance resolution — “Change related controls” strip (LayerHeader + rank cue already frame read vs write). */
export const governanceResolutionChangeRelatedControlsLead =
  "Refresh is GET. Scope changes: Packs or Workflow.";

/**
 * Governance resolution — extra line under **Change related controls** when **`useEnterpriseMutationCapability()`** is
 * false (writes live elsewhere; **Refresh** stays a safe GET).
 */
export const governanceResolutionChangeRelatedControlsReaderSupplement =
  "Writes need Execute+ at this rank (API).";

/** Governance dashboard: readers can consume signals; in-product actions still need execute on the API. */
export const governanceDashboardReaderActionLine = "Read-only queue until Execute+ (API).";

/** Governance dashboard — batch bar when rank cannot mutate (`governance/dashboard/page.tsx`). */
export const governanceDashboardApproveSelectedButtonLabelReaderRank = "Approve selected (Execute+)";

export const governanceDashboardRejectSelectedButtonLabelReaderRank = "Reject selected (Execute+)";

/** Governance dashboard — pending queue heading (`governance/dashboard/page.tsx`). */
export const governanceDashboardPendingApprovalsHeadingOperator = "Pending approvals";

export const governanceDashboardPendingApprovalsHeadingReader = "Pending approvals (inspect)";

/** Governance dashboard — signal sections above the pending queue (`governance/dashboard/page.tsx`). */
export const governanceDashboardRecentDecisionsHeadingOperator = "Recent decisions";

export const governanceDashboardRecentDecisionsHeadingReader = "Recent decisions (inspect)";

export const governanceDashboardComplianceDriftHeadingOperator = "Compliance drift trend (last 30 days)";

export const governanceDashboardComplianceDriftHeadingReader =
  "Compliance drift trend (last 30 days) (inspect)";

export const governanceDashboardChangeLogHeadingOperator = "Policy pack change log";

export const governanceDashboardChangeLogHeadingReader = "Policy pack change log (inspect)";

/** Governance dashboard — **Lineage** link on pending cards (GET detail). */
export const governanceDashboardLineageLinkTitle = "Read-only approval and review lineage (GET).";

/** Governance dashboard — **Review** opens workflow for the review (`governance/dashboard/page.tsx`). */
export const governanceDashboardOpenWorkflowReviewTitleOperator =
  "Open governance workflow for this review to continue promotion steps.";

export const governanceDashboardOpenWorkflowReviewTitleReader =
  "Open workflow for inspection; Approve or Reject in this shell need Execute+ (API).";

/** Governance workflow — lead under page title when caller can mutate (Execute+ in shell). */
export const governanceWorkflowPageLeadOperator =
  "Submit finalized architecture outputs for governance review and promotion. Load a review to see approvals, promotions, and environment activations.";

/** Governance workflow — lead under page title for read tier (inspect-first layout already elevates Load). */
export const governanceWorkflowPageLeadReader =
  "Inspect how an architecture review moved through approval. Load a review below to view its approval history.";

/** Governance workflow — submit card title (`governance/page.tsx`). */
export const governanceWorkflowSubmitCardTitleOperator = "Submit for governance approval";

export const governanceWorkflowSubmitCardTitleReader = "Submit for governance approval";

/** Governance workflow — load and list card title. */
export const governanceWorkflowApprovalRequestsCardTitleOperator = "Approval requests for this review";

export const governanceWorkflowApprovalRequestsCardTitleReader = "Approval requests for this review";

/** Governance workflow — promotions + activations section (`governance/page.tsx`). */
export const governanceWorkflowPromotionsActivationsHeadingOperator = "Governance activity";

export const governanceWorkflowPromotionsActivationsHeadingReader = "Governance activity";

/** Governance workflow — activations list under promotions. */
export const governanceWorkflowActivationsSubheadingOperator = "Environment activations";

export const governanceWorkflowActivationsSubheadingReader = "Environment activations";

/** Governance workflow — reload lists for the active review (`GET`); shown next to **Load** after a review is selected. */
export const governanceWorkflowRefreshRunDataTitle =
  "Reload approval requests, promotions, and activations for the loaded review.";

export const governanceWorkflowRefreshRunDataButtonLabel = "Refresh data";

/** Alerts triage dialog — primary control when **Confirm** is disabled at read rank (preview-only path). */
export const alertsTriageDialogConfirmButtonLabelReaderRank = "Apply triage (Execute+)";

/** Audit log — search section heading (`audit/page.tsx`); branch with **`callerAuthorityRank`**. */
export const auditSearchEventsSectionHeadingOperator = "Search audit events";

export const auditSearchEventsSectionHeadingReader = "Search audit events (inspect)";

/** Audit log — search is always **GET**; label nudges read-tier callers away from export expectations. */
export const auditSearchEventsButtonLabelReaderRank = "Search audit log";

/** Audit log — primary **Search** control `title` (`audit/page.tsx`). */
export const auditSearchEventsButtonTitleOperator = "Run search with the current filter fields (GET).";

export const auditSearchEventsButtonTitleReader =
  "Run search (GET). CSV export remains Auditor/Admin-gated on the API.";

/** Audit log — **Audit results** section heading; branch with **`callerAuthorityRank`**. */
export const auditResultsSectionHeadingOperator = "Audit results";

export const auditResultsSectionHeadingReader = "Audit results (inspect)";

/** Audit log — **Load more** pagination (`GET`). */
export const auditLoadMoreButtonTitleOperator = "Load the next page of audit events for the current filters (GET).";

export const auditLoadMoreButtonTitleReader =
  "Load older rows (GET). Export rules unchanged on the API.";

/** Audit log — **Clear filters** when rank cannot mutate in the shell (still GET-only; clarifies re-run vs export). */
export const auditClearFiltersButtonLabelReaderRank = "Clear filters & search";

/** Alert routing — delivery history fetch is **GET**; reader label clarifies inspect vs toggle writes. */
export const alertRoutingDeliveryAttemptsButtonLabelReaderRank = "Delivery attempts (inspect)";

/** Alert routing — **`title`** on **Show delivery attempts** (`alert-routing/page.tsx`). */
export const alertRoutingDeliveryAttemptsButtonTitleOperator =
  "Load recent delivery attempts for this subscription (GET).";

export const alertRoutingDeliveryAttemptsButtonTitleReader =
  "Load delivery attempts (GET). Enable/Disable subscription needs Execute+ on the API.";

/** Policy packs — compare action stays inspection-only at read rank (lifecycle writes below). */
export const policyPacksShowDiffButtonLabelReaderRank = "Show diff (inspect)";

/**
 * Governance workflow — inline review card when rank is below Execute (defense if UI state still shows the form;
 * Approve/Reject entry points are normally disabled for Reader).
 */
export const governanceWorkflowPendingReviewReaderNote =
  "Review actions need operator-level access on the server — this form is preview only at your current role.";

/**
 * Alert rules / routing / simulation / tuning / composite — rank-aware cue (`AlertOperatorToolingRankCue`) for tests
 * or routes that mount a second strip below **`LayerHeader`**.
 */
export const alertOperatorToolingReaderRankLine = "Inspect above · below: Execute+ config (API).";

export const alertOperatorToolingOperatorRankLine = "Writes below: API-enforced.";

/**
 * Alert rules / alert routing / composite — list **Refresh** (`GET` only); configure sections remain Execute+ on the
 * API (`alert-rules/page.tsx`, `alert-routing/page.tsx`, `composite-alert-rules/page.tsx`).
 */
export const alertToolingListRefreshButtonTitleOperator = "Reload the list from the API (GET).";

export const alertToolingListRefreshButtonTitleReader =
  "Reload list (GET). Creates, toggles, and edits below need Execute+ on the API.";

/**
 * Alert tuning — lead under page title (`alert-tuning/page.tsx`). **POST** recommendation is **read access** on the
 * API; persisting thresholds to production remains **Execute+** on Alert / composite rule routes.
 */
export const alertTuningPageLead =
  "Scoring ranks candidate thresholds (Read on the API). Applying a winning threshold to production uses Alert rules or composite rules (Execute+).";

/**
 * Alert simulation — lead under page title (`alert-simulation/page.tsx`). Simulation **POST**s use **read access**;
 * live subscriptions and persisted rules are changed elsewhere (**Execute+**).
 */
export const alertSimulationPageLead =
  "What-if tabs call simulation APIs (Read on the API). Enabling subscriptions or editing live rules stays on Alert routing or Alert rules (Execute+).";

/** Alert tuning — primary **Recommend threshold** control (`alert-tuning/page.tsx`). */
export const alertTuningRecommendButtonTitle =
  "Run threshold recommendation (Read access on the API; does not change live rules).";

/** Alert tuning — results section **`h3`** (`alert-tuning/page.tsx`); recommend stays available at Read on the API. */
export const alertTuningCurrentTuningHeadingOperator = "Current tuning";

export const alertTuningCurrentTuningHeadingReader = "Current tuning (inspect)";

/** Alert simulation — **Simulate** / **Compare candidates** controls (`alert-simulation/page.tsx`). */
export const alertSimulationRunControlTitle =
  "Run what-if (Read access on the API; no live rule or subscription changes from this page).";

/** Alert simulation — outcome column **`h3`** (`alert-simulation/page.tsx`); inputs stay neutral (read access POSTs). */
export const alertSimulationCurrentBehaviorHeadingOperator = "Current behavior";

export const alertSimulationCurrentBehaviorHeadingReader = "Current behavior (inspect)";

/** Alerts inbox — lead under title (Execute+); rank cue hidden — see `LayerHeader`. */
export const alertsPageLeadOperator = "Filter, page, then triage per card.";

/** Alerts inbox — lead under title (read tier); `AlertsInboxRankCue` carries write boundary. */
export const alertsPageLeadReader = "Filter and page.";

/** Alerts inbox — **Refresh** reloads the paged list (`GET`); triage remains Execute+ on the API. */
export const alertsInboxRefreshButtonTitleOperator = "Reload alerts for the current status filter (GET).";

export const alertsInboxRefreshButtonTitleReader =
  "Reload alerts (GET). Triage writes need Execute+ on the API.";

/** Alerts inbox — pagination controls when triage writes are off (`alerts/page.tsx`). */
export const alertsPaginationNavTitleReaderRank = "Page results (read-only in this shell; API authoritative).";

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

/** Governance workflow — “Approval requests for a review” card description by rank. */
export const governanceWorkflowQueryCardDescriptionReader =
  "Load a review to see its approval requests. Approving, promoting, and activating require approver rights on your account.";

export const governanceWorkflowQueryCardDescriptionOperator =
  "Pick a review, then load its approval requests. Approve or reject submitted requests, promote approved manifests, and activate in the target environment when ready.";

/** No rows returned for the loaded review — reader copy references submit section position when inspect-first layout is used. */
export const governanceWorkflowNoApprovalsReaderHint =
  "No open approval rows for this review. Try another review, or ask an operator to submit a request.";

export const governanceWorkflowNoApprovalsOperatorHint =
  "Submit a request above or choose a different review.";

/** Governance workflow — Submit for approval when rank cannot mutate (shell soft-disable; API authoritative). */
export const governanceWorkflowSubmitForApprovalButtonLabelReaderRank = "Submit for governance approval";

/** Governance workflow — inline review Submit when rank cannot mutate. */
export const governanceWorkflowReviewSubmitButtonLabelReaderRank = "Submit review (needs approver rights)";

/** Governance workflow — row actions when rank cannot mutate (buttons stay disabled; label clarifies floor). */
export const governanceWorkflowApproveButtonLabelReaderRank = "Approve (needs approver rights)";

export const governanceWorkflowRejectButtonLabelReaderRank = "Reject (needs approver rights)";

export const governanceWorkflowPromoteButtonLabelReaderRank = "Promote (needs approver rights)";

export const governanceWorkflowActivateButtonLabelReaderRank = "Activate (needs approver rights)";

/** Governance workflow — under “Promotions & activations” for Execute+ (timeline + actions). */
export const governanceWorkflowPromotionsActivationsSectionLeadOperator =
  "Promote approved rows, then activate for the target environment.";

/** Governance workflow — same section for read tier (Activate/Promote buttons stay disabled in the shell). */
export const governanceWorkflowPromotionsActivationsSectionLeadReader =
  "Read-only timeline; Promote and Activate require approver rights (API).";

/** Policy packs — lead under title (Execute+); link to Governance resolution for stack semantics. */
export const policyPacksPageLeadOperator =
  "Review inventory and effective policy first; publish or assign when your role allows.";

/** Policy packs — lead under title (read tier). */
export const policyPacksPageLeadReader =
  "Inspect registered packs and resolved policy for this scope (read-only where your role limits changes).";

/** Policy packs — **Current policy packs** section heading (`policy-packs/page.tsx`). */
export const policyPacksCurrentPacksHeadingOperator = "Current policy packs";

export const policyPacksCurrentPacksHeadingReader = "Current policy packs (inspect)";

/** Policy packs — effective / resolved JSON section heading. */
export const policyPacksPackContentHeadingOperator = "Pack content";

export const policyPacksPackContentHeadingReader = "Pack content (inspect)";

/** Policy packs — reader assist next to **Refresh** (`policy-packs/page.tsx`); reload is GET-only. */
export const policyPacksRefreshAssistReaderLine =
  "Refresh reloads inventory and effective policy (GET only; no lifecycle writes).";

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
  "Submitting requests requires approver rights in this workspace. You can still review the workflow below.";

/** Composite alert rules — empty “Current composite rules” list. */
export const compositeRulesDefinedListEmptyReaderLine =
  "No composite rules yet. Inspect definitions; writes require elevated permissions on the API.";

export const compositeRulesDefinedListEmptyOperatorLine = "None yet.";

/** Composite alert rules — lead under page title (`composite-alert-rules/page.tsx`). */
export const compositeRulesPageLeadOperator =
  "Review compound conditions in the list, then author a new composite rule below.";

export const compositeRulesPageLeadReader =
  "Inspect definitions above; new composite rules need Execute+ on the API at this rank.";

/** Composite — current list heading (mirrors simple alert rules pattern). */
export const compositeRulesCurrentRulesHeadingOperator = "Current composite rules";

export const compositeRulesCurrentRulesHeadingReader = "Current composite rules (inspect)";

/** Composite — reader assist next to **Refresh** (GET list only). */
export const compositeRulesRefreshAssistReaderLine =
  "Refresh reloads the rule list (GET only; does not create or change rules).";

/** Composite — primary create button when mutation capability is false. */
export const compositeRulesCreateButtonLabelReaderRank = "Create composite rule (Execute+)";

/** Alert rules — empty “Defined rules” list. */
export const alertRulesDefinedListEmptyReaderLine =
  "No rules yet. Inspect thresholds; writes need operator on the API.";

export const alertRulesDefinedListEmptyOperatorLine = "None yet.";

/** Alert rules — lead under page title (`alert-rules/page.tsx`); rank cue stays in `AlertOperatorToolingRankCue`. */
export const alertRulesPageLeadOperator = "Scan current thresholds, then add or adjust rules below.";

export const alertRulesPageLeadReader =
  "Inspect thresholds above; the Change configuration block is Execute+ on the API at this rank.";

/** Alert routing — lead under page title (`alert-routing/page.tsx`). */
export const alertRoutingPageLeadOperator = "Review destinations and delivery health; subscriptions below.";

export const alertRoutingPageLeadReader =
  "Inspect subscriptions first; create, enable, and disable need Execute+ on the API at this rank.";

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
  "Nothing matches your filters yet, or no alerts have been raised for this workspace.";

export const alertsFilteredEmptyDescriptionOperator =
  "Nothing matches this filter yet — or rules have not fired. Adjust filters or keep building coverage below.";

/** Audit log — zero rows after a successful search. */
export const auditSearchNoResultsReaderLine = "No audit events match your search.";

export const auditSearchNoResultsOperatorLine = "No audit events match your filters.";

/** Audit log — under “Search audit events” for read tier (LayerHeader already frames export roles). */
export const auditSearchSectionLeadReaderLine = "Export requires Auditor or Admin access.";

/** Audit log — short line above the CSV button (LayerHeader + search strip carry the rest). */
export const auditExportSectionSupportingLine = "CSV export requires Auditor or Admin access.";

/** Audit CSV — button label when From/To are incomplete (export disabled before role checks). */
export const auditExportCsvButtonLabelWindowIncomplete = "Export CSV (set From/To)";

/** Audit CSV — button label when window is valid but principal lacks Auditor/Admin for bulk export (API). */
export const auditExportCsvButtonLabelRoleRestricted = "Export CSV (Auditor/Admin)";

/** Policy packs — intro under “Compare versions” when caller can mutate (Execute+ in shell). */
export const policyPacksCompareVersionsIntroOperator =
  "Pick two versions for a JSON path diff.";

/** Policy packs — same block for read tier (diff only; lifecycle writes below). */
export const policyPacksCompareVersionsIntroReader =
  "Read-only diff; publish/assign stay under Lifecycle (Execute+).";

/** Policy packs — under “Compare versions” when rank cannot mutate (diff is still read-only inspection). */
export const policyPacksCompareVersionsReaderSubline =
  "Diff is inspect-only; writes in Lifecycle.";

/** Policy packs — title on “Show diff” when rank cannot mutate (diff stays inspection-only; lifecycle on API). */
export const policyPacksShowDiffButtonReaderTitle =
  "Read-only diff between versions; publish and assign need Execute+ in Lifecycle (API).";

/** Policy packs — **Hide diff** (`policy-packs/page.tsx`); collapses client-side diff only. */
export const policyPacksHideDiffButtonTitle = "Close diff view (client only; no API write).";

/** Architecture digests — history sidebar **`h3`** (`digests/page.tsx`). */
export const digestsHistoryHeadingOperator = "History";

export const digestsHistoryHeadingReader = "History (inspect)";

/** Architecture digests — list **Refresh** (`GET`). */
export const digestsListRefreshButtonTitleOperator = "Reload digest list from the API (GET).";

export const digestsListRefreshButtonTitleReader =
  "Reload digest list (GET). Email subscriptions are configured under Subscriptions (Execute+).";

/** Digest subscriptions — subscription list **`h3`** (`components/digests/DigestSubscriptionsContent.tsx`). */
export const digestSubscriptionsYourSubscriptionsHeadingOperator = "Your subscriptions";

export const digestSubscriptionsYourSubscriptionsHeadingReader = "Your subscriptions (inspect)";

/** Digest subscriptions — primary create when rank cannot mutate in the shell. */
export const digestSubscriptionsCreateSubscriptionButtonLabelReaderRank = "Create subscription (Execute+)";

export const digestSubscriptionsToggleToDisabledReaderRank = "Disable (Execute+)";

export const digestSubscriptionsToggleToEnabledReaderRank = "Enable (Execute+)";

/** Digest subscriptions — delivery attempts (`GET`). */
export const digestSubscriptionsDeliveryAttemptsButtonLabelReaderRank = "Delivery attempts (inspect)";

export const digestSubscriptionsDeliveryAttemptsButtonTitleOperator =
  "Load recent digest delivery attempts for this subscription (GET).";

export const digestSubscriptionsDeliveryAttemptsButtonTitleReader =
  "Load delivery attempts (GET). Create and toggle need Execute+ on the API.";

/** Digest subscriptions — empty list. */
export const digestSubscriptionsEmptyListOperatorLine = "None yet.";

export const digestSubscriptionsEmptyListReaderLine =
  "None yet. Inspect when rows exist; create and toggle need operator on the API.";

/** Advisory schedules — schedules list **`h3`** (`components/advisory/AdvisorySchedulesContent.tsx`). */
export const advisorySchedulesListHeadingOperator = "Schedules";

export const advisorySchedulesListHeadingReader = "Schedules (inspect)";

/** Advisory schedules — create block **`h3`**. */
export const advisorySchedulesCreateSectionHeadingOperator = "Create schedule";

export const advisorySchedulesCreateSectionHeadingReader = "Create schedule (Execute+)";

/** Advisory schedules — **Create schedule** submit when rank cannot mutate. */
export const advisorySchedulesCreateScheduleButtonLabelReaderRank = "Create schedule (Execute+)";

/** Advisory schedules — **Run now** when rank cannot mutate. */
export const advisorySchedulesRunNowButtonLabelReaderRank = "Run now (Execute+)";

/** Advisory schedules — **Load executions** (`GET`). */
export const advisorySchedulesLoadExecutionsButtonLabelReaderRank = "Load executions (inspect)";

export const advisorySchedulesLoadExecutionsButtonTitleOperator =
  "Load recent advisory scan executions for this schedule (GET).";

export const advisorySchedulesLoadExecutionsButtonTitleReader =
  "Load executions (GET). Run now and create schedule need Execute+ on the API.";

/** Advisory schedules — empty list (`components/advisory/AdvisorySchedulesContent.tsx`). */
export const advisorySchedulesEmptyListOperatorLine = "No schedules yet.";

export const advisorySchedulesEmptyListReaderLine =
  "No schedules yet. Inspect when data exists; create and Run now need operator on the API.";

/** Policy packs — pack selector when lifecycle writes are soft-disabled at read rank in the shell. */
export const policyPacksPackSelectReaderTitle =
  "Switch pack to inspect versions and JSON; publish, assign, and create need Execute+ below (API).";

/** Audit — Execute+ caller without Auditor/Admin claims (CSV export remains API-role-gated). */
export const auditExportExecuteRankAuditorRoleNote =
  "CSV export requires Auditor or Admin access — your current role does not include export.";

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
  "Alt+J/K between cards; Alt+1–3 only at Execute+ here.";

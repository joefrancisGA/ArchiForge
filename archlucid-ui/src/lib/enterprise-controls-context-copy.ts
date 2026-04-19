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
export const alertToolingConfigureSectionSubline = "Operator configuration below (API).";

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
export const layerHeaderEnterpriseReaderRankLine = "Read tier: inspect; writes need Execute+ (API).";

export const layerHeaderEnterpriseOperatorRankLine = "Execute+ writes (API-enforced).";

/** Deep execute tooling: only when resolved rank is below Execute (e.g. Reader deep-linked) */
export const enterpriseExecutePageHintReaderRank = "Writes need Execute+ in this shell (API-enforced).";

/** Second line on governance resolution — readers vs operators (see `GovernanceResolutionRankCue`). */
export const governanceResolutionRankReaderLine = "Edits: policy packs or workflow (API).";

export const governanceResolutionRankOperatorLine = "Ordering lives in packs or workflow—not here.";

/** Governance resolution — one line under the page title (replaces a second rank cue; LayerHeader carries when-to-use). */
export const governanceResolutionPageSubline =
  "This route is read-only — change packs or workflow on those pages (API).";

/** Governance dashboard: readers can consume signals; in-product actions still need execute on the API. */
export const governanceDashboardReaderActionLine =
  "Queue is read-only here; row actions need Execute+ (API).";

/** Governance workflow — lead under page title when caller can mutate (Execute+ in shell). */
export const governanceWorkflowPageLeadOperator =
  "One run: load ID, then submit → approve/reject → promote → activate.";

/** Governance workflow — lead under page title for read tier (inspect-first layout already elevates Load). */
export const governanceWorkflowPageLeadReader = "Load a run ID to inspect rows.";

/**
 * Governance workflow — inline review card when rank is below Execute (defense if UI state still shows the form;
 * Approve/Reject entry points are normally disabled for Reader).
 */
export const governanceWorkflowPendingReviewReaderNote =
  "Read-only preview at this rank in the shell; submit still requires operator-level access (API gate).";

/**
 * Alert rules / routing / simulation / tuning / composite — rank-aware cue (`AlertOperatorToolingRankCue`) for tests
 * or routes that mount a second strip below **`LayerHeader`**.
 */
export const alertOperatorToolingReaderRankLine =
  "Inspect list and history above; configuration below needs Execute+ (API).";

export const alertOperatorToolingOperatorRankLine = "Writes below are API-enforced.";

/** Alerts inbox — lead under title (Execute+); card shortcuts repeat Alt+1–3 below. */
export const alertsPageLeadOperator = "Filter and page, then triage on each card.";

/** Alerts inbox — lead under title (read tier); `AlertsInboxRankCue` carries write boundary. */
export const alertsPageLeadReader = "Filter and page the inbox.";

/** Alerts inbox — readers vs operators (see `AlertsInboxRankCue`). */
export const alertsInboxRankReaderLine = "Triage preview here; Confirm and writes need Execute+ (API).";

export const alertsInboxRankOperatorLine = "Triage writes are API-enforced.";

/** Alerts triage confirmation dialog — extra copy when rank is below Execute (`alerts/page.tsx`). */
export const alertsTriageDialogReaderNote =
  "Confirm stays disabled at your rank in this shell; the API enforces every write.";

/** Title on triage action buttons when rank can open the dialog but cannot Confirm (`alerts/page.tsx`). */
export const alertsTriageOpenPreviewReaderTitle =
  "Open triage preview; Confirm needs Execute+ on the API.";

/** Audit log — readers vs operators (see `AuditLogRankCue`). */
export const auditLogRankReaderLine = "CSV export: Auditor or Admin on the API (same From/To).";

export const auditLogRankOperatorLine = "Export is role-gated on the API.";

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

/** Policy packs — lead under title (Execute+); page links Governance resolution for ordering. */
export const policyPacksPageLeadOperator = "Inventory, effective JSON, compare, then lifecycle.";

/** Policy packs — lead under title (read tier). */
export const policyPacksPageLeadReader = "Inspect inventory and JSON; lifecycle is Execute+ (API).";

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
  "Lifecycle below: Execute+ only at your rank (API-enforced).";

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

/** Audit log — under “Search audit events” for read tier (LayerHeader already frames export roles). */
export const auditSearchSectionLeadReaderLine = "Export reuses the same From/To (Auditor/Admin on API).";

/** Policy packs — under “Compare versions” when rank cannot mutate (diff is still read-only inspection). */
export const policyPacksCompareVersionsReaderSubline =
  "Version diff is read-only; publish, assign, and create are in Lifecycle (Execute+).";

/** Policy packs — title on “Show diff” when rank cannot mutate (diff stays inspection-only; lifecycle on API). */
export const policyPacksShowDiffButtonReaderTitle =
  "Read-only diff between versions; publish and assign need Execute+ in Lifecycle (API).";

/** Audit — Execute+ caller without Auditor/Admin claims (CSV export remains API-role-gated). */
export const auditExportExecuteRankAuditorRoleNote =
  "CSV export still needs Auditor or Admin on the API — Execute rank alone is not enough for bulk export.";

/** Alert rules — Create button label when mutation capability is false (same Execute+ floor as the hook). */
export const alertRulesCreateButtonLabelReaderRank = "Create rule (Execute+)";

/** Alert routing — Create subscription button label when mutation capability is false. */
export const alertRoutingCreateSubscriptionButtonLabelReaderRank =
  "Create alert routing subscription (Execute+)";

/** Alerts triage dialog — appended to title when Confirm is disabled at read rank. */
export const alertsTriageDialogTitleReaderSuffix = " (read-only)";

/** Under-card shortcut hint for read tier (Alt+1–3 register for preview only). */
export const alertsPageShortcutsLineReader =
  "Alt+J/K navigate · Alt+1–3 triage preview (Confirm disabled at your rank)";

/** Alert rules — under “Change configuration” for read tier. */
export const alertRulesChangeConfigurationLeadReaderLine =
  "Create and thresholds below: Execute+ at your rank (API).";

/** Alert routing — under “Change configuration” for read tier. */
export const alertRoutingChangeConfigurationLeadReaderLine =
  "New routes and toggles below: Execute+ at your rank (API).";

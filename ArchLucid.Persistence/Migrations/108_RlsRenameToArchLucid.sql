/*
  108: Rename RLS objects from legacy "Archiforge" / "archiforge_*" / "af_*" to ArchLucid.

  Closes the historical SQL-object leftover from the ArchLucid product rename
  (docs/ARCHLUCID_RENAME_CHECKLIST.md §7.9 — committed as "still expected by design"
  pre-rename initiative close). Decision recorded in PENDING_QUESTIONS.md item 32 Part B
  (Resolved 2026-04-21): atomic cutover to al_* SESSION_CONTEXT keys + ArchLucid* SQL object
  names, single deploy, no dual-read backwards-compatibility shim. RlsSessionContextApplicator
  is updated in the same change so the new policy reads the new keys at the same moment.

  Strategy
  --------
  Inline TVFs `rls.archiforge_scope_predicate` and `rls.archiforge_tenant_predicate` are
  referenced by `rls.ArchiforgeTenantScope`; SQL Server forbids `sp_rename` on schema-bound
  functions while a security policy points at them. We therefore:

    1. Capture the policy's current STATE (ON/OFF).
    2. DROP the security policy (releases the function refs).
    3. DROP the legacy predicate functions.
    4. CREATE the renamed functions reading `al_rls_bypass`, `al_tenant_id`,
       `al_workspace_id`, `al_project_id`.
    5. CREATE `rls.ArchLucidTenantScope` enumerating every FILTER + BLOCK predicate added
       by migrations 030/036/040/046/050/068/070/078/083/096/097/102/104 — by the time
       108 runs, every owning migration has applied so all referenced tables exist.
    6. Restore the captured STATE so a production tenant that had RLS enabled does not
       silently lose enforcement after the rename.

  Idempotency: if 108 has partially applied (e.g. failed mid-flight after the DROP),
  re-run will see no legacy policy/funcs but the new policy already in place; we therefore
  drop both legacy and new objects at the start before re-creating. DbUp will only re-run
  if the script row was rolled back from `dbo.SchemaVersions`, but the script tolerates it.

  RLS predicates / SESSION_CONTEXT keys are not renamed in earlier historical migrations
  (030, 036, 046, 068, 070, 078, 083, 096, 097, 102, 104) per the project rule "never modify
  historical SQL migration files"; this migration supersedes them in steady state.
*/

SET XACT_ABORT ON;
GO

DECLARE @PreviousState BIT = 0;

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
    SELECT @PreviousState = is_enabled
    FROM sys.security_policies
    WHERE name = N'ArchiforgeTenantScope';

IF @PreviousState = 0 AND EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
    SELECT @PreviousState = is_enabled
    FROM sys.security_policies
    WHERE name = N'ArchLucidTenantScope';

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
    DROP SECURITY POLICY rls.ArchiforgeTenantScope;

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
    DROP SECURITY POLICY rls.ArchLucidTenantScope;

IF OBJECT_ID(N'rls.archiforge_scope_predicate', N'IF') IS NOT NULL
    DROP FUNCTION rls.archiforge_scope_predicate;

IF OBJECT_ID(N'rls.archiforge_tenant_predicate', N'IF') IS NOT NULL
    DROP FUNCTION rls.archiforge_tenant_predicate;

IF OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
    DROP FUNCTION rls.archlucid_scope_predicate;

IF OBJECT_ID(N'rls.archlucid_tenant_predicate', N'IF') IS NOT NULL
    DROP FUNCTION rls.archlucid_tenant_predicate;

/* Ferry the previous STATE across the GO-separated CREATE batches via SESSION_CONTEXT —
   sp_executesql + IF/ELSE outside dynamic SQL would force the CREATE FUNCTION / CREATE
   SECURITY POLICY into nested EXEC(...) which loses readability for ~170 predicates. */
EXEC sp_set_session_context @key = N'__archlucid_108_previous_state', @value = @PreviousState, @read_only = 0;
GO

CREATE FUNCTION rls.archlucid_scope_predicate(
    @TenantId uniqueidentifier,
    @WorkspaceId uniqueidentifier,
    @ProjectScopeId uniqueidentifier)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
(
    SELECT 1 AS access_granted
    WHERE ISNULL(TRY_CONVERT(int, SESSION_CONTEXT(N'al_rls_bypass')), 0) = 1
       OR (
            @TenantId = TRY_CONVERT(uniqueidentifier, SESSION_CONTEXT(N'al_tenant_id'))
        AND @WorkspaceId = TRY_CONVERT(uniqueidentifier, SESSION_CONTEXT(N'al_workspace_id'))
        AND @ProjectScopeId = TRY_CONVERT(uniqueidentifier, SESSION_CONTEXT(N'al_project_id'))
       )
);
GO

CREATE FUNCTION rls.archlucid_tenant_predicate(@TenantId uniqueidentifier)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
(
    SELECT 1 AS access_granted
    WHERE ISNULL(TRY_CONVERT(int, SESSION_CONTEXT(N'al_rls_bypass')), 0) = 1
       OR @TenantId = TRY_CONVERT(uniqueidentifier, SESSION_CONTEXT(N'al_tenant_id'))
);
GO

/*
  Recreate the consolidated security policy with FILTER + BLOCK predicates on every covered table.
  Three-key tables are listed first in the same order as the legacy CREATE in 036 + later additions;
  tenant-only tables (096 / 097) follow at the bottom. All target tables are guaranteed to exist
  by migration 108 because their owning migrations precede us in DbUp's sequential journal.
*/
CREATE SECURITY POLICY rls.ArchLucidTenantScope
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.Runs,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DecisioningTraces,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifests,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshots,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingsSnapshots,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestAssumptions,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundles,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AuditEvents,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProvenanceSnapshots,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConversationThreads,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RecommendationRecords,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RecommendationLearningProfiles,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AdvisoryScanSchedules,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AdvisoryScanExecutions,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArchitectureDigests,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DigestSubscriptions,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DigestDeliveryAttempts,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRules,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRecords,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRoutingSubscriptions,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertDeliveryAttempts,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.CompositeAlertRules,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPacks,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackAssignments,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RetrievalIndexingOutbox,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.IntegrationEventOutbox,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AuthorityPipelineWorkOutbox,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArchitectureRunIdempotency,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningPilotSignals,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementThemes,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlans,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.EvolutionCandidateChangeSets,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackChangeLog,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.UsageEvents,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.TenantHealthScores,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductFeedback,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishingTargets,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishJobs,
    ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingFeedback,
    ADD FILTER PREDICATE rls.archlucid_tenant_predicate(TenantId) ON dbo.SentEmails,
    ADD FILTER PREDICATE rls.archlucid_tenant_predicate(TenantId) ON dbo.TenantLifecycleTransitions,
    ADD FILTER PREDICATE rls.archlucid_tenant_predicate(TenantId) ON dbo.TenantTrialSeatOccupants,
    ADD FILTER PREDICATE rls.archlucid_tenant_predicate(TenantId) ON dbo.TenantOnboardingState,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.Runs AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.Runs AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.Runs BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DecisioningTraces AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DecisioningTraces AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DecisioningTraces BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifests AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifests AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifests BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshots AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshots AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshots BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingsSnapshots AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingsSnapshots AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingsSnapshots BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestAssumptions AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestAssumptions AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestAssumptions BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundles AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundles AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundles BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AuditEvents AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AuditEvents AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AuditEvents BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProvenanceSnapshots AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProvenanceSnapshots AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProvenanceSnapshots BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConversationThreads AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConversationThreads AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConversationThreads BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RecommendationRecords AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RecommendationRecords AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RecommendationRecords BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RecommendationLearningProfiles AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RecommendationLearningProfiles AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RecommendationLearningProfiles BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AdvisoryScanSchedules AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AdvisoryScanSchedules AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AdvisoryScanSchedules BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AdvisoryScanExecutions AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AdvisoryScanExecutions AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AdvisoryScanExecutions BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArchitectureDigests AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArchitectureDigests AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArchitectureDigests BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DigestSubscriptions AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DigestSubscriptions AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DigestSubscriptions BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DigestDeliveryAttempts AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DigestDeliveryAttempts AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DigestDeliveryAttempts BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRules AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRules AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRules BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRecords AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRecords AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRecords BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRoutingSubscriptions AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRoutingSubscriptions AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRoutingSubscriptions BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertDeliveryAttempts AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertDeliveryAttempts AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertDeliveryAttempts BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.CompositeAlertRules AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.CompositeAlertRules AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.CompositeAlertRules BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPacks AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPacks AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPacks BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackAssignments AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackAssignments AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackAssignments BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RetrievalIndexingOutbox AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RetrievalIndexingOutbox AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RetrievalIndexingOutbox BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.IntegrationEventOutbox AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.IntegrationEventOutbox AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.IntegrationEventOutbox BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AuthorityPipelineWorkOutbox AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AuthorityPipelineWorkOutbox AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AuthorityPipelineWorkOutbox BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArchitectureRunIdempotency AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArchitectureRunIdempotency AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArchitectureRunIdempotency BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningPilotSignals AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningPilotSignals AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningPilotSignals BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementThemes AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementThemes AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementThemes BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlans AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlans AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlans BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.EvolutionCandidateChangeSets AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.EvolutionCandidateChangeSets AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.EvolutionCandidateChangeSets BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackChangeLog AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackChangeLog AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackChangeLog BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.UsageEvents AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.UsageEvents AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.UsageEvents BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.TenantHealthScores AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.TenantHealthScores AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.TenantHealthScores BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductFeedback AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductFeedback AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductFeedback BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishingTargets AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishingTargets AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishingTargets BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishJobs AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishJobs AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishJobs BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingFeedback AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingFeedback AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingFeedback BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_tenant_predicate(TenantId) ON dbo.SentEmails AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_tenant_predicate(TenantId) ON dbo.SentEmails AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_tenant_predicate(TenantId) ON dbo.SentEmails BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_tenant_predicate(TenantId) ON dbo.TenantLifecycleTransitions AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_tenant_predicate(TenantId) ON dbo.TenantLifecycleTransitions AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_tenant_predicate(TenantId) ON dbo.TenantLifecycleTransitions BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_tenant_predicate(TenantId) ON dbo.TenantTrialSeatOccupants AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_tenant_predicate(TenantId) ON dbo.TenantTrialSeatOccupants AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_tenant_predicate(TenantId) ON dbo.TenantTrialSeatOccupants BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archlucid_tenant_predicate(TenantId) ON dbo.TenantOnboardingState AFTER INSERT,
    ADD BLOCK PREDICATE rls.archlucid_tenant_predicate(TenantId) ON dbo.TenantOnboardingState AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archlucid_tenant_predicate(TenantId) ON dbo.TenantOnboardingState BEFORE DELETE
    WITH (STATE = OFF);
GO

DECLARE @Restore BIT = TRY_CONVERT(BIT, SESSION_CONTEXT(N'__archlucid_108_previous_state'));

IF @Restore = 1
    ALTER SECURITY POLICY rls.ArchLucidTenantScope WITH (STATE = ON);

EXEC sp_set_session_context @key = N'__archlucid_108_previous_state', @value = NULL, @read_only = 0;
GO

/*
  Rollback 129: remove RLS bindings added for denormalized scope columns, then drop those columns.
  Forward: 129_RlsAuthorityChildTableScopeDenorm.sql
  See docs/security/MULTI_TENANT_RLS.md — break-glass only; re-apply 129 after corrective work if needed.
*/

SET XACT_ABORT ON;
GO

/* --- Part 1: drop security predicates (reverse order of forward migration) --- */

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ContextSnapshotSourceHashes', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.ContextSnapshotSourceHashes,
        DROP BLOCK PREDICATE ON dbo.ContextSnapshotSourceHashes FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.ContextSnapshotSourceHashes FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.ContextSnapshotSourceHashes FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ContextSnapshotErrors', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.ContextSnapshotErrors,
        DROP BLOCK PREDICATE ON dbo.ContextSnapshotErrors FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.ContextSnapshotErrors FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.ContextSnapshotErrors FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ContextSnapshotWarnings', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.ContextSnapshotWarnings,
        DROP BLOCK PREDICATE ON dbo.ContextSnapshotWarnings FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.ContextSnapshotWarnings FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.ContextSnapshotWarnings FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ContextSnapshotCanonicalObjectProperties', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.ContextSnapshotCanonicalObjectProperties,
        DROP BLOCK PREDICATE ON dbo.ContextSnapshotCanonicalObjectProperties FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.ContextSnapshotCanonicalObjectProperties FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.ContextSnapshotCanonicalObjectProperties FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ContextSnapshotCanonicalObjects', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.ContextSnapshotCanonicalObjects,
        DROP BLOCK PREDICATE ON dbo.ContextSnapshotCanonicalObjects FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.ContextSnapshotCanonicalObjects FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.ContextSnapshotCanonicalObjects FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GraphSnapshotWarnings', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.GraphSnapshotWarnings,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshotWarnings FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshotWarnings FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshotWarnings FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GraphSnapshotEdgeProperties', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.GraphSnapshotEdgeProperties,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshotEdgeProperties FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshotEdgeProperties FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshotEdgeProperties FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GraphSnapshotNodeProperties', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.GraphSnapshotNodeProperties,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshotNodeProperties FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshotNodeProperties FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshotNodeProperties FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GraphSnapshotNodes', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.GraphSnapshotNodes,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshotNodes FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshotNodes FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshotNodes FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GraphSnapshotEdges', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.GraphSnapshotEdges,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshotEdges FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshotEdges FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshotEdges FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.GraphSnapshots,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshots FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshots FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.GraphSnapshots FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ProductLearningImprovementPlanArtifactLinks', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.ProductLearningImprovementPlanArtifactLinks,
        DROP BLOCK PREDICATE ON dbo.ProductLearningImprovementPlanArtifactLinks FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.ProductLearningImprovementPlanArtifactLinks FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.ProductLearningImprovementPlanArtifactLinks FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ProductLearningImprovementPlanSignalLinks', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.ProductLearningImprovementPlanSignalLinks,
        DROP BLOCK PREDICATE ON dbo.ProductLearningImprovementPlanSignalLinks FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.ProductLearningImprovementPlanSignalLinks FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.ProductLearningImprovementPlanSignalLinks FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ProductLearningImprovementPlanArchitectureRuns', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.ProductLearningImprovementPlanArchitectureRuns,
        DROP BLOCK PREDICATE ON dbo.ProductLearningImprovementPlanArchitectureRuns FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.ProductLearningImprovementPlanArchitectureRuns FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.ProductLearningImprovementPlanArchitectureRuns FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GoldenManifestProvenanceAppliedRules', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.GoldenManifestProvenanceAppliedRules,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestProvenanceAppliedRules FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestProvenanceAppliedRules FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestProvenanceAppliedRules FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GoldenManifestProvenanceSourceGraphNodes', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.GoldenManifestProvenanceSourceGraphNodes,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestProvenanceSourceGraphNodes FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestProvenanceSourceGraphNodes FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestProvenanceSourceGraphNodes FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GoldenManifestProvenanceSourceFindings', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.GoldenManifestProvenanceSourceFindings,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestProvenanceSourceFindings FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestProvenanceSourceFindings FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestProvenanceSourceFindings FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GoldenManifestDecisionNodeLinks', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.GoldenManifestDecisionNodeLinks,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestDecisionNodeLinks FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestDecisionNodeLinks FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestDecisionNodeLinks FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GoldenManifestDecisionEvidenceLinks', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.GoldenManifestDecisionEvidenceLinks,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestDecisionEvidenceLinks FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestDecisionEvidenceLinks FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestDecisionEvidenceLinks FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GoldenManifestDecisions', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.GoldenManifestDecisions,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestDecisions FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestDecisions FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestDecisions FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GoldenManifestWarnings', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.GoldenManifestWarnings,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestWarnings FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestWarnings FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.GoldenManifestWarnings FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.EvolutionSimulationRuns', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.EvolutionSimulationRuns,
        DROP BLOCK PREDICATE ON dbo.EvolutionSimulationRuns FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.EvolutionSimulationRuns FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.EvolutionSimulationRuns FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.CompositeAlertRuleConditions', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.CompositeAlertRuleConditions,
        DROP BLOCK PREDICATE ON dbo.CompositeAlertRuleConditions FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.CompositeAlertRuleConditions FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.CompositeAlertRuleConditions FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.PolicyPackVersions', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.PolicyPackVersions,
        DROP BLOCK PREDICATE ON dbo.PolicyPackVersions FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.PolicyPackVersions FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.PolicyPackVersions FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ConversationMessages', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.ConversationMessages,
        DROP BLOCK PREDICATE ON dbo.ConversationMessages FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.ConversationMessages FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.ConversationMessages FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ArtifactBundleTraceNotes', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.ArtifactBundleTraceNotes,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleTraceNotes FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleTraceNotes FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleTraceNotes FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ArtifactBundleTraceDecisionLinks', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.ArtifactBundleTraceDecisionLinks,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleTraceDecisionLinks FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleTraceDecisionLinks FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleTraceDecisionLinks FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ArtifactBundleTraceGenerators', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.ArtifactBundleTraceGenerators,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleTraceGenerators FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleTraceGenerators FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleTraceGenerators FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ArtifactBundleArtifactDecisionLinks', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.ArtifactBundleArtifactDecisionLinks,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleArtifactDecisionLinks FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleArtifactDecisionLinks FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleArtifactDecisionLinks FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ArtifactBundleArtifactMetadata', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.ArtifactBundleArtifactMetadata,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleArtifactMetadata FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleArtifactMetadata FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleArtifactMetadata FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ArtifactBundleArtifacts', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.ArtifactBundleArtifacts,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleArtifacts FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleArtifacts FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.ArtifactBundleArtifacts FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingTraceNotes', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.FindingTraceNotes,
        DROP BLOCK PREDICATE ON dbo.FindingTraceNotes FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.FindingTraceNotes FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.FindingTraceNotes FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingTraceAlternativePaths', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.FindingTraceAlternativePaths,
        DROP BLOCK PREDICATE ON dbo.FindingTraceAlternativePaths FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.FindingTraceAlternativePaths FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.FindingTraceAlternativePaths FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingTraceDecisionsTaken', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.FindingTraceDecisionsTaken,
        DROP BLOCK PREDICATE ON dbo.FindingTraceDecisionsTaken FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.FindingTraceDecisionsTaken FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.FindingTraceDecisionsTaken FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingTraceRulesApplied', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.FindingTraceRulesApplied,
        DROP BLOCK PREDICATE ON dbo.FindingTraceRulesApplied FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.FindingTraceRulesApplied FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.FindingTraceRulesApplied FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingTraceGraphNodesExamined', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.FindingTraceGraphNodesExamined,
        DROP BLOCK PREDICATE ON dbo.FindingTraceGraphNodesExamined FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.FindingTraceGraphNodesExamined FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.FindingTraceGraphNodesExamined FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingProperties', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.FindingProperties,
        DROP BLOCK PREDICATE ON dbo.FindingProperties FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.FindingProperties FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.FindingProperties FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingRecommendedActions', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.FindingRecommendedActions,
        DROP BLOCK PREDICATE ON dbo.FindingRecommendedActions FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.FindingRecommendedActions FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.FindingRecommendedActions FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingRelatedNodes', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.FindingRelatedNodes,
        DROP BLOCK PREDICATE ON dbo.FindingRelatedNodes FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.FindingRelatedNodes FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.FindingRelatedNodes FOR BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingRecords', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.FindingRecords,
        DROP BLOCK PREDICATE ON dbo.FindingRecords FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.FindingRecords FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.FindingRecords FOR BEFORE DELETE;
END;
GO

/* --- Part 2: drop denormalized scope columns (ProjectId vs ScopeProjectId) --- */

IF OBJECT_ID(N'dbo.FindingRecords', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingRecords', N'TenantId') IS NOT NULL ALTER TABLE dbo.FindingRecords DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.FindingRecords', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.FindingRecords DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.FindingRecords', N'ProjectId') IS NOT NULL ALTER TABLE dbo.FindingRecords DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.FindingRelatedNodes', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingRelatedNodes', N'TenantId') IS NOT NULL ALTER TABLE dbo.FindingRelatedNodes DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.FindingRelatedNodes', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.FindingRelatedNodes DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.FindingRelatedNodes', N'ProjectId') IS NOT NULL ALTER TABLE dbo.FindingRelatedNodes DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.FindingRecommendedActions', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingRecommendedActions', N'TenantId') IS NOT NULL ALTER TABLE dbo.FindingRecommendedActions DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.FindingRecommendedActions', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.FindingRecommendedActions DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.FindingRecommendedActions', N'ProjectId') IS NOT NULL ALTER TABLE dbo.FindingRecommendedActions DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.FindingProperties', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingProperties', N'TenantId') IS NOT NULL ALTER TABLE dbo.FindingProperties DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.FindingProperties', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.FindingProperties DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.FindingProperties', N'ProjectId') IS NOT NULL ALTER TABLE dbo.FindingProperties DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceGraphNodesExamined', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingTraceGraphNodesExamined', N'TenantId') IS NOT NULL ALTER TABLE dbo.FindingTraceGraphNodesExamined DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.FindingTraceGraphNodesExamined', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.FindingTraceGraphNodesExamined DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.FindingTraceGraphNodesExamined', N'ProjectId') IS NOT NULL ALTER TABLE dbo.FindingTraceGraphNodesExamined DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceRulesApplied', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingTraceRulesApplied', N'TenantId') IS NOT NULL ALTER TABLE dbo.FindingTraceRulesApplied DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.FindingTraceRulesApplied', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.FindingTraceRulesApplied DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.FindingTraceRulesApplied', N'ProjectId') IS NOT NULL ALTER TABLE dbo.FindingTraceRulesApplied DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceDecisionsTaken', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingTraceDecisionsTaken', N'TenantId') IS NOT NULL ALTER TABLE dbo.FindingTraceDecisionsTaken DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.FindingTraceDecisionsTaken', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.FindingTraceDecisionsTaken DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.FindingTraceDecisionsTaken', N'ProjectId') IS NOT NULL ALTER TABLE dbo.FindingTraceDecisionsTaken DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceAlternativePaths', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingTraceAlternativePaths', N'TenantId') IS NOT NULL ALTER TABLE dbo.FindingTraceAlternativePaths DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.FindingTraceAlternativePaths', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.FindingTraceAlternativePaths DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.FindingTraceAlternativePaths', N'ProjectId') IS NOT NULL ALTER TABLE dbo.FindingTraceAlternativePaths DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceNotes', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingTraceNotes', N'TenantId') IS NOT NULL ALTER TABLE dbo.FindingTraceNotes DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.FindingTraceNotes', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.FindingTraceNotes DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.FindingTraceNotes', N'ProjectId') IS NOT NULL ALTER TABLE dbo.FindingTraceNotes DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GraphSnapshots', N'TenantId') IS NOT NULL ALTER TABLE dbo.GraphSnapshots DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.GraphSnapshots', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.GraphSnapshots DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.GraphSnapshots', N'ScopeProjectId') IS NOT NULL ALTER TABLE dbo.GraphSnapshots DROP COLUMN ScopeProjectId;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotEdges', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GraphSnapshotEdges', N'TenantId') IS NOT NULL ALTER TABLE dbo.GraphSnapshotEdges DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.GraphSnapshotEdges', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.GraphSnapshotEdges DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.GraphSnapshotEdges', N'ScopeProjectId') IS NOT NULL ALTER TABLE dbo.GraphSnapshotEdges DROP COLUMN ScopeProjectId;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotNodes', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GraphSnapshotNodes', N'TenantId') IS NOT NULL ALTER TABLE dbo.GraphSnapshotNodes DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.GraphSnapshotNodes', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.GraphSnapshotNodes DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.GraphSnapshotNodes', N'ScopeProjectId') IS NOT NULL ALTER TABLE dbo.GraphSnapshotNodes DROP COLUMN ScopeProjectId;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotNodeProperties', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GraphSnapshotNodeProperties', N'TenantId') IS NOT NULL ALTER TABLE dbo.GraphSnapshotNodeProperties DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.GraphSnapshotNodeProperties', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.GraphSnapshotNodeProperties DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.GraphSnapshotNodeProperties', N'ScopeProjectId') IS NOT NULL ALTER TABLE dbo.GraphSnapshotNodeProperties DROP COLUMN ScopeProjectId;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotEdgeProperties', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GraphSnapshotEdgeProperties', N'TenantId') IS NOT NULL ALTER TABLE dbo.GraphSnapshotEdgeProperties DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.GraphSnapshotEdgeProperties', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.GraphSnapshotEdgeProperties DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.GraphSnapshotEdgeProperties', N'ScopeProjectId') IS NOT NULL ALTER TABLE dbo.GraphSnapshotEdgeProperties DROP COLUMN ScopeProjectId;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotWarnings', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GraphSnapshotWarnings', N'TenantId') IS NOT NULL ALTER TABLE dbo.GraphSnapshotWarnings DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.GraphSnapshotWarnings', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.GraphSnapshotWarnings DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.GraphSnapshotWarnings', N'ScopeProjectId') IS NOT NULL ALTER TABLE dbo.GraphSnapshotWarnings DROP COLUMN ScopeProjectId;
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotCanonicalObjects', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ContextSnapshotCanonicalObjects', N'TenantId') IS NOT NULL ALTER TABLE dbo.ContextSnapshotCanonicalObjects DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.ContextSnapshotCanonicalObjects', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.ContextSnapshotCanonicalObjects DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.ContextSnapshotCanonicalObjects', N'ScopeProjectId') IS NOT NULL ALTER TABLE dbo.ContextSnapshotCanonicalObjects DROP COLUMN ScopeProjectId;
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotCanonicalObjectProperties', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ContextSnapshotCanonicalObjectProperties', N'TenantId') IS NOT NULL ALTER TABLE dbo.ContextSnapshotCanonicalObjectProperties DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.ContextSnapshotCanonicalObjectProperties', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.ContextSnapshotCanonicalObjectProperties DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.ContextSnapshotCanonicalObjectProperties', N'ScopeProjectId') IS NOT NULL ALTER TABLE dbo.ContextSnapshotCanonicalObjectProperties DROP COLUMN ScopeProjectId;
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotWarnings', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ContextSnapshotWarnings', N'TenantId') IS NOT NULL ALTER TABLE dbo.ContextSnapshotWarnings DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.ContextSnapshotWarnings', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.ContextSnapshotWarnings DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.ContextSnapshotWarnings', N'ScopeProjectId') IS NOT NULL ALTER TABLE dbo.ContextSnapshotWarnings DROP COLUMN ScopeProjectId;
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotErrors', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ContextSnapshotErrors', N'TenantId') IS NOT NULL ALTER TABLE dbo.ContextSnapshotErrors DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.ContextSnapshotErrors', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.ContextSnapshotErrors DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.ContextSnapshotErrors', N'ScopeProjectId') IS NOT NULL ALTER TABLE dbo.ContextSnapshotErrors DROP COLUMN ScopeProjectId;
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotSourceHashes', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ContextSnapshotSourceHashes', N'TenantId') IS NOT NULL ALTER TABLE dbo.ContextSnapshotSourceHashes DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.ContextSnapshotSourceHashes', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.ContextSnapshotSourceHashes DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.ContextSnapshotSourceHashes', N'ScopeProjectId') IS NOT NULL ALTER TABLE dbo.ContextSnapshotSourceHashes DROP COLUMN ScopeProjectId;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleArtifacts', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifacts', N'TenantId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleArtifacts DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifacts', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleArtifacts DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifacts', N'ProjectId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleArtifacts DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleArtifactMetadata', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifactMetadata', N'TenantId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleArtifactMetadata DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifactMetadata', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleArtifactMetadata DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifactMetadata', N'ProjectId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleArtifactMetadata DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleArtifactDecisionLinks', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifactDecisionLinks', N'TenantId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleArtifactDecisionLinks DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifactDecisionLinks', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleArtifactDecisionLinks DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifactDecisionLinks', N'ProjectId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleArtifactDecisionLinks DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleTraceGenerators', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceGenerators', N'TenantId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleTraceGenerators DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceGenerators', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleTraceGenerators DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceGenerators', N'ProjectId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleTraceGenerators DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleTraceDecisionLinks', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceDecisionLinks', N'TenantId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleTraceDecisionLinks DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceDecisionLinks', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleTraceDecisionLinks DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceDecisionLinks', N'ProjectId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleTraceDecisionLinks DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleTraceNotes', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceNotes', N'TenantId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleTraceNotes DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceNotes', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleTraceNotes DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceNotes', N'ProjectId') IS NOT NULL ALTER TABLE dbo.ArtifactBundleTraceNotes DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.ConversationMessages', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ConversationMessages', N'TenantId') IS NOT NULL ALTER TABLE dbo.ConversationMessages DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.ConversationMessages', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.ConversationMessages DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.ConversationMessages', N'ProjectId') IS NOT NULL ALTER TABLE dbo.ConversationMessages DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.PolicyPackVersions', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.PolicyPackVersions', N'TenantId') IS NOT NULL ALTER TABLE dbo.PolicyPackVersions DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.PolicyPackVersions', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.PolicyPackVersions DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.PolicyPackVersions', N'ProjectId') IS NOT NULL ALTER TABLE dbo.PolicyPackVersions DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.CompositeAlertRuleConditions', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.CompositeAlertRuleConditions', N'TenantId') IS NOT NULL ALTER TABLE dbo.CompositeAlertRuleConditions DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.CompositeAlertRuleConditions', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.CompositeAlertRuleConditions DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.CompositeAlertRuleConditions', N'ProjectId') IS NOT NULL ALTER TABLE dbo.CompositeAlertRuleConditions DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.EvolutionSimulationRuns', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.EvolutionSimulationRuns', N'TenantId') IS NOT NULL ALTER TABLE dbo.EvolutionSimulationRuns DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.EvolutionSimulationRuns', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.EvolutionSimulationRuns DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.EvolutionSimulationRuns', N'ProjectId') IS NOT NULL ALTER TABLE dbo.EvolutionSimulationRuns DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestWarnings', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GoldenManifestWarnings', N'TenantId') IS NOT NULL ALTER TABLE dbo.GoldenManifestWarnings DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.GoldenManifestWarnings', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.GoldenManifestWarnings DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.GoldenManifestWarnings', N'ProjectId') IS NOT NULL ALTER TABLE dbo.GoldenManifestWarnings DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestDecisions', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GoldenManifestDecisions', N'TenantId') IS NOT NULL ALTER TABLE dbo.GoldenManifestDecisions DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.GoldenManifestDecisions', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.GoldenManifestDecisions DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.GoldenManifestDecisions', N'ProjectId') IS NOT NULL ALTER TABLE dbo.GoldenManifestDecisions DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestDecisionEvidenceLinks', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GoldenManifestDecisionEvidenceLinks', N'TenantId') IS NOT NULL ALTER TABLE dbo.GoldenManifestDecisionEvidenceLinks DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.GoldenManifestDecisionEvidenceLinks', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.GoldenManifestDecisionEvidenceLinks DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.GoldenManifestDecisionEvidenceLinks', N'ProjectId') IS NOT NULL ALTER TABLE dbo.GoldenManifestDecisionEvidenceLinks DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestDecisionNodeLinks', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GoldenManifestDecisionNodeLinks', N'TenantId') IS NOT NULL ALTER TABLE dbo.GoldenManifestDecisionNodeLinks DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.GoldenManifestDecisionNodeLinks', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.GoldenManifestDecisionNodeLinks DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.GoldenManifestDecisionNodeLinks', N'ProjectId') IS NOT NULL ALTER TABLE dbo.GoldenManifestDecisionNodeLinks DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestProvenanceSourceFindings', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceSourceFindings', N'TenantId') IS NOT NULL ALTER TABLE dbo.GoldenManifestProvenanceSourceFindings DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceSourceFindings', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.GoldenManifestProvenanceSourceFindings DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceSourceFindings', N'ProjectId') IS NOT NULL ALTER TABLE dbo.GoldenManifestProvenanceSourceFindings DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestProvenanceSourceGraphNodes', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceSourceGraphNodes', N'TenantId') IS NOT NULL ALTER TABLE dbo.GoldenManifestProvenanceSourceGraphNodes DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceSourceGraphNodes', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.GoldenManifestProvenanceSourceGraphNodes DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceSourceGraphNodes', N'ProjectId') IS NOT NULL ALTER TABLE dbo.GoldenManifestProvenanceSourceGraphNodes DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestProvenanceAppliedRules', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceAppliedRules', N'TenantId') IS NOT NULL ALTER TABLE dbo.GoldenManifestProvenanceAppliedRules DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceAppliedRules', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.GoldenManifestProvenanceAppliedRules DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceAppliedRules', N'ProjectId') IS NOT NULL ALTER TABLE dbo.GoldenManifestProvenanceAppliedRules DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanArchitectureRuns', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanArchitectureRuns', N'TenantId') IS NOT NULL ALTER TABLE dbo.ProductLearningImprovementPlanArchitectureRuns DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanArchitectureRuns', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.ProductLearningImprovementPlanArchitectureRuns DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanArchitectureRuns', N'ProjectId') IS NOT NULL ALTER TABLE dbo.ProductLearningImprovementPlanArchitectureRuns DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanSignalLinks', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanSignalLinks', N'TenantId') IS NOT NULL ALTER TABLE dbo.ProductLearningImprovementPlanSignalLinks DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanSignalLinks', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.ProductLearningImprovementPlanSignalLinks DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanSignalLinks', N'ProjectId') IS NOT NULL ALTER TABLE dbo.ProductLearningImprovementPlanSignalLinks DROP COLUMN ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanArtifactLinks', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanArtifactLinks', N'TenantId') IS NOT NULL ALTER TABLE dbo.ProductLearningImprovementPlanArtifactLinks DROP COLUMN TenantId;
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanArtifactLinks', N'WorkspaceId') IS NOT NULL ALTER TABLE dbo.ProductLearningImprovementPlanArtifactLinks DROP COLUMN WorkspaceId;
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanArtifactLinks', N'ProjectId') IS NOT NULL ALTER TABLE dbo.ProductLearningImprovementPlanArtifactLinks DROP COLUMN ProjectId;
END;
GO

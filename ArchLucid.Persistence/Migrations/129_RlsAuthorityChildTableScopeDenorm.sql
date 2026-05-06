/*
  DbUp 129: Authority-path child table RLS scope denormalization (brownfield).
  Greenfield parity: ArchLucid.Persistence/Scripts/ArchLucid.sql.
  See docs/security/MULTI_TENANT_RLS.md §9.
*/

/* DbUp 129 Part 1 — add columns + backfills (see docs/security/MULTI_TENANT_RLS.md) */
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.FindingRecords', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingRecords', N'TenantId') IS NULL ALTER TABLE dbo.FindingRecords ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingRecords', N'WorkspaceId') IS NULL ALTER TABLE dbo.FindingRecords ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingRecords', N'ProjectId') IS NULL ALTER TABLE dbo.FindingRecords ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingRecords', N'U') IS NOT NULL
BEGIN
    UPDATE fr
    SET fr.TenantId = fs.TenantId,
        fr.WorkspaceId = fs.WorkspaceId,
        fr.ProjectId = fs.ProjectId
    FROM dbo.FindingRecords AS fr
    INNER JOIN dbo.FindingsSnapshots AS fs ON fr.FindingsSnapshotId = fs.FindingsSnapshotId
    WHERE fr.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingRelatedNodes', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingRelatedNodes', N'TenantId') IS NULL ALTER TABLE dbo.FindingRelatedNodes ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingRelatedNodes', N'WorkspaceId') IS NULL ALTER TABLE dbo.FindingRelatedNodes ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingRelatedNodes', N'ProjectId') IS NULL ALTER TABLE dbo.FindingRelatedNodes ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingRecommendedActions', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingRecommendedActions', N'TenantId') IS NULL ALTER TABLE dbo.FindingRecommendedActions ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingRecommendedActions', N'WorkspaceId') IS NULL ALTER TABLE dbo.FindingRecommendedActions ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingRecommendedActions', N'ProjectId') IS NULL ALTER TABLE dbo.FindingRecommendedActions ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingProperties', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingProperties', N'TenantId') IS NULL ALTER TABLE dbo.FindingProperties ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingProperties', N'WorkspaceId') IS NULL ALTER TABLE dbo.FindingProperties ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingProperties', N'ProjectId') IS NULL ALTER TABLE dbo.FindingProperties ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceGraphNodesExamined', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingTraceGraphNodesExamined', N'TenantId') IS NULL ALTER TABLE dbo.FindingTraceGraphNodesExamined ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingTraceGraphNodesExamined', N'WorkspaceId') IS NULL ALTER TABLE dbo.FindingTraceGraphNodesExamined ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingTraceGraphNodesExamined', N'ProjectId') IS NULL ALTER TABLE dbo.FindingTraceGraphNodesExamined ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceRulesApplied', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingTraceRulesApplied', N'TenantId') IS NULL ALTER TABLE dbo.FindingTraceRulesApplied ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingTraceRulesApplied', N'WorkspaceId') IS NULL ALTER TABLE dbo.FindingTraceRulesApplied ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingTraceRulesApplied', N'ProjectId') IS NULL ALTER TABLE dbo.FindingTraceRulesApplied ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceDecisionsTaken', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingTraceDecisionsTaken', N'TenantId') IS NULL ALTER TABLE dbo.FindingTraceDecisionsTaken ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingTraceDecisionsTaken', N'WorkspaceId') IS NULL ALTER TABLE dbo.FindingTraceDecisionsTaken ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingTraceDecisionsTaken', N'ProjectId') IS NULL ALTER TABLE dbo.FindingTraceDecisionsTaken ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceAlternativePaths', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingTraceAlternativePaths', N'TenantId') IS NULL ALTER TABLE dbo.FindingTraceAlternativePaths ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingTraceAlternativePaths', N'WorkspaceId') IS NULL ALTER TABLE dbo.FindingTraceAlternativePaths ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingTraceAlternativePaths', N'ProjectId') IS NULL ALTER TABLE dbo.FindingTraceAlternativePaths ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceNotes', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingTraceNotes', N'TenantId') IS NULL ALTER TABLE dbo.FindingTraceNotes ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingTraceNotes', N'WorkspaceId') IS NULL ALTER TABLE dbo.FindingTraceNotes ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.FindingTraceNotes', N'ProjectId') IS NULL ALTER TABLE dbo.FindingTraceNotes ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingRelatedNodes', N'U') IS NOT NULL
BEGIN
    UPDATE ch SET ch.TenantId = fr.TenantId, ch.WorkspaceId = fr.WorkspaceId, ch.ProjectId = fr.ProjectId
    FROM dbo.FindingRelatedNodes AS ch INNER JOIN dbo.FindingRecords AS fr ON ch.FindingRecordId = fr.FindingRecordId
    WHERE ch.TenantId IS NULL AND fr.TenantId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingRecommendedActions', N'U') IS NOT NULL
BEGIN
    UPDATE ch SET ch.TenantId = fr.TenantId, ch.WorkspaceId = fr.WorkspaceId, ch.ProjectId = fr.ProjectId
    FROM dbo.FindingRecommendedActions AS ch INNER JOIN dbo.FindingRecords AS fr ON ch.FindingRecordId = fr.FindingRecordId
    WHERE ch.TenantId IS NULL AND fr.TenantId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingProperties', N'U') IS NOT NULL
BEGIN
    UPDATE ch SET ch.TenantId = fr.TenantId, ch.WorkspaceId = fr.WorkspaceId, ch.ProjectId = fr.ProjectId
    FROM dbo.FindingProperties AS ch INNER JOIN dbo.FindingRecords AS fr ON ch.FindingRecordId = fr.FindingRecordId
    WHERE ch.TenantId IS NULL AND fr.TenantId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceGraphNodesExamined', N'U') IS NOT NULL
BEGIN
    UPDATE ch SET ch.TenantId = fr.TenantId, ch.WorkspaceId = fr.WorkspaceId, ch.ProjectId = fr.ProjectId
    FROM dbo.FindingTraceGraphNodesExamined AS ch INNER JOIN dbo.FindingRecords AS fr ON ch.FindingRecordId = fr.FindingRecordId
    WHERE ch.TenantId IS NULL AND fr.TenantId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceRulesApplied', N'U') IS NOT NULL
BEGIN
    UPDATE ch SET ch.TenantId = fr.TenantId, ch.WorkspaceId = fr.WorkspaceId, ch.ProjectId = fr.ProjectId
    FROM dbo.FindingTraceRulesApplied AS ch INNER JOIN dbo.FindingRecords AS fr ON ch.FindingRecordId = fr.FindingRecordId
    WHERE ch.TenantId IS NULL AND fr.TenantId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceDecisionsTaken', N'U') IS NOT NULL
BEGIN
    UPDATE ch SET ch.TenantId = fr.TenantId, ch.WorkspaceId = fr.WorkspaceId, ch.ProjectId = fr.ProjectId
    FROM dbo.FindingTraceDecisionsTaken AS ch INNER JOIN dbo.FindingRecords AS fr ON ch.FindingRecordId = fr.FindingRecordId
    WHERE ch.TenantId IS NULL AND fr.TenantId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceAlternativePaths', N'U') IS NOT NULL
BEGIN
    UPDATE ch SET ch.TenantId = fr.TenantId, ch.WorkspaceId = fr.WorkspaceId, ch.ProjectId = fr.ProjectId
    FROM dbo.FindingTraceAlternativePaths AS ch INNER JOIN dbo.FindingRecords AS fr ON ch.FindingRecordId = fr.FindingRecordId
    WHERE ch.TenantId IS NULL AND fr.TenantId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceNotes', N'U') IS NOT NULL
BEGIN
    UPDATE ch SET ch.TenantId = fr.TenantId, ch.WorkspaceId = fr.WorkspaceId, ch.ProjectId = fr.ProjectId
    FROM dbo.FindingTraceNotes AS ch INNER JOIN dbo.FindingRecords AS fr ON ch.FindingRecordId = fr.FindingRecordId
    WHERE ch.TenantId IS NULL AND fr.TenantId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GraphSnapshots', N'TenantId') IS NULL ALTER TABLE dbo.GraphSnapshots ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GraphSnapshots', N'WorkspaceId') IS NULL ALTER TABLE dbo.GraphSnapshots ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GraphSnapshots', N'ScopeProjectId') IS NULL ALTER TABLE dbo.GraphSnapshots ADD ScopeProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotEdges', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GraphSnapshotEdges', N'TenantId') IS NULL ALTER TABLE dbo.GraphSnapshotEdges ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GraphSnapshotEdges', N'WorkspaceId') IS NULL ALTER TABLE dbo.GraphSnapshotEdges ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GraphSnapshotEdges', N'ScopeProjectId') IS NULL ALTER TABLE dbo.GraphSnapshotEdges ADD ScopeProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotNodes', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GraphSnapshotNodes', N'TenantId') IS NULL ALTER TABLE dbo.GraphSnapshotNodes ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GraphSnapshotNodes', N'WorkspaceId') IS NULL ALTER TABLE dbo.GraphSnapshotNodes ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GraphSnapshotNodes', N'ScopeProjectId') IS NULL ALTER TABLE dbo.GraphSnapshotNodes ADD ScopeProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotNodeProperties', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GraphSnapshotNodeProperties', N'TenantId') IS NULL ALTER TABLE dbo.GraphSnapshotNodeProperties ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GraphSnapshotNodeProperties', N'WorkspaceId') IS NULL ALTER TABLE dbo.GraphSnapshotNodeProperties ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GraphSnapshotNodeProperties', N'ScopeProjectId') IS NULL ALTER TABLE dbo.GraphSnapshotNodeProperties ADD ScopeProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotEdgeProperties', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GraphSnapshotEdgeProperties', N'TenantId') IS NULL ALTER TABLE dbo.GraphSnapshotEdgeProperties ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GraphSnapshotEdgeProperties', N'WorkspaceId') IS NULL ALTER TABLE dbo.GraphSnapshotEdgeProperties ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GraphSnapshotEdgeProperties', N'ScopeProjectId') IS NULL ALTER TABLE dbo.GraphSnapshotEdgeProperties ADD ScopeProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotWarnings', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GraphSnapshotWarnings', N'TenantId') IS NULL ALTER TABLE dbo.GraphSnapshotWarnings ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GraphSnapshotWarnings', N'WorkspaceId') IS NULL ALTER TABLE dbo.GraphSnapshotWarnings ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GraphSnapshotWarnings', N'ScopeProjectId') IS NULL ALTER TABLE dbo.GraphSnapshotWarnings ADD ScopeProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
BEGIN
    UPDATE gs SET gs.TenantId = cs.TenantId, gs.WorkspaceId = cs.WorkspaceId, gs.ScopeProjectId = cs.ScopeProjectId
    FROM dbo.GraphSnapshots AS gs INNER JOIN dbo.ContextSnapshots AS cs ON gs.ContextSnapshotId = cs.SnapshotId
    WHERE gs.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotEdges', N'U') IS NOT NULL
BEGIN
    UPDATE e SET e.TenantId = gs.TenantId, e.WorkspaceId = gs.WorkspaceId, e.ScopeProjectId = gs.ScopeProjectId
    FROM dbo.GraphSnapshotEdges AS e INNER JOIN dbo.GraphSnapshots AS gs ON e.GraphSnapshotId = gs.GraphSnapshotId
    WHERE e.TenantId IS NULL AND gs.TenantId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotNodes', N'U') IS NOT NULL
BEGIN
    UPDATE n SET n.TenantId = gs.TenantId, n.WorkspaceId = gs.WorkspaceId, n.ScopeProjectId = gs.ScopeProjectId
    FROM dbo.GraphSnapshotNodes AS n INNER JOIN dbo.GraphSnapshots AS gs ON n.GraphSnapshotId = gs.GraphSnapshotId
    WHERE n.TenantId IS NULL AND gs.TenantId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotNodeProperties', N'U') IS NOT NULL
BEGIN
    UPDATE p SET p.TenantId = gn.TenantId, p.WorkspaceId = gn.WorkspaceId, p.ScopeProjectId = gn.ScopeProjectId
    FROM dbo.GraphSnapshotNodeProperties AS p INNER JOIN dbo.GraphSnapshotNodes AS gn ON p.GraphNodeRowId = gn.GraphNodeRowId
    WHERE p.TenantId IS NULL AND gn.TenantId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotEdgeProperties', N'U') IS NOT NULL
BEGIN
    UPDATE ep SET ep.TenantId = gs.TenantId, ep.WorkspaceId = gs.WorkspaceId, ep.ScopeProjectId = gs.ScopeProjectId
    FROM dbo.GraphSnapshotEdgeProperties AS ep INNER JOIN dbo.GraphSnapshots AS gs ON ep.GraphSnapshotId = gs.GraphSnapshotId
    WHERE ep.TenantId IS NULL AND gs.TenantId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotWarnings', N'U') IS NOT NULL
BEGIN
    UPDATE w SET w.TenantId = gs.TenantId, w.WorkspaceId = gs.WorkspaceId, w.ScopeProjectId = gs.ScopeProjectId
    FROM dbo.GraphSnapshotWarnings AS w INNER JOIN dbo.GraphSnapshots AS gs ON w.GraphSnapshotId = gs.GraphSnapshotId
    WHERE w.TenantId IS NULL AND gs.TenantId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotCanonicalObjects', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ContextSnapshotCanonicalObjects', N'TenantId') IS NULL ALTER TABLE dbo.ContextSnapshotCanonicalObjects ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ContextSnapshotCanonicalObjects', N'WorkspaceId') IS NULL ALTER TABLE dbo.ContextSnapshotCanonicalObjects ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ContextSnapshotCanonicalObjects', N'ScopeProjectId') IS NULL ALTER TABLE dbo.ContextSnapshotCanonicalObjects ADD ScopeProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotCanonicalObjectProperties', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ContextSnapshotCanonicalObjectProperties', N'TenantId') IS NULL ALTER TABLE dbo.ContextSnapshotCanonicalObjectProperties ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ContextSnapshotCanonicalObjectProperties', N'WorkspaceId') IS NULL ALTER TABLE dbo.ContextSnapshotCanonicalObjectProperties ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ContextSnapshotCanonicalObjectProperties', N'ScopeProjectId') IS NULL ALTER TABLE dbo.ContextSnapshotCanonicalObjectProperties ADD ScopeProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotWarnings', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ContextSnapshotWarnings', N'TenantId') IS NULL ALTER TABLE dbo.ContextSnapshotWarnings ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ContextSnapshotWarnings', N'WorkspaceId') IS NULL ALTER TABLE dbo.ContextSnapshotWarnings ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ContextSnapshotWarnings', N'ScopeProjectId') IS NULL ALTER TABLE dbo.ContextSnapshotWarnings ADD ScopeProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotErrors', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ContextSnapshotErrors', N'TenantId') IS NULL ALTER TABLE dbo.ContextSnapshotErrors ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ContextSnapshotErrors', N'WorkspaceId') IS NULL ALTER TABLE dbo.ContextSnapshotErrors ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ContextSnapshotErrors', N'ScopeProjectId') IS NULL ALTER TABLE dbo.ContextSnapshotErrors ADD ScopeProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotSourceHashes', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ContextSnapshotSourceHashes', N'TenantId') IS NULL ALTER TABLE dbo.ContextSnapshotSourceHashes ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ContextSnapshotSourceHashes', N'WorkspaceId') IS NULL ALTER TABLE dbo.ContextSnapshotSourceHashes ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ContextSnapshotSourceHashes', N'ScopeProjectId') IS NULL ALTER TABLE dbo.ContextSnapshotSourceHashes ADD ScopeProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotCanonicalObjects', N'U') IS NOT NULL
BEGIN
    UPDATE o SET o.TenantId = cs.TenantId, o.WorkspaceId = cs.WorkspaceId, o.ScopeProjectId = cs.ScopeProjectId
    FROM dbo.ContextSnapshotCanonicalObjects AS o INNER JOIN dbo.ContextSnapshots AS cs ON o.SnapshotId = cs.SnapshotId
    WHERE o.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotCanonicalObjectProperties', N'U') IS NOT NULL
BEGIN
    UPDATE cp SET cp.TenantId = o.TenantId, cp.WorkspaceId = o.WorkspaceId, cp.ScopeProjectId = o.ScopeProjectId
    FROM dbo.ContextSnapshotCanonicalObjectProperties AS cp INNER JOIN dbo.ContextSnapshotCanonicalObjects AS o ON cp.CanonicalObjectRowId = o.CanonicalObjectRowId
    WHERE cp.TenantId IS NULL AND o.TenantId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotWarnings', N'U') IS NOT NULL
BEGIN
    UPDATE w SET w.TenantId = cs.TenantId, w.WorkspaceId = cs.WorkspaceId, w.ScopeProjectId = cs.ScopeProjectId
    FROM dbo.ContextSnapshotWarnings AS w INNER JOIN dbo.ContextSnapshots AS cs ON w.SnapshotId = cs.SnapshotId
    WHERE w.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotErrors', N'U') IS NOT NULL
BEGIN
    UPDATE e SET e.TenantId = cs.TenantId, e.WorkspaceId = cs.WorkspaceId, e.ScopeProjectId = cs.ScopeProjectId
    FROM dbo.ContextSnapshotErrors AS e INNER JOIN dbo.ContextSnapshots AS cs ON e.SnapshotId = cs.SnapshotId
    WHERE e.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotSourceHashes', N'U') IS NOT NULL
BEGIN
    UPDATE h SET h.TenantId = cs.TenantId, h.WorkspaceId = cs.WorkspaceId, h.ScopeProjectId = cs.ScopeProjectId
    FROM dbo.ContextSnapshotSourceHashes AS h INNER JOIN dbo.ContextSnapshots AS cs ON h.SnapshotId = cs.SnapshotId
    WHERE h.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleArtifacts', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifacts', N'TenantId') IS NULL ALTER TABLE dbo.ArtifactBundleArtifacts ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifacts', N'WorkspaceId') IS NULL ALTER TABLE dbo.ArtifactBundleArtifacts ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifacts', N'ProjectId') IS NULL ALTER TABLE dbo.ArtifactBundleArtifacts ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleArtifactMetadata', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifactMetadata', N'TenantId') IS NULL ALTER TABLE dbo.ArtifactBundleArtifactMetadata ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifactMetadata', N'WorkspaceId') IS NULL ALTER TABLE dbo.ArtifactBundleArtifactMetadata ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifactMetadata', N'ProjectId') IS NULL ALTER TABLE dbo.ArtifactBundleArtifactMetadata ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleArtifactDecisionLinks', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifactDecisionLinks', N'TenantId') IS NULL ALTER TABLE dbo.ArtifactBundleArtifactDecisionLinks ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifactDecisionLinks', N'WorkspaceId') IS NULL ALTER TABLE dbo.ArtifactBundleArtifactDecisionLinks ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ArtifactBundleArtifactDecisionLinks', N'ProjectId') IS NULL ALTER TABLE dbo.ArtifactBundleArtifactDecisionLinks ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleTraceGenerators', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceGenerators', N'TenantId') IS NULL ALTER TABLE dbo.ArtifactBundleTraceGenerators ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceGenerators', N'WorkspaceId') IS NULL ALTER TABLE dbo.ArtifactBundleTraceGenerators ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceGenerators', N'ProjectId') IS NULL ALTER TABLE dbo.ArtifactBundleTraceGenerators ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleTraceDecisionLinks', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceDecisionLinks', N'TenantId') IS NULL ALTER TABLE dbo.ArtifactBundleTraceDecisionLinks ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceDecisionLinks', N'WorkspaceId') IS NULL ALTER TABLE dbo.ArtifactBundleTraceDecisionLinks ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceDecisionLinks', N'ProjectId') IS NULL ALTER TABLE dbo.ArtifactBundleTraceDecisionLinks ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleTraceNotes', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceNotes', N'TenantId') IS NULL ALTER TABLE dbo.ArtifactBundleTraceNotes ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceNotes', N'WorkspaceId') IS NULL ALTER TABLE dbo.ArtifactBundleTraceNotes ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ArtifactBundleTraceNotes', N'ProjectId') IS NULL ALTER TABLE dbo.ArtifactBundleTraceNotes ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleArtifacts', N'U') IS NOT NULL
BEGIN
    UPDATE a SET a.TenantId = b.TenantId, a.WorkspaceId = b.WorkspaceId, a.ProjectId = b.ProjectId
    FROM dbo.ArtifactBundleArtifacts AS a INNER JOIN dbo.ArtifactBundles AS b ON a.BundleId = b.BundleId
    WHERE a.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleArtifactMetadata', N'U') IS NOT NULL
BEGIN
    UPDATE m SET m.TenantId = a.TenantId, m.WorkspaceId = a.WorkspaceId, m.ProjectId = a.ProjectId
    FROM dbo.ArtifactBundleArtifactMetadata AS m INNER JOIN dbo.ArtifactBundleArtifacts AS a ON m.BundleId = a.BundleId AND m.ArtifactSortOrder = a.SortOrder
    WHERE m.TenantId IS NULL AND a.TenantId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleArtifactDecisionLinks', N'U') IS NOT NULL
BEGIN
    UPDATE d SET d.TenantId = a.TenantId, d.WorkspaceId = a.WorkspaceId, d.ProjectId = a.ProjectId
    FROM dbo.ArtifactBundleArtifactDecisionLinks AS d INNER JOIN dbo.ArtifactBundleArtifacts AS a ON d.BundleId = a.BundleId AND d.ArtifactSortOrder = a.SortOrder
    WHERE d.TenantId IS NULL AND a.TenantId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleTraceGenerators', N'U') IS NOT NULL
BEGIN
    UPDATE tr SET tr.TenantId = b.TenantId, tr.WorkspaceId = b.WorkspaceId, tr.ProjectId = b.ProjectId
    FROM dbo.ArtifactBundleTraceGenerators AS tr INNER JOIN dbo.ArtifactBundles AS b ON tr.BundleId = b.BundleId
    WHERE tr.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleTraceDecisionLinks', N'U') IS NOT NULL
BEGIN
    UPDATE tr SET tr.TenantId = b.TenantId, tr.WorkspaceId = b.WorkspaceId, tr.ProjectId = b.ProjectId
    FROM dbo.ArtifactBundleTraceDecisionLinks AS tr INNER JOIN dbo.ArtifactBundles AS b ON tr.BundleId = b.BundleId
    WHERE tr.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleTraceNotes', N'U') IS NOT NULL
BEGIN
    UPDATE tr SET tr.TenantId = b.TenantId, tr.WorkspaceId = b.WorkspaceId, tr.ProjectId = b.ProjectId
    FROM dbo.ArtifactBundleTraceNotes AS tr INNER JOIN dbo.ArtifactBundles AS b ON tr.BundleId = b.BundleId
    WHERE tr.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.ConversationMessages', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ConversationMessages', N'TenantId') IS NULL ALTER TABLE dbo.ConversationMessages ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ConversationMessages', N'WorkspaceId') IS NULL ALTER TABLE dbo.ConversationMessages ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ConversationMessages', N'ProjectId') IS NULL ALTER TABLE dbo.ConversationMessages ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.ConversationMessages', N'U') IS NOT NULL
BEGIN
    UPDATE cm SET cm.TenantId = ct.TenantId, cm.WorkspaceId = ct.WorkspaceId, cm.ProjectId = ct.ProjectId
    FROM dbo.ConversationMessages AS cm INNER JOIN dbo.ConversationThreads AS ct ON cm.ThreadId = ct.ThreadId
    WHERE cm.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.PolicyPackVersions', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.PolicyPackVersions', N'TenantId') IS NULL ALTER TABLE dbo.PolicyPackVersions ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.PolicyPackVersions', N'WorkspaceId') IS NULL ALTER TABLE dbo.PolicyPackVersions ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.PolicyPackVersions', N'ProjectId') IS NULL ALTER TABLE dbo.PolicyPackVersions ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.PolicyPackVersions', N'U') IS NOT NULL
BEGIN
    UPDATE v SET v.TenantId = p.TenantId, v.WorkspaceId = p.WorkspaceId, v.ProjectId = p.ProjectId
    FROM dbo.PolicyPackVersions AS v INNER JOIN dbo.PolicyPacks AS p ON v.PolicyPackId = p.PolicyPackId
    WHERE v.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.CompositeAlertRuleConditions', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.CompositeAlertRuleConditions', N'TenantId') IS NULL ALTER TABLE dbo.CompositeAlertRuleConditions ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.CompositeAlertRuleConditions', N'WorkspaceId') IS NULL ALTER TABLE dbo.CompositeAlertRuleConditions ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.CompositeAlertRuleConditions', N'ProjectId') IS NULL ALTER TABLE dbo.CompositeAlertRuleConditions ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.CompositeAlertRuleConditions', N'U') IS NOT NULL
BEGIN
    UPDATE c SET c.TenantId = r.TenantId, c.WorkspaceId = r.WorkspaceId, c.ProjectId = r.ProjectId
    FROM dbo.CompositeAlertRuleConditions AS c INNER JOIN dbo.CompositeAlertRules AS r ON c.CompositeRuleId = r.CompositeRuleId
    WHERE c.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.EvolutionSimulationRuns', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.EvolutionSimulationRuns', N'TenantId') IS NULL ALTER TABLE dbo.EvolutionSimulationRuns ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.EvolutionSimulationRuns', N'WorkspaceId') IS NULL ALTER TABLE dbo.EvolutionSimulationRuns ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.EvolutionSimulationRuns', N'ProjectId') IS NULL ALTER TABLE dbo.EvolutionSimulationRuns ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.EvolutionSimulationRuns', N'U') IS NOT NULL
BEGIN
    UPDATE s SET s.TenantId = e.TenantId, s.WorkspaceId = e.WorkspaceId, s.ProjectId = e.ProjectId
    FROM dbo.EvolutionSimulationRuns AS s INNER JOIN dbo.EvolutionCandidateChangeSets AS e ON s.CandidateChangeSetId = e.CandidateChangeSetId
    WHERE s.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestWarnings', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GoldenManifestWarnings', N'TenantId') IS NULL ALTER TABLE dbo.GoldenManifestWarnings ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GoldenManifestWarnings', N'WorkspaceId') IS NULL ALTER TABLE dbo.GoldenManifestWarnings ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GoldenManifestWarnings', N'ProjectId') IS NULL ALTER TABLE dbo.GoldenManifestWarnings ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestDecisions', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GoldenManifestDecisions', N'TenantId') IS NULL ALTER TABLE dbo.GoldenManifestDecisions ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GoldenManifestDecisions', N'WorkspaceId') IS NULL ALTER TABLE dbo.GoldenManifestDecisions ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GoldenManifestDecisions', N'ProjectId') IS NULL ALTER TABLE dbo.GoldenManifestDecisions ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestDecisionEvidenceLinks', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GoldenManifestDecisionEvidenceLinks', N'TenantId') IS NULL ALTER TABLE dbo.GoldenManifestDecisionEvidenceLinks ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GoldenManifestDecisionEvidenceLinks', N'WorkspaceId') IS NULL ALTER TABLE dbo.GoldenManifestDecisionEvidenceLinks ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GoldenManifestDecisionEvidenceLinks', N'ProjectId') IS NULL ALTER TABLE dbo.GoldenManifestDecisionEvidenceLinks ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestDecisionNodeLinks', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GoldenManifestDecisionNodeLinks', N'TenantId') IS NULL ALTER TABLE dbo.GoldenManifestDecisionNodeLinks ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GoldenManifestDecisionNodeLinks', N'WorkspaceId') IS NULL ALTER TABLE dbo.GoldenManifestDecisionNodeLinks ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GoldenManifestDecisionNodeLinks', N'ProjectId') IS NULL ALTER TABLE dbo.GoldenManifestDecisionNodeLinks ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestProvenanceSourceFindings', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceSourceFindings', N'TenantId') IS NULL ALTER TABLE dbo.GoldenManifestProvenanceSourceFindings ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceSourceFindings', N'WorkspaceId') IS NULL ALTER TABLE dbo.GoldenManifestProvenanceSourceFindings ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceSourceFindings', N'ProjectId') IS NULL ALTER TABLE dbo.GoldenManifestProvenanceSourceFindings ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestProvenanceSourceGraphNodes', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceSourceGraphNodes', N'TenantId') IS NULL ALTER TABLE dbo.GoldenManifestProvenanceSourceGraphNodes ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceSourceGraphNodes', N'WorkspaceId') IS NULL ALTER TABLE dbo.GoldenManifestProvenanceSourceGraphNodes ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceSourceGraphNodes', N'ProjectId') IS NULL ALTER TABLE dbo.GoldenManifestProvenanceSourceGraphNodes ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestProvenanceAppliedRules', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceAppliedRules', N'TenantId') IS NULL ALTER TABLE dbo.GoldenManifestProvenanceAppliedRules ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceAppliedRules', N'WorkspaceId') IS NULL ALTER TABLE dbo.GoldenManifestProvenanceAppliedRules ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.GoldenManifestProvenanceAppliedRules', N'ProjectId') IS NULL ALTER TABLE dbo.GoldenManifestProvenanceAppliedRules ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestWarnings', N'U') IS NOT NULL
BEGIN
    UPDATE w SET w.TenantId = gm.TenantId, w.WorkspaceId = gm.WorkspaceId, w.ProjectId = gm.ProjectId
    FROM dbo.GoldenManifestWarnings AS w INNER JOIN dbo.GoldenManifests AS gm ON w.ManifestId = gm.ManifestId
    WHERE w.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestDecisions', N'U') IS NOT NULL
BEGIN
    UPDATE w SET w.TenantId = gm.TenantId, w.WorkspaceId = gm.WorkspaceId, w.ProjectId = gm.ProjectId
    FROM dbo.GoldenManifestDecisions AS w INNER JOIN dbo.GoldenManifests AS gm ON w.ManifestId = gm.ManifestId
    WHERE w.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestDecisionEvidenceLinks', N'U') IS NOT NULL
BEGIN
    UPDATE l SET l.TenantId = gm.TenantId, l.WorkspaceId = gm.WorkspaceId, l.ProjectId = gm.ProjectId
    FROM dbo.GoldenManifestDecisionEvidenceLinks AS l INNER JOIN dbo.GoldenManifests AS gm ON l.ManifestId = gm.ManifestId
    WHERE l.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestDecisionNodeLinks', N'U') IS NOT NULL
BEGIN
    UPDATE l SET l.TenantId = gm.TenantId, l.WorkspaceId = gm.WorkspaceId, l.ProjectId = gm.ProjectId
    FROM dbo.GoldenManifestDecisionNodeLinks AS l INNER JOIN dbo.GoldenManifests AS gm ON l.ManifestId = gm.ManifestId
    WHERE l.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestProvenanceSourceFindings', N'U') IS NOT NULL
BEGIN
    UPDATE ps SET ps.TenantId = gm.TenantId, ps.WorkspaceId = gm.WorkspaceId, ps.ProjectId = gm.ProjectId
    FROM dbo.GoldenManifestProvenanceSourceFindings AS ps INNER JOIN dbo.GoldenManifests AS gm ON ps.ManifestId = gm.ManifestId
    WHERE ps.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestProvenanceSourceGraphNodes', N'U') IS NOT NULL
BEGIN
    UPDATE pn SET pn.TenantId = gm.TenantId, pn.WorkspaceId = gm.WorkspaceId, pn.ProjectId = gm.ProjectId
    FROM dbo.GoldenManifestProvenanceSourceGraphNodes AS pn INNER JOIN dbo.GoldenManifests AS gm ON pn.ManifestId = gm.ManifestId
    WHERE pn.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestProvenanceAppliedRules', N'U') IS NOT NULL
BEGIN
    UPDATE pr SET pr.TenantId = gm.TenantId, pr.WorkspaceId = gm.WorkspaceId, pr.ProjectId = gm.ProjectId
    FROM dbo.GoldenManifestProvenanceAppliedRules AS pr INNER JOIN dbo.GoldenManifests AS gm ON pr.ManifestId = gm.ManifestId
    WHERE pr.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanArchitectureRuns', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanArchitectureRuns', N'TenantId') IS NULL ALTER TABLE dbo.ProductLearningImprovementPlanArchitectureRuns ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanArchitectureRuns', N'WorkspaceId') IS NULL ALTER TABLE dbo.ProductLearningImprovementPlanArchitectureRuns ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanArchitectureRuns', N'ProjectId') IS NULL ALTER TABLE dbo.ProductLearningImprovementPlanArchitectureRuns ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanSignalLinks', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanSignalLinks', N'TenantId') IS NULL ALTER TABLE dbo.ProductLearningImprovementPlanSignalLinks ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanSignalLinks', N'WorkspaceId') IS NULL ALTER TABLE dbo.ProductLearningImprovementPlanSignalLinks ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanSignalLinks', N'ProjectId') IS NULL ALTER TABLE dbo.ProductLearningImprovementPlanSignalLinks ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanArtifactLinks', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanArtifactLinks', N'TenantId') IS NULL ALTER TABLE dbo.ProductLearningImprovementPlanArtifactLinks ADD TenantId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanArtifactLinks', N'WorkspaceId') IS NULL ALTER TABLE dbo.ProductLearningImprovementPlanArtifactLinks ADD WorkspaceId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.ProductLearningImprovementPlanArtifactLinks', N'ProjectId') IS NULL ALTER TABLE dbo.ProductLearningImprovementPlanArtifactLinks ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanArchitectureRuns', N'U') IS NOT NULL
BEGIN
    UPDATE x SET x.TenantId = p.TenantId, x.WorkspaceId = p.WorkspaceId, x.ProjectId = p.ProjectId
    FROM dbo.ProductLearningImprovementPlanArchitectureRuns AS x INNER JOIN dbo.ProductLearningImprovementPlans AS p ON x.PlanId = p.PlanId
    WHERE x.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanSignalLinks', N'U') IS NOT NULL
BEGIN
    UPDATE x SET x.TenantId = p.TenantId, x.WorkspaceId = p.WorkspaceId, x.ProjectId = p.ProjectId
    FROM dbo.ProductLearningImprovementPlanSignalLinks AS x INNER JOIN dbo.ProductLearningImprovementPlans AS p ON x.PlanId = p.PlanId
    WHERE x.TenantId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanArtifactLinks', N'U') IS NOT NULL
BEGIN
    UPDATE x SET x.TenantId = p.TenantId, x.WorkspaceId = p.WorkspaceId, x.ProjectId = p.ProjectId
    FROM dbo.ProductLearningImprovementPlanArtifactLinks AS x INNER JOIN dbo.ProductLearningImprovementPlans AS p ON x.PlanId = p.PlanId
    WHERE x.TenantId IS NULL;
END;
GO
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingRecords', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.FindingRecords', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingRecords,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingRecords AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingRecords AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingRecords BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingRelatedNodes', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.FindingRelatedNodes', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingRelatedNodes,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingRelatedNodes AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingRelatedNodes AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingRelatedNodes BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingRecommendedActions', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.FindingRecommendedActions', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingRecommendedActions,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingRecommendedActions AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingRecommendedActions AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingRecommendedActions BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingProperties', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.FindingProperties', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingProperties,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingProperties AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingProperties AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingProperties BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingTraceGraphNodesExamined', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.FindingTraceGraphNodesExamined', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceGraphNodesExamined,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceGraphNodesExamined AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceGraphNodesExamined AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceGraphNodesExamined BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingTraceRulesApplied', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.FindingTraceRulesApplied', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceRulesApplied,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceRulesApplied AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceRulesApplied AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceRulesApplied BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingTraceDecisionsTaken', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.FindingTraceDecisionsTaken', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceDecisionsTaken,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceDecisionsTaken AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceDecisionsTaken AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceDecisionsTaken BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingTraceAlternativePaths', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.FindingTraceAlternativePaths', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceAlternativePaths,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceAlternativePaths AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceAlternativePaths AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceAlternativePaths BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.FindingTraceNotes', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.FindingTraceNotes', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceNotes,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceNotes AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceNotes AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingTraceNotes BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ArtifactBundleArtifacts', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.ArtifactBundleArtifacts', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleArtifacts,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleArtifacts AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleArtifacts AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleArtifacts BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ArtifactBundleArtifactMetadata', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.ArtifactBundleArtifactMetadata', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleArtifactMetadata,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleArtifactMetadata AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleArtifactMetadata AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleArtifactMetadata BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ArtifactBundleArtifactDecisionLinks', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.ArtifactBundleArtifactDecisionLinks', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleArtifactDecisionLinks,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleArtifactDecisionLinks AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleArtifactDecisionLinks AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleArtifactDecisionLinks BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ArtifactBundleTraceGenerators', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.ArtifactBundleTraceGenerators', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleTraceGenerators,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleTraceGenerators AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleTraceGenerators AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleTraceGenerators BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ArtifactBundleTraceDecisionLinks', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.ArtifactBundleTraceDecisionLinks', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleTraceDecisionLinks,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleTraceDecisionLinks AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleTraceDecisionLinks AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleTraceDecisionLinks BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ArtifactBundleTraceNotes', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.ArtifactBundleTraceNotes', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleTraceNotes,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleTraceNotes AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleTraceNotes AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundleTraceNotes BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ConversationMessages', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.ConversationMessages', N'U'))
BEGIN
    /* Dynamic SQL: batch compile must not bind dbo.ConversationMessages when the table is absent on older DbUp-only DBs. */
    DECLARE @archlucidRlsConversationMessages NVARCHAR(MAX) = N'
ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConversationMessages,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConversationMessages AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConversationMessages AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConversationMessages BEFORE DELETE;';
    EXEC sys.sp_executesql @archlucidRlsConversationMessages;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.PolicyPackVersions', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.PolicyPackVersions', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackVersions,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackVersions AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackVersions AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackVersions BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.CompositeAlertRuleConditions', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.CompositeAlertRuleConditions', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.CompositeAlertRuleConditions,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.CompositeAlertRuleConditions AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.CompositeAlertRuleConditions AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.CompositeAlertRuleConditions BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.EvolutionSimulationRuns', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.EvolutionSimulationRuns', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.EvolutionSimulationRuns,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.EvolutionSimulationRuns AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.EvolutionSimulationRuns AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.EvolutionSimulationRuns BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GoldenManifestWarnings', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.GoldenManifestWarnings', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestWarnings,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestWarnings AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestWarnings AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestWarnings BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GoldenManifestDecisions', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.GoldenManifestDecisions', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestDecisions,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestDecisions AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestDecisions AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestDecisions BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GoldenManifestDecisionEvidenceLinks', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.GoldenManifestDecisionEvidenceLinks', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestDecisionEvidenceLinks,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestDecisionEvidenceLinks AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestDecisionEvidenceLinks AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestDecisionEvidenceLinks BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GoldenManifestDecisionNodeLinks', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.GoldenManifestDecisionNodeLinks', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestDecisionNodeLinks,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestDecisionNodeLinks AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestDecisionNodeLinks AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestDecisionNodeLinks BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GoldenManifestProvenanceSourceFindings', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.GoldenManifestProvenanceSourceFindings', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestProvenanceSourceFindings,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestProvenanceSourceFindings AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestProvenanceSourceFindings AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestProvenanceSourceFindings BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GoldenManifestProvenanceSourceGraphNodes', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.GoldenManifestProvenanceSourceGraphNodes', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestProvenanceSourceGraphNodes,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestProvenanceSourceGraphNodes AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestProvenanceSourceGraphNodes AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestProvenanceSourceGraphNodes BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GoldenManifestProvenanceAppliedRules', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.GoldenManifestProvenanceAppliedRules', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestProvenanceAppliedRules,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestProvenanceAppliedRules AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestProvenanceAppliedRules AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestProvenanceAppliedRules BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ProductLearningImprovementPlanArchitectureRuns', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.ProductLearningImprovementPlanArchitectureRuns', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlanArchitectureRuns,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlanArchitectureRuns AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlanArchitectureRuns AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlanArchitectureRuns BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ProductLearningImprovementPlanSignalLinks', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.ProductLearningImprovementPlanSignalLinks', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlanSignalLinks,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlanSignalLinks AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlanSignalLinks AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlanSignalLinks BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ProductLearningImprovementPlanArtifactLinks', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.ProductLearningImprovementPlanArtifactLinks', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlanArtifactLinks,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlanArtifactLinks AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlanArtifactLinks AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlanArtifactLinks BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.GraphSnapshots', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshots,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshots AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshots AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshots BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GraphSnapshotEdges', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.GraphSnapshotEdges', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotEdges,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotEdges AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotEdges AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotEdges BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GraphSnapshotNodes', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.GraphSnapshotNodes', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotNodes,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotNodes AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotNodes AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotNodes BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GraphSnapshotNodeProperties', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.GraphSnapshotNodeProperties', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotNodeProperties,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotNodeProperties AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotNodeProperties AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotNodeProperties BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GraphSnapshotEdgeProperties', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.GraphSnapshotEdgeProperties', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotEdgeProperties,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotEdgeProperties AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotEdgeProperties AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotEdgeProperties BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GraphSnapshotWarnings', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.GraphSnapshotWarnings', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotWarnings,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotWarnings AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotWarnings AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.GraphSnapshotWarnings BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ContextSnapshotCanonicalObjects', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.ContextSnapshotCanonicalObjects', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotCanonicalObjects,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotCanonicalObjects AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotCanonicalObjects AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotCanonicalObjects BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ContextSnapshotCanonicalObjectProperties', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.ContextSnapshotCanonicalObjectProperties', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotCanonicalObjectProperties,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotCanonicalObjectProperties AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotCanonicalObjectProperties AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotCanonicalObjectProperties BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ContextSnapshotWarnings', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.ContextSnapshotWarnings', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotWarnings,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotWarnings AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotWarnings AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotWarnings BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ContextSnapshotErrors', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.ContextSnapshotErrors', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotErrors,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotErrors AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotErrors AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotErrors BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.ContextSnapshotSourceHashes', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archlucid_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.security_policies AS sp ON sp.object_id = p.object_id
        WHERE sp.name = N'ArchLucidTenantScope'
          AND p.target_object_id = OBJECT_ID(N'dbo.ContextSnapshotSourceHashes', N'U'))
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotSourceHashes,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotSourceHashes AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotSourceHashes AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshotSourceHashes BEFORE DELETE;
END;
GO

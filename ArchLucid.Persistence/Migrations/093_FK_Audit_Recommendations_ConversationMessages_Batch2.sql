/*
  093: Foreign keys — batch 2 (audit optional refs, recommendation runs, conversation messages → thread).

  Idempotent: each FK added only when absent and referenced tables exist.

  Data hygiene:
  - dbo.AuditEvents: null RunId / ManifestId when no matching parent row.
  - dbo.RecommendationRecords: null ComparedToRunId when no matching dbo.Runs row.
  - dbo.ConversationMessages: DELETE rows whose ThreadId has no dbo.ConversationThreads row (orphan messages).

  Conditional add: dbo.RecommendationRecords.RunId → dbo.Runs is added only when every row has a matching run
  (NOT NULL column; no automatic null). If orphans exist, remediate and re-apply.

  Not in scope: dbo.AuditEvents.ArtifactId — line-level artifact IDs are unique per bundle
  (dbo.ArtifactBundleArtifacts UQ is on BundleId + ArtifactId), not a single-column parent key for FK.

  Rollback: Rollback/R093_FK_Audit_Recommendations_ConversationMessages_Batch2.sql
*/

IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
BEGIN
    UPDATE ae
    SET RunId = NULL
    FROM dbo.AuditEvents AS ae
    WHERE ae.RunId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.Runs AS r WHERE r.RunId = ae.RunId);
END;
GO

IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
BEGIN
    UPDATE ae
    SET ManifestId = NULL
    FROM dbo.AuditEvents AS ae
    WHERE ae.ManifestId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.GoldenManifests AS gm WHERE gm.ManifestId = ae.ManifestId);
END;
GO

IF OBJECT_ID(N'dbo.RecommendationRecords', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
BEGIN
    UPDATE rr
    SET ComparedToRunId = NULL
    FROM dbo.RecommendationRecords AS rr
    WHERE rr.ComparedToRunId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.Runs AS r WHERE r.RunId = rr.ComparedToRunId);
END;
GO

IF OBJECT_ID(N'dbo.ConversationMessages', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ConversationThreads', N'U') IS NOT NULL
BEGIN
    DELETE cm
    FROM dbo.ConversationMessages AS cm
    WHERE NOT EXISTS (SELECT 1 FROM dbo.ConversationThreads AS ct WHERE ct.ThreadId = cm.ThreadId);
END;
GO

IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AuditEvents_Runs_RunId')
BEGIN
    ALTER TABLE dbo.AuditEvents ADD CONSTRAINT FK_AuditEvents_Runs_RunId
        FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;
GO

IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AuditEvents_GoldenManifests_ManifestId')
BEGIN
    ALTER TABLE dbo.AuditEvents ADD CONSTRAINT FK_AuditEvents_GoldenManifests_ManifestId
        FOREIGN KEY (ManifestId) REFERENCES dbo.GoldenManifests (ManifestId);
END;
GO

IF OBJECT_ID(N'dbo.RecommendationRecords', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RecommendationRecords_Runs_RunId')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.RecommendationRecords AS rr
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Runs AS r WHERE r.RunId = rr.RunId))
BEGIN
    ALTER TABLE dbo.RecommendationRecords ADD CONSTRAINT FK_RecommendationRecords_Runs_RunId
        FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;
GO

IF OBJECT_ID(N'dbo.RecommendationRecords', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RecommendationRecords_Runs_ComparedToRunId')
BEGIN
    ALTER TABLE dbo.RecommendationRecords ADD CONSTRAINT FK_RecommendationRecords_Runs_ComparedToRunId
        FOREIGN KEY (ComparedToRunId) REFERENCES dbo.Runs (RunId);
END;
GO

IF OBJECT_ID(N'dbo.ConversationMessages', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ConversationThreads', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ConversationMessages_ConversationThreads_ThreadId')
BEGIN
    ALTER TABLE dbo.ConversationMessages ADD CONSTRAINT FK_ConversationMessages_ConversationThreads_ThreadId
        FOREIGN KEY (ThreadId) REFERENCES dbo.ConversationThreads (ThreadId);
END;
GO

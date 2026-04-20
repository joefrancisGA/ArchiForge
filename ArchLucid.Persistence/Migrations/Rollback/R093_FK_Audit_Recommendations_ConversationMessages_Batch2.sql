/*
  R093: Roll back 093 — drop batch-2 foreign keys (audit, recommendations, conversation messages).

  Does not restore deleted ConversationMessages rows or re-populate nulled AuditEvents / ComparedToRunId columns.
*/

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ConversationMessages_ConversationThreads_ThreadId' AND parent_object_id = OBJECT_ID(N'dbo.ConversationMessages'))
    ALTER TABLE dbo.ConversationMessages DROP CONSTRAINT FK_ConversationMessages_ConversationThreads_ThreadId;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RecommendationRecords_Runs_ComparedToRunId' AND parent_object_id = OBJECT_ID(N'dbo.RecommendationRecords'))
    ALTER TABLE dbo.RecommendationRecords DROP CONSTRAINT FK_RecommendationRecords_Runs_ComparedToRunId;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RecommendationRecords_Runs_RunId' AND parent_object_id = OBJECT_ID(N'dbo.RecommendationRecords'))
    ALTER TABLE dbo.RecommendationRecords DROP CONSTRAINT FK_RecommendationRecords_Runs_RunId;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AuditEvents_GoldenManifests_ManifestId' AND parent_object_id = OBJECT_ID(N'dbo.AuditEvents'))
    ALTER TABLE dbo.AuditEvents DROP CONSTRAINT FK_AuditEvents_GoldenManifests_ManifestId;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AuditEvents_Runs_RunId' AND parent_object_id = OBJECT_ID(N'dbo.AuditEvents'))
    ALTER TABLE dbo.AuditEvents DROP CONSTRAINT FK_AuditEvents_Runs_RunId;
GO

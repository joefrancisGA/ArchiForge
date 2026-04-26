/*
  096: CHECK (ISJSON(...)=1) on core NVARCHAR(MAX) columns that are contractually JSON (API / authority / worker payloads).

  Idempotent: each constraint is added only when absent and no row fails ISJSON (invalid or empty string fails; NULL
  passes only on nullable columns via (col IS NULL OR ISJSON(col)=1)).

  Skipped when legacy data violates the check — remediate JSON and re-run DbUp or ship a follow-up migration.

  Rollback: Rollback/R096_CheckJson_CorePayloadColumns.sql
*/

IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AuditEvents_DataJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.AuditEvents AS t WHERE ISJSON(t.DataJson) <> 1)
BEGIN
    ALTER TABLE dbo.AuditEvents ADD CONSTRAINT CK_AuditEvents_DataJson_IsJson
        CHECK (ISJSON(DataJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.AgentExecutionTraces', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AgentExecutionTraces_TraceJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.AgentExecutionTraces AS t WHERE ISJSON(t.TraceJson) <> 1)
BEGIN
    ALTER TABLE dbo.AgentExecutionTraces ADD CONSTRAINT CK_AgentExecutionTraces_TraceJson_IsJson
        CHECK (ISJSON(TraceJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.AgentResults', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AgentResults_ResultJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.AgentResults AS t WHERE ISJSON(t.ResultJson) <> 1)
BEGIN
    ALTER TABLE dbo.AgentResults ADD CONSTRAINT CK_AgentResults_ResultJson_IsJson
        CHECK (ISJSON(ResultJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.ComparisonRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ComparisonRecords_PayloadJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.ComparisonRecords AS t WHERE ISJSON(t.PayloadJson) <> 1)
BEGIN
    ALTER TABLE dbo.ComparisonRecords ADD CONSTRAINT CK_ComparisonRecords_PayloadJson_IsJson
        CHECK (ISJSON(PayloadJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.DecisionTraces', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DecisionTraces_EventJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.DecisionTraces AS t WHERE ISJSON(t.EventJson) <> 1)
BEGIN
    ALTER TABLE dbo.DecisionTraces ADD CONSTRAINT CK_DecisionTraces_EventJson_IsJson
        CHECK (ISJSON(EventJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DecisioningTraces_AppliedRuleIdsJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.DecisioningTraces AS t WHERE ISJSON(t.AppliedRuleIdsJson) <> 1)
BEGIN
    ALTER TABLE dbo.DecisioningTraces ADD CONSTRAINT CK_DecisioningTraces_AppliedRuleIdsJson_IsJson
        CHECK (ISJSON(AppliedRuleIdsJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DecisioningTraces_AcceptedFindingIdsJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.DecisioningTraces AS t WHERE ISJSON(t.AcceptedFindingIdsJson) <> 1)
BEGIN
    ALTER TABLE dbo.DecisioningTraces ADD CONSTRAINT CK_DecisioningTraces_AcceptedFindingIdsJson_IsJson
        CHECK (ISJSON(AcceptedFindingIdsJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DecisioningTraces_RejectedFindingIdsJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.DecisioningTraces AS t WHERE ISJSON(t.RejectedFindingIdsJson) <> 1)
BEGIN
    ALTER TABLE dbo.DecisioningTraces ADD CONSTRAINT CK_DecisioningTraces_RejectedFindingIdsJson_IsJson
        CHECK (ISJSON(RejectedFindingIdsJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DecisioningTraces_NotesJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.DecisioningTraces AS t WHERE ISJSON(t.NotesJson) <> 1)
BEGIN
    ALTER TABLE dbo.DecisioningTraces ADD CONSTRAINT CK_DecisioningTraces_NotesJson_IsJson
        CHECK (ISJSON(NotesJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.AuthorityPipelineWorkOutbox', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AuthorityPipelineWorkOutbox_PayloadJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.AuthorityPipelineWorkOutbox AS t WHERE ISJSON(t.PayloadJson) <> 1)
BEGIN
    ALTER TABLE dbo.AuthorityPipelineWorkOutbox ADD CONSTRAINT CK_AuthorityPipelineWorkOutbox_PayloadJson_IsJson
        CHECK (ISJSON(PayloadJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.RecommendationRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RecommendationRecords_SupportingFindingIdsJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.RecommendationRecords AS t WHERE ISJSON(t.SupportingFindingIdsJson) <> 1)
BEGIN
    ALTER TABLE dbo.RecommendationRecords ADD CONSTRAINT CK_RecommendationRecords_SupportingFindingIdsJson_IsJson
        CHECK (ISJSON(SupportingFindingIdsJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.RecommendationRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RecommendationRecords_SupportingDecisionIdsJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.RecommendationRecords AS t WHERE ISJSON(t.SupportingDecisionIdsJson) <> 1)
BEGIN
    ALTER TABLE dbo.RecommendationRecords ADD CONSTRAINT CK_RecommendationRecords_SupportingDecisionIdsJson_IsJson
        CHECK (ISJSON(SupportingDecisionIdsJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.RecommendationRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RecommendationRecords_SupportingArtifactIdsJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.RecommendationRecords AS t WHERE ISJSON(t.SupportingArtifactIdsJson) <> 1)
BEGIN
    ALTER TABLE dbo.RecommendationRecords ADD CONSTRAINT CK_RecommendationRecords_SupportingArtifactIdsJson_IsJson
        CHECK (ISJSON(SupportingArtifactIdsJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.BackgroundJobs', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.check_constraints
       WHERE name = N'CK_BackgroundJobs_WorkUnitJson_IsJson'
         AND parent_object_id = OBJECT_ID(N'dbo.BackgroundJobs'))
BEGIN
    /* Deferred name resolution: dbo.BackgroundJobs may be created only by ISchemaBootstrapper (ArchLucid.sql) after DbUp. */
    EXEC (N'
        IF NOT EXISTS (SELECT 1 FROM dbo.BackgroundJobs AS t WHERE ISJSON(t.WorkUnitJson) <> 1)
            ALTER TABLE dbo.BackgroundJobs ADD CONSTRAINT CK_BackgroundJobs_WorkUnitJson_IsJson
                CHECK (ISJSON(WorkUnitJson) = 1);
    ');
END;
GO

/*
  R096: Roll back 096 — drop ISJSON CHECK constraints on core JSON payload columns.
*/

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_BackgroundJobs_WorkUnitJson_IsJson' AND parent_object_id = OBJECT_ID(N'dbo.BackgroundJobs'))
    ALTER TABLE dbo.BackgroundJobs DROP CONSTRAINT CK_BackgroundJobs_WorkUnitJson_IsJson;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RecommendationRecords_SupportingArtifactIdsJson_IsJson' AND parent_object_id = OBJECT_ID(N'dbo.RecommendationRecords'))
    ALTER TABLE dbo.RecommendationRecords DROP CONSTRAINT CK_RecommendationRecords_SupportingArtifactIdsJson_IsJson;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RecommendationRecords_SupportingDecisionIdsJson_IsJson' AND parent_object_id = OBJECT_ID(N'dbo.RecommendationRecords'))
    ALTER TABLE dbo.RecommendationRecords DROP CONSTRAINT CK_RecommendationRecords_SupportingDecisionIdsJson_IsJson;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RecommendationRecords_SupportingFindingIdsJson_IsJson' AND parent_object_id = OBJECT_ID(N'dbo.RecommendationRecords'))
    ALTER TABLE dbo.RecommendationRecords DROP CONSTRAINT CK_RecommendationRecords_SupportingFindingIdsJson_IsJson;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AuthorityPipelineWorkOutbox_PayloadJson_IsJson' AND parent_object_id = OBJECT_ID(N'dbo.AuthorityPipelineWorkOutbox'))
    ALTER TABLE dbo.AuthorityPipelineWorkOutbox DROP CONSTRAINT CK_AuthorityPipelineWorkOutbox_PayloadJson_IsJson;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DecisioningTraces_NotesJson_IsJson' AND parent_object_id = OBJECT_ID(N'dbo.DecisioningTraces'))
    ALTER TABLE dbo.DecisioningTraces DROP CONSTRAINT CK_DecisioningTraces_NotesJson_IsJson;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DecisioningTraces_RejectedFindingIdsJson_IsJson' AND parent_object_id = OBJECT_ID(N'dbo.DecisioningTraces'))
    ALTER TABLE dbo.DecisioningTraces DROP CONSTRAINT CK_DecisioningTraces_RejectedFindingIdsJson_IsJson;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DecisioningTraces_AcceptedFindingIdsJson_IsJson' AND parent_object_id = OBJECT_ID(N'dbo.DecisioningTraces'))
    ALTER TABLE dbo.DecisioningTraces DROP CONSTRAINT CK_DecisioningTraces_AcceptedFindingIdsJson_IsJson;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DecisioningTraces_AppliedRuleIdsJson_IsJson' AND parent_object_id = OBJECT_ID(N'dbo.DecisioningTraces'))
    ALTER TABLE dbo.DecisioningTraces DROP CONSTRAINT CK_DecisioningTraces_AppliedRuleIdsJson_IsJson;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DecisionTraces_EventJson_IsJson' AND parent_object_id = OBJECT_ID(N'dbo.DecisionTraces'))
    ALTER TABLE dbo.DecisionTraces DROP CONSTRAINT CK_DecisionTraces_EventJson_IsJson;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ComparisonRecords_PayloadJson_IsJson' AND parent_object_id = OBJECT_ID(N'dbo.ComparisonRecords'))
    ALTER TABLE dbo.ComparisonRecords DROP CONSTRAINT CK_ComparisonRecords_PayloadJson_IsJson;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AgentResults_ResultJson_IsJson' AND parent_object_id = OBJECT_ID(N'dbo.AgentResults'))
    ALTER TABLE dbo.AgentResults DROP CONSTRAINT CK_AgentResults_ResultJson_IsJson;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AgentExecutionTraces_TraceJson_IsJson' AND parent_object_id = OBJECT_ID(N'dbo.AgentExecutionTraces'))
    ALTER TABLE dbo.AgentExecutionTraces DROP CONSTRAINT CK_AgentExecutionTraces_TraceJson_IsJson;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AuditEvents_DataJson_IsJson' AND parent_object_id = OBJECT_ID(N'dbo.AuditEvents'))
    ALTER TABLE dbo.AuditEvents DROP CONSTRAINT CK_AuditEvents_DataJson_IsJson;
GO

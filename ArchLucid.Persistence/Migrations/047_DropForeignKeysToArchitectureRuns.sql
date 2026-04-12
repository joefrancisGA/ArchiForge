/*
  Migration 047: Drop FK constraints from coordinator / learning tables to dbo.ArchitectureRuns.

  Explicit constraint inventory (all reference dbo.ArchitectureRuns (RunId)):
  1. FK_AgentTasks_Run
  2. FK_AgentResults_Run
  3. FK_GoldenManifestVersions_Run
  4. FK_DecisionTraces_Run
  5. FK_AgentEvidencePackages_Run
  6. FK_AgentExecutionTraces_Run
  7. FK_RunExportRecords_Run
  8. FK_ComparisonRecords_LeftRun
  9. FK_ComparisonRecords_RightRun
  10. FK_DecisionNodes_Run
  11. FK_AgentEvaluations_Run
  12. FK_ArchitectureRunIdempotency_Run
  13. FK_ProductLearningPilotSignals_ArchitectureRun
  14. FK_ProductLearningImprovementPlanArchitectureRuns_Run
  15. FK_EvolutionSimulationRuns_ArchitectureRun

  Rationale (ADR-0012): NVARCHAR(64) string RunId columns cannot reference dbo.Runs.RunId
  (UNIQUEIDENTIFIER) without type migration. No replacement FK to dbo.Runs in this migration;
  integrity is application-enforced alongside dbo.Runs authority rows.

  Idempotent: each DROP is guarded by sys.foreign_keys.
*/

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentTasks_Run')
    ALTER TABLE dbo.AgentTasks DROP CONSTRAINT FK_AgentTasks_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentResults_Run')
    ALTER TABLE dbo.AgentResults DROP CONSTRAINT FK_AgentResults_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoldenManifestVersions_Run')
    ALTER TABLE dbo.GoldenManifestVersions DROP CONSTRAINT FK_GoldenManifestVersions_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DecisionTraces_Run')
    ALTER TABLE dbo.DecisionTraces DROP CONSTRAINT FK_DecisionTraces_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentEvidencePackages_Run')
    ALTER TABLE dbo.AgentEvidencePackages DROP CONSTRAINT FK_AgentEvidencePackages_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentExecutionTraces_Run')
    ALTER TABLE dbo.AgentExecutionTraces DROP CONSTRAINT FK_AgentExecutionTraces_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RunExportRecords_Run')
    ALTER TABLE dbo.RunExportRecords DROP CONSTRAINT FK_RunExportRecords_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ComparisonRecords_LeftRun')
    ALTER TABLE dbo.ComparisonRecords DROP CONSTRAINT FK_ComparisonRecords_LeftRun;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ComparisonRecords_RightRun')
    ALTER TABLE dbo.ComparisonRecords DROP CONSTRAINT FK_ComparisonRecords_RightRun;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DecisionNodes_Run')
    ALTER TABLE dbo.DecisionNodes DROP CONSTRAINT FK_DecisionNodes_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentEvaluations_Run')
    ALTER TABLE dbo.AgentEvaluations DROP CONSTRAINT FK_AgentEvaluations_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ArchitectureRunIdempotency_Run')
    ALTER TABLE dbo.ArchitectureRunIdempotency DROP CONSTRAINT FK_ArchitectureRunIdempotency_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProductLearningPilotSignals_ArchitectureRun')
    ALTER TABLE dbo.ProductLearningPilotSignals DROP CONSTRAINT FK_ProductLearningPilotSignals_ArchitectureRun;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProductLearningImprovementPlanArchitectureRuns_Run')
    ALTER TABLE dbo.ProductLearningImprovementPlanArchitectureRuns DROP CONSTRAINT FK_ProductLearningImprovementPlanArchitectureRuns_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_EvolutionSimulationRuns_ArchitectureRun')
    ALTER TABLE dbo.EvolutionSimulationRuns DROP CONSTRAINT FK_EvolutionSimulationRuns_ArchitectureRun;
GO

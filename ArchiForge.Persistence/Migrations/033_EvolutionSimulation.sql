-- 60R — Controlled evolution: candidate change sets from 59R improvement plans, shadow evaluation (read-only analysis only; no automatic system mutation).

IF OBJECT_ID(N'dbo.EvolutionCandidateChangeSets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EvolutionCandidateChangeSets
    (
        CandidateChangeSetId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_EvolutionCandidateChangeSets PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        SourcePlanId UNIQUEIDENTIFIER NOT NULL,
        Status NVARCHAR(32) NOT NULL CONSTRAINT DF_EvolutionCandidateChangeSets_Status DEFAULT (N'Draft'),
        Title NVARCHAR(512) NOT NULL,
        Summary NVARCHAR(MAX) NOT NULL,
        PlanSnapshotJson NVARCHAR(MAX) NOT NULL,
        DerivationRuleVersion NVARCHAR(64) NOT NULL CONSTRAINT DF_EvolutionCandidateChangeSets_RuleVersion DEFAULT (N'60R-v1'),
        CreatedUtc DATETIME2 NOT NULL,
        CreatedByUserId NVARCHAR(256) NULL,
        INDEX IX_EvolutionCandidateChangeSets_Scope_CreatedUtc NONCLUSTERED (TenantId, WorkspaceId, ProjectId, CreatedUtc DESC),
        INDEX IX_EvolutionCandidateChangeSets_SourcePlanId NONCLUSTERED (SourcePlanId),
        CONSTRAINT FK_EvolutionCandidateChangeSets_Plan FOREIGN KEY (SourcePlanId)
            REFERENCES dbo.ProductLearningImprovementPlans (PlanId)
    );
END;
GO

IF OBJECT_ID(N'dbo.EvolutionCandidateChangeSets', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_EvolutionCandidateChangeSets_Status')
    ALTER TABLE dbo.EvolutionCandidateChangeSets ADD CONSTRAINT CK_EvolutionCandidateChangeSets_Status
        CHECK (Status IN (N'Draft', N'Simulated', N'PendingHumanReview', N'Declined', N'Archived'));
GO

IF OBJECT_ID(N'dbo.EvolutionSimulationRuns', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EvolutionSimulationRuns
    (
        SimulationRunId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_EvolutionSimulationRuns PRIMARY KEY,
        CandidateChangeSetId UNIQUEIDENTIFIER NOT NULL,
        BaselineArchitectureRunId NVARCHAR(64) NOT NULL,
        EvaluationMode NVARCHAR(64) NOT NULL,
        OutcomeJson NVARCHAR(MAX) NOT NULL,
        WarningsJson NVARCHAR(MAX) NULL,
        CompletedUtc DATETIME2 NOT NULL,
        IsShadowOnly BIT NOT NULL CONSTRAINT DF_EvolutionSimulationRuns_IsShadowOnly DEFAULT (1),
        CONSTRAINT FK_EvolutionSimulationRuns_Candidate FOREIGN KEY (CandidateChangeSetId)
            REFERENCES dbo.EvolutionCandidateChangeSets (CandidateChangeSetId) ON DELETE CASCADE,
        INDEX IX_EvolutionSimulationRuns_CandidateId NONCLUSTERED (CandidateChangeSetId)
    );
END;
GO

IF OBJECT_ID(N'dbo.EvolutionSimulationRuns', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_EvolutionSimulationRuns_EvaluationMode')
    ALTER TABLE dbo.EvolutionSimulationRuns ADD CONSTRAINT CK_EvolutionSimulationRuns_EvaluationMode
        CHECK (EvaluationMode IN (N'ReadOnlyArchitectureAnalysis'));
GO

IF OBJECT_ID(N'dbo.EvolutionSimulationRuns', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_EvolutionSimulationRuns_ShadowOnly')
    ALTER TABLE dbo.EvolutionSimulationRuns ADD CONSTRAINT CK_EvolutionSimulationRuns_ShadowOnly
        CHECK (IsShadowOnly = 1);
GO

IF OBJECT_ID(N'dbo.EvolutionSimulationRuns', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_EvolutionSimulationRuns_ArchitectureRun')
    ALTER TABLE dbo.EvolutionSimulationRuns ADD CONSTRAINT FK_EvolutionSimulationRuns_ArchitectureRun
        FOREIGN KEY (BaselineArchitectureRunId) REFERENCES dbo.ArchitectureRuns (RunId);
GO

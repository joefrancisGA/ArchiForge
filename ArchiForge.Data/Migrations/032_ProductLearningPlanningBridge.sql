-- 59R — Learning-to-planning bridge: structured improvement themes, bounded plans, and explicit links to runs, signals, and artifacts.
-- Human-reviewable persistence only; no autonomous mutation of generation or evaluation logic.
IF OBJECT_ID(N'dbo.ProductLearningImprovementThemes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductLearningImprovementThemes
    (
        ThemeId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProductLearningImprovementThemes PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        ThemeKey NVARCHAR(256) NOT NULL,
        SourceAggregateKey NVARCHAR(450) NULL,
        PatternKey NVARCHAR(200) NULL,
        Title NVARCHAR(512) NOT NULL,
        Summary NVARCHAR(MAX) NOT NULL,
        AffectedArtifactTypeOrWorkflowArea NVARCHAR(512) NOT NULL,
        SeverityBand NVARCHAR(32) NOT NULL,
        EvidenceSignalCount INT NOT NULL,
        DistinctRunCount INT NOT NULL,
        AverageTrustScore FLOAT NULL,
        DerivationRuleVersion NVARCHAR(64) NOT NULL,
        Status NVARCHAR(32) NOT NULL CONSTRAINT DF_ProductLearningImprovementThemes_Status DEFAULT (N'Proposed'),
        CreatedUtc DATETIME2 NOT NULL,
        CreatedByUserId NVARCHAR(256) NULL,
        CONSTRAINT UQ_ProductLearningImprovementThemes_Scope_ThemeKey UNIQUE (TenantId, WorkspaceId, ProjectId, ThemeKey),
        INDEX IX_ProductLearningImprovementThemes_Scope_CreatedUtc NONCLUSTERED (TenantId, WorkspaceId, ProjectId, CreatedUtc DESC)
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementThemes', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ProductLearningImprovementThemes_Status')
    ALTER TABLE dbo.ProductLearningImprovementThemes ADD CONSTRAINT CK_ProductLearningImprovementThemes_Status
        CHECK (Status IN (N'Proposed', N'Accepted', N'Superseded', N'Archived'));
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlans', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductLearningImprovementPlans
    (
        PlanId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProductLearningImprovementPlans PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        ThemeId UNIQUEIDENTIFIER NOT NULL,
        Title NVARCHAR(512) NOT NULL,
        Summary NVARCHAR(MAX) NOT NULL,
        BoundedActionsJson NVARCHAR(MAX) NOT NULL,
        PriorityScore INT NOT NULL,
        PriorityExplanation NVARCHAR(MAX) NULL,
        Status NVARCHAR(32) NOT NULL CONSTRAINT DF_ProductLearningImprovementPlans_Status DEFAULT (N'Proposed'),
        CreatedUtc DATETIME2 NOT NULL,
        CreatedByUserId NVARCHAR(256) NULL,
        INDEX IX_ProductLearningImprovementPlans_Scope_CreatedUtc NONCLUSTERED (TenantId, WorkspaceId, ProjectId, CreatedUtc DESC),
        INDEX IX_ProductLearningImprovementPlans_ThemeId NONCLUSTERED (ThemeId),
        CONSTRAINT FK_ProductLearningImprovementPlans_Theme FOREIGN KEY (ThemeId)
            REFERENCES dbo.ProductLearningImprovementThemes (ThemeId)
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlans', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ProductLearningImprovementPlans_Status')
    ALTER TABLE dbo.ProductLearningImprovementPlans ADD CONSTRAINT CK_ProductLearningImprovementPlans_Status
        CHECK (Status IN (N'Proposed', N'UnderReview', N'Approved', N'Rejected', N'Completed'));
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanArchitectureRuns', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductLearningImprovementPlanArchitectureRuns
    (
        PlanId UNIQUEIDENTIFIER NOT NULL,
        ArchitectureRunId NVARCHAR(64) NOT NULL,
        CONSTRAINT PK_ProductLearningImprovementPlanArchitectureRuns PRIMARY KEY (PlanId, ArchitectureRunId),
        CONSTRAINT FK_ProductLearningImprovementPlanArchitectureRuns_Plan FOREIGN KEY (PlanId)
            REFERENCES dbo.ProductLearningImprovementPlans (PlanId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ProductLearningImprovementPlanArchitectureRuns_PlanId
        ON dbo.ProductLearningImprovementPlanArchitectureRuns (PlanId);
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanArchitectureRuns', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProductLearningImprovementPlanArchitectureRuns_Run')
    ALTER TABLE dbo.ProductLearningImprovementPlanArchitectureRuns ADD CONSTRAINT FK_ProductLearningImprovementPlanArchitectureRuns_Run
        FOREIGN KEY (ArchitectureRunId) REFERENCES dbo.ArchitectureRuns (RunId);
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanSignalLinks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductLearningImprovementPlanSignalLinks
    (
        PlanId UNIQUEIDENTIFIER NOT NULL,
        SignalId UNIQUEIDENTIFIER NOT NULL,
        TriageStatusSnapshot NVARCHAR(32) NULL,
        CONSTRAINT PK_ProductLearningImprovementPlanSignalLinks PRIMARY KEY (PlanId, SignalId),
        CONSTRAINT FK_ProductLearningImprovementPlanSignalLinks_Plan FOREIGN KEY (PlanId)
            REFERENCES dbo.ProductLearningImprovementPlans (PlanId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ProductLearningImprovementPlanSignalLinks_PlanId
        ON dbo.ProductLearningImprovementPlanSignalLinks (PlanId);
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanSignalLinks', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProductLearningImprovementPlanSignalLinks_Signal')
    ALTER TABLE dbo.ProductLearningImprovementPlanSignalLinks ADD CONSTRAINT FK_ProductLearningImprovementPlanSignalLinks_Signal
        FOREIGN KEY (SignalId) REFERENCES dbo.ProductLearningPilotSignals (SignalId);
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanSignalLinks', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ProductLearningImprovementPlanSignalLinks_TriageSnapshot')
    ALTER TABLE dbo.ProductLearningImprovementPlanSignalLinks ADD CONSTRAINT CK_ProductLearningImprovementPlanSignalLinks_TriageSnapshot
        CHECK (
            TriageStatusSnapshot IS NULL OR TriageStatusSnapshot IN (N'Open', N'Triaged', N'Backlog', N'Done', N'WontFix')
        );
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanArtifactLinks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductLearningImprovementPlanArtifactLinks
    (
        LinkId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProductLearningImprovementPlanArtifactLinks PRIMARY KEY,
        PlanId UNIQUEIDENTIFIER NOT NULL,
        AuthorityBundleId UNIQUEIDENTIFIER NULL,
        AuthorityArtifactSortOrder INT NULL,
        PilotArtifactHint NVARCHAR(512) NULL,
        CONSTRAINT FK_ProductLearningImprovementPlanArtifactLinks_Plan FOREIGN KEY (PlanId)
            REFERENCES dbo.ProductLearningImprovementPlans (PlanId) ON DELETE CASCADE,
        CONSTRAINT CK_ProductLearningImprovementPlanArtifactLinks_Target
            CHECK (
                (AuthorityBundleId IS NOT NULL AND AuthorityArtifactSortOrder IS NOT NULL)
                OR (PilotArtifactHint IS NOT NULL)
            )
    );

    CREATE NONCLUSTERED INDEX IX_ProductLearningImprovementPlanArtifactLinks_PlanId
        ON dbo.ProductLearningImprovementPlanArtifactLinks (PlanId);
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanArtifactLinks', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ArtifactBundleArtifacts', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProductLearningImprovementPlanArtifactLinks_BundleArtifact')
    ALTER TABLE dbo.ProductLearningImprovementPlanArtifactLinks ADD CONSTRAINT FK_ProductLearningImprovementPlanArtifactLinks_BundleArtifact
        FOREIGN KEY (AuthorityBundleId, AuthorityArtifactSortOrder) REFERENCES dbo.ArtifactBundleArtifacts (BundleId, SortOrder);
GO

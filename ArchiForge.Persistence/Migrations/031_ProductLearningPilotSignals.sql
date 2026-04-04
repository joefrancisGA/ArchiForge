-- 58R — Pilot / product-learning signals: trusted vs rejected vs revised outputs, optional pattern keys for aggregation.
IF OBJECT_ID(N'dbo.ProductLearningPilotSignals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductLearningPilotSignals
    (
        SignalId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProductLearningPilotSignals PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        ArchitectureRunId NVARCHAR(64) NULL,
        AuthorityRunId UNIQUEIDENTIFIER NULL,
        ManifestVersion NVARCHAR(128) NULL,
        SubjectType NVARCHAR(64) NOT NULL,
        Disposition NVARCHAR(32) NOT NULL,
        PatternKey NVARCHAR(200) NULL,
        ArtifactHint NVARCHAR(512) NULL,
        CommentShort NVARCHAR(2000) NULL,
        DetailJson NVARCHAR(MAX) NULL,
        RecordedByUserId NVARCHAR(256) NULL,
        RecordedByDisplayName NVARCHAR(256) NULL,
        RecordedUtc DATETIME2 NOT NULL,
        TriageStatus NVARCHAR(32) NOT NULL CONSTRAINT DF_ProductLearningPilotSignals_TriageStatus DEFAULT (N'Open'),
        INDEX IX_ProductLearningPilotSignals_Scope_RecordedUtc NONCLUSTERED (TenantId, WorkspaceId, ProjectId, RecordedUtc DESC),
        INDEX IX_ProductLearningPilotSignals_Scope_Disposition NONCLUSTERED (TenantId, WorkspaceId, ProjectId, Disposition, RecordedUtc DESC),
        INDEX IX_ProductLearningPilotSignals_Scope_PatternKey_Filtered NONCLUSTERED (TenantId, WorkspaceId, ProjectId, PatternKey, RecordedUtc DESC)
            WHERE PatternKey IS NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningPilotSignals', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProductLearningPilotSignals_ArchitectureRun')
    ALTER TABLE dbo.ProductLearningPilotSignals ADD CONSTRAINT FK_ProductLearningPilotSignals_ArchitectureRun
        FOREIGN KEY (ArchitectureRunId) REFERENCES dbo.ArchitectureRuns (RunId);
GO

IF OBJECT_ID(N'dbo.ProductLearningPilotSignals', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ProductLearningPilotSignals_Disposition')
    ALTER TABLE dbo.ProductLearningPilotSignals ADD CONSTRAINT CK_ProductLearningPilotSignals_Disposition
        CHECK (Disposition IN (N'Trusted', N'Rejected', N'Revised', N'NeedsFollowUp'));
GO

IF OBJECT_ID(N'dbo.ProductLearningPilotSignals', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ProductLearningPilotSignals_TriageStatus')
    ALTER TABLE dbo.ProductLearningPilotSignals ADD CONSTRAINT CK_ProductLearningPilotSignals_TriageStatus
        CHECK (TriageStatus IN (N'Open', N'Triaged', N'Backlog', N'Done', N'WontFix'));
GO

IF OBJECT_ID('dbo.RecommendationRecords', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RecommendationRecords
    (
        RecommendationId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,

        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,

        RunId UNIQUEIDENTIFIER NOT NULL,
        ComparedToRunId UNIQUEIDENTIFIER NULL,

        Title NVARCHAR(500) NOT NULL,
        Category NVARCHAR(100) NOT NULL,
        Rationale NVARCHAR(MAX) NOT NULL,
        SuggestedAction NVARCHAR(MAX) NOT NULL,
        Urgency NVARCHAR(50) NOT NULL,
        ExpectedImpact NVARCHAR(MAX) NOT NULL,
        PriorityScore INT NOT NULL,

        Status NVARCHAR(50) NOT NULL,

        CreatedUtc DATETIME2 NOT NULL,
        LastUpdatedUtc DATETIME2 NOT NULL,

        ReviewedByUserId NVARCHAR(200) NULL,
        ReviewedByUserName NVARCHAR(200) NULL,
        ReviewComment NVARCHAR(MAX) NULL,
        ResolutionRationale NVARCHAR(MAX) NULL,

        SupportingFindingIdsJson NVARCHAR(MAX) NOT NULL,
        SupportingDecisionIdsJson NVARCHAR(MAX) NOT NULL,
        SupportingArtifactIdsJson NVARCHAR(MAX) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_RecommendationRecords_Scope_Run
        ON dbo.RecommendationRecords (TenantId, WorkspaceId, ProjectId, RunId, CreatedUtc DESC);

    CREATE NONCLUSTERED INDEX IX_RecommendationRecords_Scope_Status
        ON dbo.RecommendationRecords (TenantId, WorkspaceId, ProjectId, Status, LastUpdatedUtc DESC);
END

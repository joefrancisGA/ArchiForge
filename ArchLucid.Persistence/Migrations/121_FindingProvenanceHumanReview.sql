/* 121 — Finding provenance / human-review columns + durable review trail. */

IF OBJECT_ID(N'dbo.FindingRecords', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingRecords', N'RequestInputRef') IS NULL
        ALTER TABLE dbo.FindingRecords ADD RequestInputRef NVARCHAR(64) NULL;

    IF COL_LENGTH(N'dbo.FindingRecords', N'RunIdRef') IS NULL
        ALTER TABLE dbo.FindingRecords ADD RunIdRef NVARCHAR(64) NULL;

    IF COL_LENGTH(N'dbo.FindingRecords', N'AgentExecutionTraceId') IS NULL
        ALTER TABLE dbo.FindingRecords ADD AgentExecutionTraceId NVARCHAR(32) NULL;

    IF COL_LENGTH(N'dbo.FindingRecords', N'ModelDeploymentName') IS NULL
        ALTER TABLE dbo.FindingRecords ADD ModelDeploymentName NVARCHAR(200) NULL;

    IF COL_LENGTH(N'dbo.FindingRecords', N'ModelVersion') IS NULL
        ALTER TABLE dbo.FindingRecords ADD ModelVersion NVARCHAR(200) NULL;

    IF COL_LENGTH(N'dbo.FindingRecords', N'PromptTemplateId') IS NULL
        ALTER TABLE dbo.FindingRecords ADD PromptTemplateId NVARCHAR(200) NULL;

    IF COL_LENGTH(N'dbo.FindingRecords', N'PromptTemplateVersion') IS NULL
        ALTER TABLE dbo.FindingRecords ADD PromptTemplateVersion NVARCHAR(100) NULL;

    IF COL_LENGTH(N'dbo.FindingRecords', N'ConfidenceScore') IS NULL
        ALTER TABLE dbo.FindingRecords ADD ConfidenceScore FLOAT NULL;

    IF COL_LENGTH(N'dbo.FindingRecords', N'PolicyRuleId') IS NULL
        ALTER TABLE dbo.FindingRecords ADD PolicyRuleId NVARCHAR(500) NULL;

    IF COL_LENGTH(N'dbo.FindingRecords', N'HumanReviewStatus') IS NULL
        ALTER TABLE dbo.FindingRecords
            ADD HumanReviewStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_FindingRecords_HumanReview DEFAULT (N'NotRequired');

    IF COL_LENGTH(N'dbo.FindingRecords', N'ReviewedByUserId') IS NULL
        ALTER TABLE dbo.FindingRecords ADD ReviewedByUserId NVARCHAR(256) NULL;

    IF COL_LENGTH(N'dbo.FindingRecords', N'ReviewedAtUtc') IS NULL
        ALTER TABLE dbo.FindingRecords ADD ReviewedAtUtc DATETIME2 NULL;

    IF COL_LENGTH(N'dbo.FindingRecords', N'ReviewNotes') IS NULL
        ALTER TABLE dbo.FindingRecords ADD ReviewNotes NVARCHAR(MAX) NULL;
END;
GO

IF OBJECT_ID(N'dbo.FindingReviewEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingReviewEvents
    (
        EventId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FindingReviewEvents PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        FindingId NVARCHAR(200) NOT NULL,
        ReviewerUserId NVARCHAR(256) NOT NULL,
        Action NVARCHAR(50) NOT NULL,
        Notes NVARCHAR(MAX) NULL,
        OccurredAtUtc DATETIME2 NOT NULL,
        RunId UNIQUEIDENTIFIER NULL
    );

    CREATE NONCLUSTERED INDEX IX_FindingReviewEvents_Tenant_Finding
        ON dbo.FindingReviewEvents (TenantId, FindingId, OccurredAtUtc DESC);
END;
GO

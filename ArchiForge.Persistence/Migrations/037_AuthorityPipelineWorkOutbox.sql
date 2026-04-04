-- Deferred authority pipeline: context ingestion + graph (+ downstream stages) processed by worker after run header commits.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuthorityPipelineWorkOutbox' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.AuthorityPipelineWorkOutbox
    (
        OutboxId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AuthorityPipelineWorkOutbox PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        PayloadJson NVARCHAR(MAX) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        ProcessedUtc DATETIME2 NULL
    );

    CREATE NONCLUSTERED INDEX IX_AuthorityPipelineWorkOutbox_Pending
        ON dbo.AuthorityPipelineWorkOutbox (ProcessedUtc, CreatedUtc)
        WHERE ProcessedUtc IS NULL;
END
GO

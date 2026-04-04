-- Post-commit queue for retrieval (RAG) indexing; processed by RetrievalIndexingOutboxProcessorHostedService.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RetrievalIndexingOutbox' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.RetrievalIndexingOutbox
    (
        OutboxId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RetrievalIndexingOutbox PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        ProcessedUtc DATETIME2 NULL
    );

    CREATE NONCLUSTERED INDEX IX_RetrievalIndexingOutbox_Pending
        ON dbo.RetrievalIndexingOutbox (ProcessedUtc, CreatedUtc)
        WHERE ProcessedUtc IS NULL;
END
GO

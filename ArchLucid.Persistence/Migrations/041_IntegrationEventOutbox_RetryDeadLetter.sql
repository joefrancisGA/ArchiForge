-- Retry / backoff and dead-letter columns for integration event outbox (Service Bus publish failures).

IF COL_LENGTH('dbo.IntegrationEventOutbox', 'RetryCount') IS NULL
BEGIN
    ALTER TABLE dbo.IntegrationEventOutbox ADD
        RetryCount INT NOT NULL CONSTRAINT DF_IntegrationEventOutbox_RetryCount DEFAULT (0);
END;
GO

IF COL_LENGTH('dbo.IntegrationEventOutbox', 'NextRetryUtc') IS NULL
BEGIN
    ALTER TABLE dbo.IntegrationEventOutbox ADD NextRetryUtc DATETIME2 NULL;
END;
GO

IF COL_LENGTH('dbo.IntegrationEventOutbox', 'LastErrorMessage') IS NULL
BEGIN
    ALTER TABLE dbo.IntegrationEventOutbox ADD LastErrorMessage NVARCHAR(2048) NULL;
END;
GO

IF COL_LENGTH('dbo.IntegrationEventOutbox', 'DeadLetteredUtc') IS NULL
BEGIN
    ALTER TABLE dbo.IntegrationEventOutbox ADD DeadLetteredUtc DATETIME2 NULL;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_IntegrationEventOutbox_Pending'
      AND object_id = OBJECT_ID(N'dbo.IntegrationEventOutbox'))
BEGIN
    DROP INDEX IX_IntegrationEventOutbox_Pending ON dbo.IntegrationEventOutbox;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_IntegrationEventOutbox_Pending'
      AND object_id = OBJECT_ID(N'dbo.IntegrationEventOutbox'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_IntegrationEventOutbox_Pending
        ON dbo.IntegrationEventOutbox (ProcessedUtc, NextRetryUtc, CreatedUtc)
        WHERE ProcessedUtc IS NULL AND DeadLetteredUtc IS NULL;
END;
GO

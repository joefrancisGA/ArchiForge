-- 056: Add BlobUploadFailed flag to AgentExecutionTraces
IF COL_LENGTH('dbo.AgentExecutionTraces', 'BlobUploadFailed') IS NULL
BEGIN
    ALTER TABLE dbo.AgentExecutionTraces
        ADD BlobUploadFailed BIT NULL;
END

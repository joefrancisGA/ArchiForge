-- 065: Filtered index for operator diagnostics — traces where mandatory inline forensic fallback failed
IF COL_LENGTH('dbo.AgentExecutionTraces', 'InlineFallbackFailed') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.indexes
       WHERE name = N'IX_AgentExecutionTraces_InlineFallbackFailed'
         AND object_id = OBJECT_ID(N'dbo.AgentExecutionTraces'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AgentExecutionTraces_InlineFallbackFailed
        ON dbo.AgentExecutionTraces (RunId, CreatedUtc DESC)
        WHERE InlineFallbackFailed = 1;
END

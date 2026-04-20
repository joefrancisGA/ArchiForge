/*
  Rollback 084: restore rowstore compression to NONE for dbo.AuditEvents and dbo.AgentExecutionTraces (all rowstore indexes).
*/
IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.indexes AS i
        INNER JOIN sys.partitions AS p
            ON p.object_id = i.object_id AND p.index_id = i.index_id
        WHERE i.object_id = OBJECT_ID(N'dbo.AuditEvents')
          AND i.is_disabled = 0
          AND i.type IN (0, 1, 2)
          AND p.data_compression_desc = N'PAGE')
BEGIN
    ALTER INDEX ALL ON dbo.AuditEvents REBUILD WITH (DATA_COMPRESSION = NONE, SORT_IN_TEMPDB = ON);
END;
GO

IF OBJECT_ID(N'dbo.AgentExecutionTraces', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.indexes AS i
        INNER JOIN sys.partitions AS p
            ON p.object_id = i.object_id AND p.index_id = i.index_id
        WHERE i.object_id = OBJECT_ID(N'dbo.AgentExecutionTraces')
          AND i.is_disabled = 0
          AND i.type IN (0, 1, 2)
          AND p.data_compression_desc = N'PAGE')
BEGIN
    ALTER INDEX ALL ON dbo.AgentExecutionTraces REBUILD WITH (DATA_COMPRESSION = NONE, SORT_IN_TEMPDB = ON);
END;
GO

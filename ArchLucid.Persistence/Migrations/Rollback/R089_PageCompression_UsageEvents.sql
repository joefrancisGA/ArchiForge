/*
  Rollback 089: restore rowstore compression to NONE for dbo.UsageEvents (all rowstore indexes).
*/
IF OBJECT_ID(N'dbo.UsageEvents', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.indexes AS i
        INNER JOIN sys.partitions AS p
            ON p.object_id = i.object_id AND p.index_id = i.index_id
        WHERE i.object_id = OBJECT_ID(N'dbo.UsageEvents')
          AND i.is_disabled = 0
          AND i.type IN (0, 1, 2)
          AND p.data_compression_desc = N'PAGE')
BEGIN
    ALTER INDEX ALL ON dbo.UsageEvents REBUILD WITH (DATA_COMPRESSION = NONE, SORT_IN_TEMPDB = ON);
END;
GO

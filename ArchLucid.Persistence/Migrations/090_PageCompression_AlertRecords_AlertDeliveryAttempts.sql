/*
  090: Rowstore PAGE compression on dbo.AlertRecords and dbo.AlertDeliveryAttempts (operational alert history).

  Paired slice (same pattern as 084): append-mostly alert rows and delivery attempt ledger; PAGE reduces
  storage and logical reads on scope/time list patterns.

  Idempotent per table: rebuild only when that table exists and any enabled rowstore partition is not PAGE.

  SKU: vCore / DTU Standard+; not Basic. Estimate: sp_estimate_data_compression_savings per object in pre-prod.
*/

IF OBJECT_ID(N'dbo.AlertRecords', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.indexes AS i
        INNER JOIN sys.partitions AS p
            ON p.object_id = i.object_id AND p.index_id = i.index_id
        WHERE i.object_id = OBJECT_ID(N'dbo.AlertRecords')
          AND i.is_disabled = 0
          AND i.type IN (0, 1, 2)
          AND p.data_compression_desc <> N'PAGE')
BEGIN
    ALTER INDEX ALL ON dbo.AlertRecords REBUILD WITH (DATA_COMPRESSION = PAGE, SORT_IN_TEMPDB = ON);
END;
GO

IF OBJECT_ID(N'dbo.AlertDeliveryAttempts', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.indexes AS i
        INNER JOIN sys.partitions AS p
            ON p.object_id = i.object_id AND p.index_id = i.index_id
        WHERE i.object_id = OBJECT_ID(N'dbo.AlertDeliveryAttempts')
          AND i.is_disabled = 0
          AND i.type IN (0, 1, 2)
          AND p.data_compression_desc <> N'PAGE')
BEGIN
    ALTER INDEX ALL ON dbo.AlertDeliveryAttempts REBUILD WITH (DATA_COMPRESSION = PAGE, SORT_IN_TEMPDB = ON);
END;
GO

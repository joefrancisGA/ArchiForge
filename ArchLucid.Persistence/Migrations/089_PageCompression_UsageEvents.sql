/*
  089: Rowstore PAGE compression on dbo.UsageEvents (metering append stream + tenant/kind indexes).

  Idempotent: rebuilds only when any enabled rowstore index partition is not already PAGE.
  Schedule off-peak: billing path table; rebuild holds schema locks briefly per index.

  SKU: compression requires vCore / DTU Standard+ (not Basic). Capture before/after logical reads or
  sp_estimate_data_compression_savings on a restored copy for documentation in ops reviews.
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
          AND p.data_compression_desc <> N'PAGE')
BEGIN
    ALTER INDEX ALL ON dbo.UsageEvents REBUILD WITH (DATA_COMPRESSION = PAGE, SORT_IN_TEMPDB = ON);
END;
GO

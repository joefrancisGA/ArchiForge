/*
  085: Rowstore PAGE compression on dbo.Runs (authority run header + all nonclustered indexes).

  Highest-impact follow-up to 084: list-by-scope queries and covering indexes (e.g. 061) benefit from denser pages
  on Azure SQL (fewer logical reads, smaller buffer pool footprint).

  Idempotent: rebuilds only when any enabled rowstore index partition is not already PAGE.
  Tier: requires a SKU that supports data compression (vCore / DTU Standard+; not Basic). Schedule apply during
  low traffic; consider ONLINE rebuild on tiers that support it (not specified here for broad SKU compatibility).

  Estimate on a copy: sp_estimate_data_compression_savings @schema_name='dbo', @object_name='Runs', ...
*/

IF OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.indexes AS i
        INNER JOIN sys.partitions AS p
            ON p.object_id = i.object_id AND p.index_id = i.index_id
        WHERE i.object_id = OBJECT_ID(N'dbo.Runs')
          AND i.is_disabled = 0
          AND i.type IN (0, 1, 2)
          AND p.data_compression_desc <> N'PAGE')
BEGIN
    ALTER INDEX ALL ON dbo.Runs REBUILD WITH (DATA_COMPRESSION = PAGE, SORT_IN_TEMPDB = ON);
END;
GO

/*
  088: Rowstore PAGE compression on dbo.DecisioningTraces (authority decision trace + JSON columns).

  Companion to 087 on dbo.DecisionTraces: same append-mostly pattern, multiple NVARCHAR(MAX) payloads,
  NC index on RunId. Idempotent: rebuilds only when any enabled rowstore partition is not already PAGE.

  SKU: PAGE compression unavailable on legacy DTU Basic; use vCore / DTU Standard+. Pre-prod estimate:
  sp_estimate_data_compression_savings @schema_name='dbo', @object_name='DecisioningTraces', ...
*/

IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.indexes AS i
        INNER JOIN sys.partitions AS p
            ON p.object_id = i.object_id AND p.index_id = i.index_id
        WHERE i.object_id = OBJECT_ID(N'dbo.DecisioningTraces')
          AND i.is_disabled = 0
          AND i.type IN (0, 1, 2)
          AND p.data_compression_desc <> N'PAGE')
BEGIN
    ALTER INDEX ALL ON dbo.DecisioningTraces REBUILD WITH (DATA_COMPRESSION = PAGE, SORT_IN_TEMPDB = ON);
END;
GO

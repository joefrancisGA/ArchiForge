/*
  087: Rowstore PAGE compression on dbo.DecisionTraces (append-mostly decision trace rows + indexes).

  Next slice after 084 (audit + agent execution traces) and 085 (Runs): same operational pattern for
  large JSON payloads and list-by-run indexes.

  Idempotent: rebuilds only when any enabled rowstore index partition is not already PAGE.

  SKU: PAGE compression is unavailable on legacy DTU Basic; use a tier that supports data compression
  (vCore / DTU Standard+). Before production apply, estimate on a copy:
  sp_estimate_data_compression_savings @schema_name='dbo', @object_name='DecisionTraces', ...
*/

IF OBJECT_ID(N'dbo.DecisionTraces', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.indexes AS i
        INNER JOIN sys.partitions AS p
            ON p.object_id = i.object_id AND p.index_id = i.index_id
        WHERE i.object_id = OBJECT_ID(N'dbo.DecisionTraces')
          AND i.is_disabled = 0
          AND i.type IN (0, 1, 2)
          AND p.data_compression_desc <> N'PAGE')
BEGIN
    ALTER INDEX ALL ON dbo.DecisionTraces REBUILD WITH (DATA_COMPRESSION = PAGE, SORT_IN_TEMPDB = ON);
END;
GO

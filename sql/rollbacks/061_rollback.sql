-- Rollback for migration 061: drop covering index IX_Runs_Scope_CreatedUtc.
-- WARNING: Dashboard list queries may regress until a replacement index is added.

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Runs_Scope_CreatedUtc'
      AND object_id = OBJECT_ID(N'dbo.Runs'))
BEGIN
    DROP INDEX IX_Runs_Scope_CreatedUtc ON dbo.Runs;
END

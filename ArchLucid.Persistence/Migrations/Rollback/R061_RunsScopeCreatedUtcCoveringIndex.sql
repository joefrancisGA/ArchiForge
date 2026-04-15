-- Rollback for migration 061: IX_Runs_Scope_CreatedUtc (operator-only; not run by DbUp).
-- Idempotent: safe to run when the index is absent.

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Runs_Scope_CreatedUtc'
      AND object_id = OBJECT_ID(N'dbo.Runs'))
    DROP INDEX IX_Runs_Scope_CreatedUtc ON dbo.Runs;
GO

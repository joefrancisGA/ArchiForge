/*
  091: Enable READ_COMMITTED_SNAPSHOT (RCSI) so default READ COMMITTED uses row versioning instead of
  blocking readers behind writers — pairs with removal of dirty-read hints on dbo.AuditEvents (DapperAuditRepository).

  Azure SQL / SQL Server: idempotent; runs only when the flag is currently off. Schedule during low traffic if
  the ALTER must wait on active transactions.

  See: https://learn.microsoft.com/sql/t-sql/statements/alter-database-transact-sql-set-options
*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.databases
    WHERE database_id = DB_ID()
      AND is_read_committed_snapshot_on = 1)
BEGIN
    ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON;
END;
GO

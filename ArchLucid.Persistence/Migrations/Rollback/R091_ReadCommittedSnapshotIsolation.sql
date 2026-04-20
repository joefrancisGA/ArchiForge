/*
  R091: Roll back 091 — disable READ_COMMITTED_SNAPSHOT.

  Warning: ALTER DATABASE ... SET READ_COMMITTED_SNAPSHOT OFF may block waiting for exclusive access.
  Use only in dev rollback or a controlled maintenance window; not a routine production operation.
*/

IF EXISTS (
    SELECT 1
    FROM sys.databases
    WHERE database_id = DB_ID()
      AND is_read_committed_snapshot_on = 1)
BEGIN
    ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT OFF;
END;
GO

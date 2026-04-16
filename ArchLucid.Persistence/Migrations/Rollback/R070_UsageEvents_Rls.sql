/*
  Rollback 070: remove UsageEvents RLS bindings then drop table.
*/
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
BEGIN
    ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        DROP FILTER PREDICATE ON dbo.UsageEvents,
        DROP BLOCK PREDICATE ON dbo.UsageEvents FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.UsageEvents FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.UsageEvents FOR BEFORE DELETE;
END;
GO

IF OBJECT_ID(N'dbo.UsageEvents', N'U') IS NOT NULL
    DROP TABLE dbo.UsageEvents;
GO

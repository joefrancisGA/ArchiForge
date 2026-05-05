IF EXISTS (
        SELECT 1
        FROM sys.security_policies AS p
        WHERE p.name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.PilotCloseouts', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.security_predicates AS pr
        INNER JOIN sys.objects AS t ON t.object_id = pr.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'PilotCloseouts')
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.PilotCloseouts,
        DROP BLOCK PREDICATE ON dbo.PilotCloseouts FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.PilotCloseouts FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.PilotCloseouts FOR BEFORE DELETE;
END;
GO

IF OBJECT_ID(N'dbo.PilotCloseouts', N'U') IS NOT NULL
    DROP TABLE dbo.PilotCloseouts;
GO

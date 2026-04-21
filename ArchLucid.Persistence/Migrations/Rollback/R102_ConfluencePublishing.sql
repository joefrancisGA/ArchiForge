SET NOCOUNT ON;
GO

/* R102: Rollback 102_ConfluencePublishing.sql — remove Confluence publisher tables (drops RLS predicates first). */

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.ConfluencePublishJobs', N'U') IS NOT NULL
BEGIN
    BEGIN TRY
        ALTER SECURITY POLICY rls.ArchiforgeTenantScope
            DROP FILTER PREDICATE ON dbo.ConfluencePublishJobs,
            DROP BLOCK PREDICATE ON dbo.ConfluencePublishJobs FOR AFTER INSERT,
            DROP BLOCK PREDICATE ON dbo.ConfluencePublishJobs FOR AFTER UPDATE,
            DROP BLOCK PREDICATE ON dbo.ConfluencePublishJobs FOR BEFORE DELETE;
    END TRY
    BEGIN CATCH
        /* Idempotent rollback when predicates were never bound. */
    END CATCH;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.ConfluencePublishingTargets', N'U') IS NOT NULL
BEGIN
    BEGIN TRY
        ALTER SECURITY POLICY rls.ArchiforgeTenantScope
            DROP FILTER PREDICATE ON dbo.ConfluencePublishingTargets,
            DROP BLOCK PREDICATE ON dbo.ConfluencePublishingTargets FOR AFTER INSERT,
            DROP BLOCK PREDICATE ON dbo.ConfluencePublishingTargets FOR AFTER UPDATE,
            DROP BLOCK PREDICATE ON dbo.ConfluencePublishingTargets FOR BEFORE DELETE;
    END TRY
    BEGIN CATCH
        /* Idempotent rollback when predicates were never bound. */
    END CATCH;
END;
GO

IF OBJECT_ID(N'dbo.ConfluencePublishJobs', N'U') IS NOT NULL
    DROP TABLE dbo.ConfluencePublishJobs;
GO

IF OBJECT_ID(N'dbo.ConfluencePublishingTargets', N'U') IS NOT NULL
    DROP TABLE dbo.ConfluencePublishingTargets;
GO

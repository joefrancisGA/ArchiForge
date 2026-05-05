IF DATABASE_PRINCIPAL_ID(N'ArchLucidApp') IS NOT NULL
   AND OBJECT_ID(N'dbo.sp_TenantHealthScores_BatchRefresh', N'P') IS NOT NULL
BEGIN
    REVOKE EXECUTE ON OBJECT::dbo.sp_TenantHealthScores_BatchRefresh TO [ArchLucidApp];
END;
GO

IF OBJECT_ID(N'dbo.sp_TenantHealthScores_BatchRefresh', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_TenantHealthScores_BatchRefresh;
GO

/*
  079: Append-only log of automated trial lifecycle transitions (Worker scheduler).
*/

IF OBJECT_ID(N'dbo.TenantLifecycleTransitions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantLifecycleTransitions
    (
        TransitionId BIGINT            NOT NULL IDENTITY(1, 1) CONSTRAINT PK_TenantLifecycleTransitions PRIMARY KEY,
        TenantId       UNIQUEIDENTIFIER NOT NULL,
        FromStatus     NVARCHAR(32)     NOT NULL,
        ToStatus       NVARCHAR(32)     NOT NULL,
        OccurredUtc    DATETIMEOFFSET   NOT NULL CONSTRAINT DF_TenantLifecycleTransitions_OccurredUtc DEFAULT (SYSUTCDATETIME()),
        Reason         NVARCHAR(256)    NULL
    );

    CREATE NONCLUSTERED INDEX IX_TenantLifecycleTransitions_Tenant_OccurredUtc
        ON dbo.TenantLifecycleTransitions (TenantId, OccurredUtc DESC);
END;
GO

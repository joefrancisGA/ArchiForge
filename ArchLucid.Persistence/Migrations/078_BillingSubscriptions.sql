/*
  078: Billing subscription state + webhook idempotency (Stripe / Azure Marketplace).

  RLS: dbo.BillingSubscriptions is tenant/workspace/project scoped like dbo.UsageEvents.
  dbo.BillingWebhookEvents is global (no RLS); webhook handlers must not echo raw payloads into logs.

  dbo.BillingSubscriptions: least-privilege app role mutates only via stored procedures (EXECUTE AS OWNER).
*/

IF OBJECT_ID(N'dbo.BillingSubscriptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BillingSubscriptions
    (
        TenantId               UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_BillingSubscriptions PRIMARY KEY,
        WorkspaceId            UNIQUEIDENTIFIER NOT NULL,
        ProjectId              UNIQUEIDENTIFIER NOT NULL,
        Provider               NVARCHAR(64)     NOT NULL,
        ProviderSubscriptionId NVARCHAR(256)    NOT NULL CONSTRAINT DF_BillingSubscriptions_ProviderSubscriptionId DEFAULT N'',
        Tier                   NVARCHAR(32)     NOT NULL,
        SeatsPurchased         INT              NOT NULL CONSTRAINT DF_BillingSubscriptions_SeatsPurchased DEFAULT (0),
        WorkspacesPurchased    INT              NOT NULL CONSTRAINT DF_BillingSubscriptions_WorkspacesPurchased DEFAULT (0),
        Status                 NVARCHAR(32)     NOT NULL,
        ActivatedUtc           DATETIMEOFFSET   NULL,
        CanceledUtc            DATETIMEOFFSET   NULL,
        RawWebhookJson         NVARCHAR(MAX)    NULL,
        CreatedUtc             DATETIMEOFFSET   NOT NULL CONSTRAINT DF_BillingSubscriptions_CreatedUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedUtc             DATETIMEOFFSET   NOT NULL CONSTRAINT DF_BillingSubscriptions_UpdatedUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_BillingSubscriptions_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
        CONSTRAINT CK_BillingSubscriptions_Status CHECK (Status IN (N'Pending', N'Active', N'Suspended', N'Canceled'))
    );

    CREATE NONCLUSTERED INDEX IX_BillingSubscriptions_ProviderSession
        ON dbo.BillingSubscriptions (Provider, ProviderSubscriptionId);
END;
GO

IF OBJECT_ID(N'dbo.BillingWebhookEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BillingWebhookEvents
    (
        EventId      NVARCHAR(128)  NOT NULL CONSTRAINT PK_BillingWebhookEvents PRIMARY KEY,
        Provider     NVARCHAR(64)    NOT NULL,
        EventType    NVARCHAR(128)   NOT NULL,
        PayloadJson  NVARCHAR(MAX)   NOT NULL,
        ReceivedUtc  DATETIMEOFFSET  NOT NULL CONSTRAINT DF_BillingWebhookEvents_ReceivedUtc DEFAULT (SYSUTCDATETIME()),
        ProcessedUtc DATETIMEOFFSET  NULL,
        ResultStatus NVARCHAR(64)    NULL
    );

    CREATE NONCLUSTERED INDEX IX_BillingWebhookEvents_ProviderReceived
        ON dbo.BillingWebhookEvents (Provider, ReceivedUtc);
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.BillingSubscriptions', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'BillingSubscriptions')
BEGIN
    EXEC (N'
ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.BillingSubscriptions,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.BillingSubscriptions AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.BillingSubscriptions AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.BillingSubscriptions BEFORE DELETE;
');
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Billing_UpsertPending
    @TenantId uniqueidentifier,
    @WorkspaceId uniqueidentifier,
    @ProjectId uniqueidentifier,
    @Provider nvarchar(64),
    @ProviderSubscriptionId nvarchar(256),
    @Tier nvarchar(32),
    @SeatsPurchased int,
    @WorkspacesPurchased int
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.BillingSubscriptions AS t
    USING (SELECT @TenantId AS TenantId) AS s ON t.TenantId = s.TenantId
    WHEN MATCHED THEN
        UPDATE SET
            WorkspaceId = @WorkspaceId,
            ProjectId = @ProjectId,
            Provider = @Provider,
            ProviderSubscriptionId = @ProviderSubscriptionId,
            Tier = @Tier,
            SeatsPurchased = @SeatsPurchased,
            WorkspacesPurchased = @WorkspacesPurchased,
            Status = N'Pending',
            ActivatedUtc = NULL,
            CanceledUtc = NULL,
            UpdatedUtc = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (TenantId, WorkspaceId, ProjectId, Provider, ProviderSubscriptionId, Tier, SeatsPurchased, WorkspacesPurchased, Status, ActivatedUtc, CanceledUtc, RawWebhookJson, CreatedUtc, UpdatedUtc)
        VALUES (@TenantId, @WorkspaceId, @ProjectId, @Provider, @ProviderSubscriptionId, @Tier, @SeatsPurchased, @WorkspacesPurchased, N'Pending', NULL, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Billing_Activate
    @TenantId uniqueidentifier,
    @WorkspaceId uniqueidentifier,
    @ProjectId uniqueidentifier,
    @Provider nvarchar(64),
    @ProviderSubscriptionId nvarchar(256),
    @Tier nvarchar(32),
    @SeatsPurchased int,
    @WorkspacesPurchased int,
    @RawWebhookJson nvarchar(max)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.BillingSubscriptions AS t
    USING (SELECT @TenantId AS TenantId) AS s ON t.TenantId = s.TenantId
    WHEN MATCHED THEN
        UPDATE SET
            WorkspaceId = @WorkspaceId,
            ProjectId = @ProjectId,
            Provider = @Provider,
            ProviderSubscriptionId = @ProviderSubscriptionId,
            Tier = @Tier,
            SeatsPurchased = @SeatsPurchased,
            WorkspacesPurchased = @WorkspacesPurchased,
            Status = N'Active',
            ActivatedUtc = SYSUTCDATETIME(),
            CanceledUtc = NULL,
            RawWebhookJson = @RawWebhookJson,
            UpdatedUtc = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (TenantId, WorkspaceId, ProjectId, Provider, ProviderSubscriptionId, Tier, SeatsPurchased, WorkspacesPurchased, Status, ActivatedUtc, CanceledUtc, RawWebhookJson, CreatedUtc, UpdatedUtc)
        VALUES (@TenantId, @WorkspaceId, @ProjectId, @Provider, @ProviderSubscriptionId, @Tier, @SeatsPurchased, @WorkspacesPurchased, N'Active', SYSUTCDATETIME(), NULL, @RawWebhookJson, SYSUTCDATETIME(), SYSUTCDATETIME());
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Billing_Suspend
    @TenantId uniqueidentifier
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.BillingSubscriptions
    SET Status = N'Suspended', UpdatedUtc = SYSUTCDATETIME()
    WHERE TenantId = @TenantId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Billing_Reinstate
    @TenantId uniqueidentifier
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.BillingSubscriptions
    SET Status = N'Active', UpdatedUtc = SYSUTCDATETIME()
    WHERE TenantId = @TenantId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Billing_Cancel
    @TenantId uniqueidentifier
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.BillingSubscriptions
    SET Status = N'Canceled', CanceledUtc = SYSUTCDATETIME(), UpdatedUtc = SYSUTCDATETIME()
    WHERE TenantId = @TenantId;
END;
GO

IF DATABASE_PRINCIPAL_ID(N'ArchLucidApp') IS NOT NULL
   AND OBJECT_ID(N'dbo.BillingSubscriptions', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_permissions AS dp
        INNER JOIN sys.database_principals AS gp ON dp.grantee_principal_id = gp.principal_id
        WHERE dp.class_desc = N'OBJECT_OR_COLUMN'
          AND dp.major_id = OBJECT_ID(N'dbo.BillingSubscriptions')
          AND dp.permission_name = N'INSERT'
          AND dp.state_desc = N'DENY'
          AND gp.name = N'ArchLucidApp')
    BEGIN
        DENY INSERT ON dbo.BillingSubscriptions TO [ArchLucidApp];
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_permissions AS dp
        INNER JOIN sys.database_principals AS gp ON dp.grantee_principal_id = gp.principal_id
        WHERE dp.class_desc = N'OBJECT_OR_COLUMN'
          AND dp.major_id = OBJECT_ID(N'dbo.BillingSubscriptions')
          AND dp.permission_name = N'UPDATE'
          AND dp.state_desc = N'DENY'
          AND gp.name = N'ArchLucidApp')
    BEGIN
        DENY UPDATE ON dbo.BillingSubscriptions TO [ArchLucidApp];
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_permissions AS dp
        INNER JOIN sys.database_principals AS gp ON dp.grantee_principal_id = gp.principal_id
        WHERE dp.class_desc = N'OBJECT_OR_COLUMN'
          AND dp.major_id = OBJECT_ID(N'dbo.BillingSubscriptions')
          AND dp.permission_name = N'DELETE'
          AND dp.state_desc = N'DENY'
          AND gp.name = N'ArchLucidApp')
    BEGIN
        DENY DELETE ON dbo.BillingSubscriptions TO [ArchLucidApp];
    END;

    GRANT EXECUTE ON OBJECT::dbo.sp_Billing_UpsertPending TO [ArchLucidApp];
    GRANT EXECUTE ON OBJECT::dbo.sp_Billing_Activate TO [ArchLucidApp];
    GRANT EXECUTE ON OBJECT::dbo.sp_Billing_Suspend TO [ArchLucidApp];
    GRANT EXECUTE ON OBJECT::dbo.sp_Billing_Reinstate TO [ArchLucidApp];
    GRANT EXECUTE ON OBJECT::dbo.sp_Billing_Cancel TO [ArchLucidApp];
END;
GO

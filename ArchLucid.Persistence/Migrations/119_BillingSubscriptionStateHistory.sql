/*
  119: Append-only billing subscription state history (audit trail alongside dbo.BillingSubscriptions).

  Inserts run inside existing dbo.sp_Billing_* procedures (EXECUTE AS OWNER).
  ArchLucidApp: DENY DML on history; no direct INSERT grant.
  RLS: triple scope (TenantId, WorkspaceId, ProjectId) on rls.ArchLucidTenantScope.
*/

IF OBJECT_ID(N'dbo.BillingSubscriptionStateHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BillingSubscriptionStateHistory
    (
        HistoryId                    UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_BillingSubscriptionStateHistory_Id DEFAULT NEWSEQUENTIALID(),
        TenantId                     UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId                 UNIQUEIDENTIFIER NOT NULL,
        ProjectId                    UNIQUEIDENTIFIER NOT NULL,
        RecordedUtc                  DATETIMEOFFSET   NOT NULL CONSTRAINT DF_BillingSubscriptionStateHistory_RecordedUtc DEFAULT (SYSDATETIMEOFFSET()),
        ChangeKind                   NVARCHAR(64)     NOT NULL,
        PrevStatus                   NVARCHAR(32)     NULL,
        NewStatus                    NVARCHAR(32)     NULL,
        PrevTier                     NVARCHAR(32)     NULL,
        NewTier                      NVARCHAR(32)     NULL,
        PrevSeatsPurchased           INT              NULL,
        NewSeatsPurchased            INT              NULL,
        PrevWorkspacesPurchased      INT              NULL,
        NewWorkspacesPurchased       INT              NULL,
        PrevProvider                 NVARCHAR(64)     NULL,
        NewProvider                  NVARCHAR(64)     NULL,
        PrevProviderSubscriptionId   NVARCHAR(256)    NULL,
        NewProviderSubscriptionId    NVARCHAR(256)    NULL,
        CONSTRAINT PK_BillingSubscriptionStateHistory PRIMARY KEY CLUSTERED (HistoryId),
        CONSTRAINT FK_BillingSubscriptionStateHistory_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );

    CREATE NONCLUSTERED INDEX IX_BillingSubscriptionStateHistory_Tenant_RecordedUtc
        ON dbo.BillingSubscriptionStateHistory (TenantId, RecordedUtc DESC);
END;
GO

IF DATABASE_PRINCIPAL_ID(N'ArchLucidApp') IS NOT NULL
   AND OBJECT_ID(N'dbo.BillingSubscriptionStateHistory', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_permissions AS dp
        INNER JOIN sys.database_principals AS gp ON dp.grantee_principal_id = gp.principal_id
        WHERE dp.class_desc = N'OBJECT_OR_COLUMN'
          AND dp.major_id = OBJECT_ID(N'dbo.BillingSubscriptionStateHistory')
          AND dp.permission_name = N'INSERT'
          AND dp.state_desc = N'DENY'
          AND gp.name = N'ArchLucidApp')
        DENY INSERT ON dbo.BillingSubscriptionStateHistory TO [ArchLucidApp];

    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_permissions AS dp
        INNER JOIN sys.database_principals AS gp ON dp.grantee_principal_id = gp.principal_id
        WHERE dp.class_desc = N'OBJECT_OR_COLUMN'
          AND dp.major_id = OBJECT_ID(N'dbo.BillingSubscriptionStateHistory')
          AND dp.permission_name = N'UPDATE'
          AND dp.state_desc = N'DENY'
          AND gp.name = N'ArchLucidApp')
        DENY UPDATE ON dbo.BillingSubscriptionStateHistory TO [ArchLucidApp];

    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_permissions AS dp
        INNER JOIN sys.database_principals AS gp ON dp.grantee_principal_id = gp.principal_id
        WHERE dp.class_desc = N'OBJECT_OR_COLUMN'
          AND dp.major_id = OBJECT_ID(N'dbo.BillingSubscriptionStateHistory')
          AND dp.permission_name = N'DELETE'
          AND dp.state_desc = N'DENY'
          AND gp.name = N'ArchLucidApp')
        DENY DELETE ON dbo.BillingSubscriptionStateHistory TO [ArchLucidApp];
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.BillingSubscriptionStateHistory', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'BillingSubscriptionStateHistory')
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.BillingSubscriptionStateHistory,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.BillingSubscriptionStateHistory AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.BillingSubscriptionStateHistory AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.BillingSubscriptionStateHistory BEFORE DELETE;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Billing_AppendStateHistory
    @TenantId uniqueidentifier,
    @WorkspaceId uniqueidentifier,
    @ProjectId uniqueidentifier,
    @ChangeKind nvarchar(64),
    @PrevStatus nvarchar(32),
    @NewStatus nvarchar(32),
    @PrevTier nvarchar(32),
    @NewTier nvarchar(32),
    @PrevSeatsPurchased int,
    @NewSeatsPurchased int,
    @PrevWorkspacesPurchased int,
    @NewWorkspacesPurchased int,
    @PrevProvider nvarchar(64),
    @NewProvider nvarchar(64),
    @PrevProviderSubscriptionId nvarchar(256),
    @NewProviderSubscriptionId nvarchar(256)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.BillingSubscriptionStateHistory (
        TenantId,
        WorkspaceId,
        ProjectId,
        ChangeKind,
        PrevStatus,
        NewStatus,
        PrevTier,
        NewTier,
        PrevSeatsPurchased,
        NewSeatsPurchased,
        PrevWorkspacesPurchased,
        NewWorkspacesPurchased,
        PrevProvider,
        NewProvider,
        PrevProviderSubscriptionId,
        NewProviderSubscriptionId)
    VALUES (
        @TenantId,
        @WorkspaceId,
        @ProjectId,
        @ChangeKind,
        @PrevStatus,
        @NewStatus,
        @PrevTier,
        @NewTier,
        @PrevSeatsPurchased,
        @NewSeatsPurchased,
        @PrevWorkspacesPurchased,
        @NewWorkspacesPurchased,
        @PrevProvider,
        @NewProvider,
        @PrevProviderSubscriptionId,
        @NewProviderSubscriptionId);
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

    DECLARE
        @PrevWorkspaceId uniqueidentifier,
        @PrevProjectId uniqueidentifier,
        @PrevProvider nvarchar(64),
        @PrevProviderSubscriptionId nvarchar(256),
        @PrevTier nvarchar(32),
        @PrevSeats int,
        @PrevWorkspaces int,
        @PrevStatus nvarchar(32);

    SELECT
        @PrevWorkspaceId = WorkspaceId,
        @PrevProjectId = ProjectId,
        @PrevProvider = Provider,
        @PrevProviderSubscriptionId = ProviderSubscriptionId,
        @PrevTier = Tier,
        @PrevSeats = SeatsPurchased,
        @PrevWorkspaces = WorkspacesPurchased,
        @PrevStatus = Status
    FROM dbo.BillingSubscriptions
    WHERE TenantId = @TenantId;

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

    EXEC dbo.sp_Billing_AppendStateHistory
        @TenantId = @TenantId,
        @WorkspaceId = @WorkspaceId,
        @ProjectId = @ProjectId,
        @ChangeKind = N'UpsertPending',
        @PrevStatus = @PrevStatus,
        @NewStatus = N'Pending',
        @PrevTier = @PrevTier,
        @NewTier = @Tier,
        @PrevSeatsPurchased = @PrevSeats,
        @NewSeatsPurchased = @SeatsPurchased,
        @PrevWorkspacesPurchased = @PrevWorkspaces,
        @NewWorkspacesPurchased = @WorkspacesPurchased,
        @PrevProvider = @PrevProvider,
        @NewProvider = @Provider,
        @PrevProviderSubscriptionId = @PrevProviderSubscriptionId,
        @NewProviderSubscriptionId = @ProviderSubscriptionId;
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

    DECLARE
        @PrevWorkspaceId uniqueidentifier,
        @PrevProjectId uniqueidentifier,
        @PrevProvider nvarchar(64),
        @PrevProviderSubscriptionId nvarchar(256),
        @PrevTier nvarchar(32),
        @PrevSeats int,
        @PrevWorkspaces int,
        @PrevStatus nvarchar(32);

    SELECT
        @PrevWorkspaceId = WorkspaceId,
        @PrevProjectId = ProjectId,
        @PrevProvider = Provider,
        @PrevProviderSubscriptionId = ProviderSubscriptionId,
        @PrevTier = Tier,
        @PrevSeats = SeatsPurchased,
        @PrevWorkspaces = WorkspacesPurchased,
        @PrevStatus = Status
    FROM dbo.BillingSubscriptions
    WHERE TenantId = @TenantId;

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

    EXEC dbo.sp_Billing_AppendStateHistory
        @TenantId = @TenantId,
        @WorkspaceId = @WorkspaceId,
        @ProjectId = @ProjectId,
        @ChangeKind = N'Activate',
        @PrevStatus = @PrevStatus,
        @NewStatus = N'Active',
        @PrevTier = @PrevTier,
        @NewTier = @Tier,
        @PrevSeatsPurchased = @PrevSeats,
        @NewSeatsPurchased = @SeatsPurchased,
        @PrevWorkspacesPurchased = @PrevWorkspaces,
        @NewWorkspacesPurchased = @WorkspacesPurchased,
        @PrevProvider = @PrevProvider,
        @NewProvider = @Provider,
        @PrevProviderSubscriptionId = @PrevProviderSubscriptionId,
        @NewProviderSubscriptionId = @ProviderSubscriptionId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Billing_Suspend
    @TenantId uniqueidentifier
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @WorkspaceId uniqueidentifier,
        @ProjectId uniqueidentifier,
        @PrevStatus nvarchar(32),
        @Tier nvarchar(32),
        @Seats int,
        @Workspaces int,
        @Prov nvarchar(64),
        @ProvSub nvarchar(256);

    SELECT
        @WorkspaceId = WorkspaceId,
        @ProjectId = ProjectId,
        @PrevStatus = Status,
        @Tier = Tier,
        @Seats = SeatsPurchased,
        @Workspaces = WorkspacesPurchased,
        @Prov = Provider,
        @ProvSub = ProviderSubscriptionId
    FROM dbo.BillingSubscriptions
    WHERE TenantId = @TenantId;

    UPDATE dbo.BillingSubscriptions
    SET Status = N'Suspended', UpdatedUtc = SYSUTCDATETIME()
    WHERE TenantId = @TenantId;

    IF @@ROWCOUNT > 0
        EXEC dbo.sp_Billing_AppendStateHistory
            @TenantId = @TenantId,
            @WorkspaceId = @WorkspaceId,
            @ProjectId = @ProjectId,
            @ChangeKind = N'Suspend',
            @PrevStatus = @PrevStatus,
            @NewStatus = N'Suspended',
            @PrevTier = @Tier,
            @NewTier = @Tier,
            @PrevSeatsPurchased = @Seats,
            @NewSeatsPurchased = @Seats,
            @PrevWorkspacesPurchased = @Workspaces,
            @NewWorkspacesPurchased = @Workspaces,
            @PrevProvider = @Prov,
            @NewProvider = @Prov,
            @PrevProviderSubscriptionId = @ProvSub,
            @NewProviderSubscriptionId = @ProvSub;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Billing_Reinstate
    @TenantId uniqueidentifier
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @WorkspaceId uniqueidentifier,
        @ProjectId uniqueidentifier,
        @PrevStatus nvarchar(32),
        @Tier nvarchar(32),
        @Seats int,
        @Workspaces int,
        @Prov nvarchar(64),
        @ProvSub nvarchar(256);

    SELECT
        @WorkspaceId = WorkspaceId,
        @ProjectId = ProjectId,
        @PrevStatus = Status,
        @Tier = Tier,
        @Seats = SeatsPurchased,
        @Workspaces = WorkspacesPurchased,
        @Prov = Provider,
        @ProvSub = ProviderSubscriptionId
    FROM dbo.BillingSubscriptions
    WHERE TenantId = @TenantId;

    UPDATE dbo.BillingSubscriptions
    SET Status = N'Active', UpdatedUtc = SYSUTCDATETIME()
    WHERE TenantId = @TenantId;

    IF @@ROWCOUNT > 0
        EXEC dbo.sp_Billing_AppendStateHistory
            @TenantId = @TenantId,
            @WorkspaceId = @WorkspaceId,
            @ProjectId = @ProjectId,
            @ChangeKind = N'Reinstate',
            @PrevStatus = @PrevStatus,
            @NewStatus = N'Active',
            @PrevTier = @Tier,
            @NewTier = @Tier,
            @PrevSeatsPurchased = @Seats,
            @NewSeatsPurchased = @Seats,
            @PrevWorkspacesPurchased = @Workspaces,
            @NewWorkspacesPurchased = @Workspaces,
            @PrevProvider = @Prov,
            @NewProvider = @Prov,
            @PrevProviderSubscriptionId = @ProvSub,
            @NewProviderSubscriptionId = @ProvSub;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Billing_Cancel
    @TenantId uniqueidentifier
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @WorkspaceId uniqueidentifier,
        @ProjectId uniqueidentifier,
        @PrevStatus nvarchar(32),
        @Tier nvarchar(32),
        @Seats int,
        @Workspaces int,
        @Prov nvarchar(64),
        @ProvSub nvarchar(256);

    SELECT
        @WorkspaceId = WorkspaceId,
        @ProjectId = ProjectId,
        @PrevStatus = Status,
        @Tier = Tier,
        @Seats = SeatsPurchased,
        @Workspaces = WorkspacesPurchased,
        @Prov = Provider,
        @ProvSub = ProviderSubscriptionId
    FROM dbo.BillingSubscriptions
    WHERE TenantId = @TenantId;

    UPDATE dbo.BillingSubscriptions
    SET Status = N'Canceled', CanceledUtc = SYSUTCDATETIME(), UpdatedUtc = SYSUTCDATETIME()
    WHERE TenantId = @TenantId;

    IF @@ROWCOUNT > 0
        EXEC dbo.sp_Billing_AppendStateHistory
            @TenantId = @TenantId,
            @WorkspaceId = @WorkspaceId,
            @ProjectId = @ProjectId,
            @ChangeKind = N'Cancel',
            @PrevStatus = @PrevStatus,
            @NewStatus = N'Canceled',
            @PrevTier = @Tier,
            @NewTier = @Tier,
            @PrevSeatsPurchased = @Seats,
            @NewSeatsPurchased = @Seats,
            @PrevWorkspacesPurchased = @Workspaces,
            @NewWorkspacesPurchased = @Workspaces,
            @PrevProvider = @Prov,
            @NewProvider = @Prov,
            @PrevProviderSubscriptionId = @ProvSub,
            @NewProviderSubscriptionId = @ProvSub;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Billing_ChangePlan
    @TenantId uniqueidentifier,
    @Tier nvarchar(32),
    @RawWebhookJson nvarchar(max)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @WorkspaceId uniqueidentifier,
        @ProjectId uniqueidentifier,
        @PrevStatus nvarchar(32),
        @PrevTier nvarchar(32),
        @Seats int,
        @Workspaces int,
        @Prov nvarchar(64),
        @ProvSub nvarchar(256);

    SELECT
        @WorkspaceId = WorkspaceId,
        @ProjectId = ProjectId,
        @PrevStatus = Status,
        @PrevTier = Tier,
        @Seats = SeatsPurchased,
        @Workspaces = WorkspacesPurchased,
        @Prov = Provider,
        @ProvSub = ProviderSubscriptionId
    FROM dbo.BillingSubscriptions
    WHERE TenantId = @TenantId;

    UPDATE dbo.BillingSubscriptions
    SET Tier = @Tier,
        RawWebhookJson = @RawWebhookJson,
        UpdatedUtc = SYSUTCDATETIME()
    WHERE TenantId = @TenantId;

    IF @@ROWCOUNT > 0
        EXEC dbo.sp_Billing_AppendStateHistory
            @TenantId = @TenantId,
            @WorkspaceId = @WorkspaceId,
            @ProjectId = @ProjectId,
            @ChangeKind = N'ChangePlan',
            @PrevStatus = @PrevStatus,
            @NewStatus = @PrevStatus,
            @PrevTier = @PrevTier,
            @NewTier = @Tier,
            @PrevSeatsPurchased = @Seats,
            @NewSeatsPurchased = @Seats,
            @PrevWorkspacesPurchased = @Workspaces,
            @NewWorkspacesPurchased = @Workspaces,
            @PrevProvider = @Prov,
            @NewProvider = @Prov,
            @PrevProviderSubscriptionId = @ProvSub,
            @NewProviderSubscriptionId = @ProvSub;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Billing_ChangeQuantity
    @TenantId uniqueidentifier,
    @SeatsPurchased int,
    @RawWebhookJson nvarchar(max)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @WorkspaceId uniqueidentifier,
        @ProjectId uniqueidentifier,
        @PrevStatus nvarchar(32),
        @Tier nvarchar(32),
        @PrevSeats int,
        @Workspaces int,
        @Prov nvarchar(64),
        @ProvSub nvarchar(256);

    SELECT
        @WorkspaceId = WorkspaceId,
        @ProjectId = ProjectId,
        @PrevStatus = Status,
        @Tier = Tier,
        @PrevSeats = SeatsPurchased,
        @Workspaces = WorkspacesPurchased,
        @Prov = Provider,
        @ProvSub = ProviderSubscriptionId
    FROM dbo.BillingSubscriptions
    WHERE TenantId = @TenantId;

    UPDATE dbo.BillingSubscriptions
    SET SeatsPurchased = @SeatsPurchased,
        RawWebhookJson = @RawWebhookJson,
        UpdatedUtc = SYSUTCDATETIME()
    WHERE TenantId = @TenantId;

    IF @@ROWCOUNT > 0
        EXEC dbo.sp_Billing_AppendStateHistory
            @TenantId = @TenantId,
            @WorkspaceId = @WorkspaceId,
            @ProjectId = @ProjectId,
            @ChangeKind = N'ChangeQuantity',
            @PrevStatus = @PrevStatus,
            @NewStatus = @PrevStatus,
            @PrevTier = @Tier,
            @NewTier = @Tier,
            @PrevSeatsPurchased = @PrevSeats,
            @NewSeatsPurchased = @SeatsPurchased,
            @PrevWorkspacesPurchased = @Workspaces,
            @NewWorkspacesPurchased = @Workspaces,
            @PrevProvider = @Prov,
            @NewProvider = @Prov,
            @PrevProviderSubscriptionId = @ProvSub,
            @NewProviderSubscriptionId = @ProvSub;
END;
GO

/* One-time snapshot so existing catalogs get a baseline row before subsequent mutations. */
IF OBJECT_ID(N'dbo.BillingSubscriptionStateHistory', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.BillingSubscriptions', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.BillingSubscriptionStateHistory)
        INSERT INTO dbo.BillingSubscriptionStateHistory (
            TenantId,
            WorkspaceId,
            ProjectId,
            ChangeKind,
            PrevStatus,
            NewStatus,
            PrevTier,
            NewTier,
            PrevSeatsPurchased,
            NewSeatsPurchased,
            PrevWorkspacesPurchased,
            NewWorkspacesPurchased,
            PrevProvider,
            NewProvider,
            PrevProviderSubscriptionId,
            NewProviderSubscriptionId)
        SELECT
            s.TenantId,
            s.WorkspaceId,
            s.ProjectId,
            N'Migration119Baseline',
            NULL,
            s.Status,
            NULL,
            s.Tier,
            NULL,
            s.SeatsPurchased,
            NULL,
            s.WorkspacesPurchased,
            NULL,
            s.Provider,
            NULL,
            s.ProviderSubscriptionId
        FROM dbo.BillingSubscriptions AS s;
END;
GO

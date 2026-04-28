/*
  Rollback 119: restore billing procs without state history; drop history table and helper proc.
*/

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.BillingSubscriptionStateHistory', N'U') IS NOT NULL
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        DROP FILTER PREDICATE ON dbo.BillingSubscriptionStateHistory,
        DROP BLOCK PREDICATE ON dbo.BillingSubscriptionStateHistory FOR AFTER INSERT,
        DROP BLOCK PREDICATE ON dbo.BillingSubscriptionStateHistory FOR AFTER UPDATE,
        DROP BLOCK PREDICATE ON dbo.BillingSubscriptionStateHistory FOR BEFORE DELETE;
END;
GO

IF OBJECT_ID(N'dbo.sp_Billing_AppendStateHistory', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Billing_AppendStateHistory;
GO

IF OBJECT_ID(N'dbo.BillingSubscriptionStateHistory', N'U') IS NOT NULL
    DROP TABLE dbo.BillingSubscriptionStateHistory;
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

CREATE OR ALTER PROCEDURE dbo.sp_Billing_ChangePlan
    @TenantId uniqueidentifier,
    @Tier nvarchar(32),
    @RawWebhookJson nvarchar(max)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.BillingSubscriptions
    SET Tier = @Tier,
        RawWebhookJson = @RawWebhookJson,
        UpdatedUtc = SYSUTCDATETIME()
    WHERE TenantId = @TenantId;
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

    UPDATE dbo.BillingSubscriptions
    SET SeatsPurchased = @SeatsPurchased,
        RawWebhookJson = @RawWebhookJson,
        UpdatedUtc = SYSUTCDATETIME()
    WHERE TenantId = @TenantId;
END;
GO

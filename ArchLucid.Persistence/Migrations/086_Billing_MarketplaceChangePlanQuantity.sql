/*
  086: Azure Marketplace ChangePlan / ChangeQuantity — persisted subscription row updates (see AzureMarketplaceBillingProvider).

  Least-privilege: mutations only via stored procedures (EXECUTE AS OWNER), same pattern as 078.
*/

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

IF DATABASE_PRINCIPAL_ID(N'ArchLucidApp') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'dbo.sp_Billing_ChangePlan', N'P') IS NOT NULL
        GRANT EXECUTE ON OBJECT::dbo.sp_Billing_ChangePlan TO [ArchLucidApp];

    IF OBJECT_ID(N'dbo.sp_Billing_ChangeQuantity', N'P') IS NOT NULL
        GRANT EXECUTE ON OBJECT::dbo.sp_Billing_ChangeQuantity TO [ArchLucidApp];
END;
GO

using System.Data;
using System.Globalization;

using ArchLucid.Core.Scoping;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Api.Tests.Billing;

/// <summary>Shared SQL seed + read helpers for Marketplace billing webhook HTTP tests.</summary>
internal static class BillingMarketplaceWebhookTestSeed
{
    public static async Task SeedTenantWithActiveBillingAsync(string connectionString, Guid tenantId)
    {
        string slug = "mkt_" + tenantId.ToString("N")[..16];

        await using SqlConnection connection = new(connectionString);

        await connection.OpenAsync();

        await using (SqlCommand insertTenant = connection.CreateCommand())
        {
            insertTenant.CommandText =
                """
                INSERT INTO dbo.Tenants (Id, Name, Slug, Tier)
                VALUES (@Id, N'Marketplace Webhook Test', @Slug, N'Standard');
                """;

            insertTenant.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = tenantId;
            insertTenant.Parameters.Add("@Slug", SqlDbType.NVarChar, 100).Value = slug;

            await insertTenant.ExecuteNonQueryAsync();
        }

        await using (SqlCommand insertBilling = connection.CreateCommand())
        {
            insertBilling.CommandText =
                """
                INSERT INTO dbo.BillingSubscriptions (
                    TenantId, WorkspaceId, ProjectId, Provider, ProviderSubscriptionId, Tier,
                    SeatsPurchased, WorkspacesPurchased, Status, ActivatedUtc, CanceledUtc, RawWebhookJson, CreatedUtc, UpdatedUtc)
                VALUES (
                    @TenantId, @WorkspaceId, @ProjectId, N'AzureMarketplace', N'sub-seed', N'Standard',
                    2, 1, N'Active', SYSUTCDATETIME(), NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
                """;

            insertBilling.Parameters.Add("@TenantId", SqlDbType.UniqueIdentifier).Value = tenantId;
            insertBilling.Parameters.Add("@WorkspaceId", SqlDbType.UniqueIdentifier).Value = ScopeIds.DefaultWorkspace;
            insertBilling.Parameters.Add("@ProjectId", SqlDbType.UniqueIdentifier).Value = ScopeIds.DefaultProject;

            await insertBilling.ExecuteNonQueryAsync();
        }
    }

    public static async Task<string> ReadBillingTierAsync(string connectionString, Guid tenantId)
    {
        await using SqlConnection connection = new(connectionString);

        await connection.OpenAsync();

        await using SqlCommand command = connection.CreateCommand();

        command.CommandText = "SELECT Tier FROM dbo.BillingSubscriptions WHERE TenantId = @TenantId;";
        command.Parameters.Add("@TenantId", SqlDbType.UniqueIdentifier).Value = tenantId;

        object? scalar = await command.ExecuteScalarAsync();

        return scalar is string s ? s : string.Empty;
    }

    public static async Task<int> ReadBillingSeatsAsync(string connectionString, Guid tenantId)
    {
        await using SqlConnection connection = new(connectionString);

        await connection.OpenAsync();

        await using SqlCommand command = connection.CreateCommand();

        command.CommandText = "SELECT SeatsPurchased FROM dbo.BillingSubscriptions WHERE TenantId = @TenantId;";
        command.Parameters.Add("@TenantId", SqlDbType.UniqueIdentifier).Value = tenantId;

        object? scalar = await command.ExecuteScalarAsync();

        return scalar is int i ? i : Convert.ToInt32(scalar, CultureInfo.InvariantCulture);
    }
}

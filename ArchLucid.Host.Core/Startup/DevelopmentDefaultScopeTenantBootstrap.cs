using ArchLucid.Core.Scoping;

using Dapper;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Host.Core.Startup;

/// <summary>
/// Ensures <see cref="ScopeIds"/> default tenant (and workspace) rows exist in SQL so Development hosts pass
/// <c>CommercialTenantTierFilter</c> for the well-known integration scope.
/// </summary>
public static class DevelopmentDefaultScopeTenantBootstrap
{
    /// <summary>Idempotent inserts for empty greenfield / integration catalogs.</summary>
    public static void TryEnsure(string connectionString, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        using SqlConnection connection = new(connectionString);
        connection.Open();
        ApplyRlsBypassSessionContext(connection);

        int tenantsTableExists = connection.QuerySingle<int>(
            "SELECT CASE WHEN OBJECT_ID(N'dbo.Tenants', N'U') IS NULL THEN 0 ELSE 1 END;");

        if (tenantsTableExists == 0)
            return;

        int tenantCount = connection.QuerySingle<int>(
            "SELECT COUNT(1) FROM dbo.Tenants WHERE Id = @TenantId;",
            new
            {
                TenantId = ScopeIds.DefaultTenant,
            });

        if (tenantCount == 0)
        {
            _ = connection.Execute(
                """
                INSERT INTO dbo.Tenants (Id, Name, Slug, Tier, EntraTenantId)
                VALUES (@TenantId, @TenantName, @TenantSlug, N'Standard', NULL);
                """,
                new
                {
                    TenantId = ScopeIds.DefaultTenant,
                    TenantName = "Development default tenant",
                    TenantSlug = "archlucid-dev-default-scope",
                });
        }

        int workspacesTableExists = connection.QuerySingle<int>(
            "SELECT CASE WHEN OBJECT_ID(N'dbo.TenantWorkspaces', N'U') IS NULL THEN 0 ELSE 1 END;");

        if (workspacesTableExists == 0)
            return;

        int workspaceCount = connection.QuerySingle<int>(
            "SELECT COUNT(1) FROM dbo.TenantWorkspaces WHERE Id = @WorkspaceId;",
            new
            {
                WorkspaceId = ScopeIds.DefaultWorkspace,
            });

        if (workspaceCount == 0)
        {
            _ = connection.Execute(
                """
                INSERT INTO dbo.TenantWorkspaces (Id, TenantId, Name, DefaultProjectId)
                VALUES (@WorkspaceId, @TenantId, @WorkspaceName, @DefaultProjectId);
                """,
                new
                {
                    WorkspaceId = ScopeIds.DefaultWorkspace,
                    TenantId = ScopeIds.DefaultTenant,
                    WorkspaceName = "Development default workspace",
                    DefaultProjectId = ScopeIds.DefaultProject,
                });
        }

        int verifyTenant = connection.QuerySingle<int>(
            "SELECT COUNT(1) FROM dbo.Tenants WHERE Id = @TenantId;",
            new
            {
                TenantId = ScopeIds.DefaultTenant,
            });

        if (verifyTenant != 1)
            throw new InvalidOperationException(
                "Development default tenant bootstrap failed: dbo.Tenants row for ScopeIds.DefaultTenant is missing after upsert.");

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Development default scope tenant/workspace ensured.");
    }

    /// <summary>RLS predicates may block bootstrap without an explicit bypass on this ADO.NET connection.</summary>
    private static void ApplyRlsBypassSessionContext(SqlConnection connection)
    {
        using SqlCommand command = connection.CreateCommand();
        command.CommandText = "EXEC sp_set_session_context @k, @v, @read_only;";
        command.Parameters.AddWithValue("@k", "af_rls_bypass");
        command.Parameters.AddWithValue("@v", 1);
        command.Parameters.AddWithValue("@read_only", 0);
        _ = command.ExecuteNonQuery();
    }
}

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Support;

/// <summary>
///     Sets <c>SESSION_CONTEXT</c> so integration tests can read/write <c>dbo.Runs</c> and authority-chain tables when
///     <c>rls.ArchLucidTenantScope</c> is enabled. Matches keys used by <c>rls.archlucid_scope_predicate</c>.
/// </summary>
internal static class PersistenceIntegrationTestRlsSession
{
    private static readonly string[] SessionKeys = ["al_rls_bypass", "al_tenant_id", "al_workspace_id", "al_project_id"];

    /// <summary>
    ///     Clears tenant keys then sets bypass plus the scope triple matching <paramref name="tenantId" />,
    ///     so FK checks against RLS-protected parents (<c>dbo.AlertRecords</c>, etc.) can see seeded rows reliably.
    /// </summary>
    internal static async Task ApplyArchLucidRlsBypassAndTenantScopeAsync(
        SqlConnection connection,
        CancellationToken ct,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId)
    {
        await ClearSessionKeysAsync(connection, ct);

        await SetIntSessionContextAsync(connection, ct, "al_rls_bypass", 1);
        await SetGuidSessionContextAsync(connection, ct, "al_tenant_id", tenantId);
        await SetGuidSessionContextAsync(connection, ct, "al_workspace_id", workspaceId);
        await SetGuidSessionContextAsync(connection, ct, "al_project_id", projectId);
    }

    /// <summary>
    ///     Clears scope keys and sets <c>al_rls_bypass</c> so the connection is not filtered by tenant predicates.
    /// </summary>
    internal static async Task ApplyArchLucidRlsBypassAsync(SqlConnection connection, CancellationToken ct)
    {
        await ClearSessionKeysAsync(connection, ct);

        await SetIntSessionContextAsync(connection, ct, "al_rls_bypass", 1);
    }

    private static async Task ClearSessionKeysAsync(SqlConnection connection, CancellationToken ct)
    {
        foreach (string key in SessionKeys)
        {
            await using SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = "EXEC sp_set_session_context @k, NULL, @read_only;";
            cmd.Parameters.AddWithValue("@k", key);
            cmd.Parameters.AddWithValue("@read_only", 0);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    private static async Task SetIntSessionContextAsync(SqlConnection connection, CancellationToken ct, string key, int value)
    {
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = "EXEC sp_set_session_context @k, @v, @read_only;";
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@v", value);
        cmd.Parameters.AddWithValue("@read_only", 0);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task SetGuidSessionContextAsync(SqlConnection connection, CancellationToken ct, string key, Guid value)
    {
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = "EXEC sp_set_session_context @k, @v, @read_only;";
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@v", value);
        cmd.Parameters.AddWithValue("@read_only", 0);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}

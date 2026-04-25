using System.Diagnostics.CodeAnalysis;

using ArchLucid.Core.Scoping;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Connections;

/// <inheritdoc cref="IRlsSessionContextApplicator" />
[ExcludeFromCodeCoverage(Justification =
    "Executes sp_set_session_context via SqlCommand; requires live SQL Server connection.")]
public sealed class RlsSessionContextApplicator(
    IScopeContextProvider scopeContextProvider,
    IOptionsMonitor<SqlServerOptions> optionsMonitor) : IRlsSessionContextApplicator
{
    private readonly IOptionsMonitor<SqlServerOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    /// <inheritdoc />
    public async Task ApplyAsync(SqlConnection connection, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(connection);

        SqlServerOptions opts = _optionsMonitor.CurrentValue;

        if (!opts.RowLevelSecurity.ApplySessionContext)
            return;

        if (SqlRowLevelSecurityBypassAmbient.IsActive)
        {
            await SetIntContextAsync(connection, "al_rls_bypass", 1, ct);
            return;
        }

        ScopeContext scope = _scopeContextProvider.GetCurrentScope();

        await SetIntContextAsync(connection, "al_rls_bypass", 0, ct);
        await SetGuidContextAsync(connection, "al_tenant_id", scope.TenantId, ct);
        await SetGuidContextAsync(connection, "al_workspace_id", scope.WorkspaceId, ct);
        await SetGuidContextAsync(connection, "al_project_id", scope.ProjectId, ct);
    }

    private static async Task SetIntContextAsync(SqlConnection connection, string key, int value, CancellationToken ct)
    {
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = "EXEC sp_set_session_context @k, @v, @read_only;";
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@v", value);
        cmd.Parameters.AddWithValue("@read_only", 0);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task SetGuidContextAsync(SqlConnection connection, string key, Guid value,
        CancellationToken ct)
    {
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = "EXEC sp_set_session_context @k, @v, @read_only;";
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@v", value);
        cmd.Parameters.AddWithValue("@read_only", 0);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}

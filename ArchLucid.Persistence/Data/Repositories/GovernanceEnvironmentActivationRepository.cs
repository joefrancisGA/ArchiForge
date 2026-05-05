using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Infrastructure;

using Dapper;

namespace ArchLucid.Persistence.Data.Repositories;

[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class GovernanceEnvironmentActivationRepository(
    IDbConnectionFactory connectionFactory,
    IScopeContextProvider scopeContextProvider)
    : IGovernanceEnvironmentActivationRepository
{
    public async Task CreateAsync(
        GovernanceEnvironmentActivation item,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (connection is not null && transaction is null)
            throw new ArgumentException(
                "A database transaction is required when a connection is supplied.",
                nameof(transaction));

        ApplyScopeToNewRow(item);

        const string sql = """
                           INSERT INTO dbo.GovernanceEnvironmentActivations
                           (
                               ActivationId,
                               RunId,
                               TenantId,
                               WorkspaceId,
                               ProjectId,
                               ManifestVersion,
                               Environment,
                               IsActive,
                               ActivatedUtc
                           )
                           VALUES
                           (
                               @ActivationId,
                               @RunId,
                               @TenantId,
                               @WorkspaceId,
                               @ProjectId,
                               @ManifestVersion,
                               @Environment,
                               @IsActive,
                               @ActivatedUtc
                           );
                           """;

        (IDbConnection conn, bool ownsConnection) =
            await ExternalDbConnection.ResolveAsync(connectionFactory, connection, cancellationToken);

        try
        {
            // Column is DATETIME2; default SqlClient param mapping uses legacy datetime and overflows near DateTime.MaxValue.
            DynamicParameters parameters = new();
            parameters.Add("ActivationId", item.ActivationId);
            parameters.Add("RunId", item.RunId);
            parameters.Add("TenantId", item.TenantId);
            parameters.Add("WorkspaceId", item.WorkspaceId);
            parameters.Add("ProjectId", item.ProjectId);
            parameters.Add("ManifestVersion", item.ManifestVersion);
            parameters.Add("Environment", item.Environment);
            parameters.Add("IsActive", item.IsActive);
            parameters.Add("ActivatedUtc", item.ActivatedUtc, DbType.DateTime2);

            await conn.ExecuteAsync(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: transaction,
                    commandTimeout: null,
                    commandType: null,
                    flags: CommandFlags.Buffered,
                    cancellationToken: cancellationToken));
        }
        finally
        {
            ExternalDbConnection.DisposeIfOwned(conn, ownsConnection);
        }
    }

    public async Task UpdateAsync(
        GovernanceEnvironmentActivation item,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(item);

        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        string scopeSql = RepositoryScopePredicate.AndTripleWhere(scope);

        string sql = $"""
                           UPDATE dbo.GovernanceEnvironmentActivations
                           SET IsActive = @IsActive
                           WHERE ActivationId = @ActivationId{scopeSql};
                           """;

        (IDbConnection conn, bool ownsConnection) =
            await ExternalDbConnection.ResolveAsync(connectionFactory, connection, cancellationToken);

        try
        {
            DynamicParameters p = new();
            p.Add("ActivationId", item.ActivationId);
            p.Add("IsActive", item.IsActive);
            RepositoryScopePredicate.AddScopeTripleIfNeeded(p, scope);

            await conn.ExecuteAsync(new CommandDefinition(
                sql,
                p,
                transaction,
                cancellationToken: cancellationToken));
        }
        finally
        {
            ExternalDbConnection.DisposeIfOwned(conn, ownsConnection);
        }
    }

    public async Task<IReadOnlyList<GovernanceEnvironmentActivation>> GetByEnvironmentAsync(
        string environment,
        CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        string scopeSql = RepositoryScopePredicate.AndTripleWhere(scope);

        string sql = $"""
                      SELECT
                          ActivationId,
                          RunId,
                          TenantId,
                          WorkspaceId,
                          ProjectId,
                          ManifestVersion,
                          Environment,
                          IsActive,
                          ActivatedUtc
                      FROM GovernanceEnvironmentActivations
                      WHERE Environment = @Environment{scopeSql}
                      ORDER BY ActivatedUtc DESC
                      {SqlPagingSyntax.FirstRowsOnly(200)};
                      """;

        DynamicParameters p = new();
        p.Add("Environment", environment);
        RepositoryScopePredicate.AddScopeTripleIfNeeded(p, scope);

        IEnumerable<GovernanceEnvironmentActivation> rows =
            await connection.QueryAsync<GovernanceEnvironmentActivation>(new CommandDefinition(
                sql,
                p,
                cancellationToken: cancellationToken));

        return [.. rows];
    }

    public async Task<IReadOnlyList<GovernanceEnvironmentActivation>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        string scopeSql = RepositoryScopePredicate.AndTripleWhere(scope);

        string sql = $"""
                      SELECT
                          ActivationId,
                          RunId,
                          TenantId,
                          WorkspaceId,
                          ProjectId,
                          ManifestVersion,
                          Environment,
                          IsActive,
                          ActivatedUtc
                      FROM dbo.GovernanceEnvironmentActivations
                      WHERE {RepositoryRunIdPredicate.WhereClauseMatching("RunId")}{scopeSql}
                      ORDER BY ActivatedUtc DESC
                      {SqlPagingSyntax.FirstRowsOnly(200)};
                      """;

        DynamicParameters p = new();

        RepositoryRunIdPredicate.AddRunIdMatchParameters(p, runId);

        RepositoryScopePredicate.AddScopeTripleIfNeeded(p, scope);

        IEnumerable<GovernanceEnvironmentActivation> rows =
            await connection.QueryAsync<GovernanceEnvironmentActivation>(new CommandDefinition(
                sql,
                p,
                cancellationToken: cancellationToken));

        return [.. rows];
    }

    private void ApplyScopeToNewRow(GovernanceEnvironmentActivation item)
    {
        ScopeContext ctx = scopeContextProvider.GetCurrentScope();

        if (ctx.TenantId == Guid.Empty)
            return;


        item.TenantId = ctx.TenantId;
        item.WorkspaceId = ctx.WorkspaceId;
        item.ProjectId = ctx.ProjectId;
    }
}

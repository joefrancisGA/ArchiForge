using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Infrastructure;

using Dapper;

namespace ArchLucid.Persistence.Data.Repositories;

[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class GovernancePromotionRecordRepository(
    IDbConnectionFactory connectionFactory,
    IScopeContextProvider scopeContextProvider)
    : IGovernancePromotionRecordRepository
{
    public async Task CreateAsync(
        GovernancePromotionRecord item,
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
                           INSERT INTO dbo.GovernancePromotionRecords
                           (
                               PromotionRecordId,
                               RunId,
                               TenantId,
                               WorkspaceId,
                               ProjectId,
                               ManifestVersion,
                               SourceEnvironment,
                               TargetEnvironment,
                               PromotedBy,
                               PromotedUtc,
                               ApprovalRequestId,
                               Notes
                           )
                           VALUES
                           (
                               @PromotionRecordId,
                               @RunId,
                               @TenantId,
                               @WorkspaceId,
                               @ProjectId,
                               @ManifestVersion,
                               @SourceEnvironment,
                               @TargetEnvironment,
                               @PromotedBy,
                               @PromotedUtc,
                               @ApprovalRequestId,
                               @Notes
                           );
                           """;

        (IDbConnection conn, bool ownsConnection) =
            await ExternalDbConnection.ResolveAsync(connectionFactory, connection, cancellationToken);

        try
        {
            // Column is DATETIME2; default SqlClient mapping uses legacy datetime and collapses sub-ms values near DateTime.MaxValue.
            DynamicParameters parameters = new();
            parameters.Add("PromotionRecordId", item.PromotionRecordId);
            parameters.Add("RunId", item.RunId);
            parameters.Add("TenantId", item.TenantId);
            parameters.Add("WorkspaceId", item.WorkspaceId);
            parameters.Add("ProjectId", item.ProjectId);
            parameters.Add("ManifestVersion", item.ManifestVersion);
            parameters.Add("SourceEnvironment", item.SourceEnvironment);
            parameters.Add("TargetEnvironment", item.TargetEnvironment);
            parameters.Add("PromotedBy", item.PromotedBy);
            parameters.Add("PromotedUtc", item.PromotedUtc, DbType.DateTime2);
            parameters.Add("ApprovalRequestId", item.ApprovalRequestId);
            parameters.Add("Notes", item.Notes);

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

    public async Task<IReadOnlyList<GovernancePromotionRecord>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        string scopeSql = RepositoryScopePredicate.AndTripleWhere(scope);

        string sql = $"""
                      SELECT
                          PromotionRecordId,
                          RunId,
                          TenantId,
                          WorkspaceId,
                          ProjectId,
                          ManifestVersion,
                          SourceEnvironment,
                          TargetEnvironment,
                          PromotedBy,
                          PromotedUtc,
                          ApprovalRequestId,
                          Notes
                      FROM GovernancePromotionRecords
                      WHERE {RepositoryRunIdPredicate.WhereClauseMatching("RunId")}{scopeSql}
                      ORDER BY PromotedUtc DESC
                      {SqlPagingSyntax.FirstRowsOnly(200)};
                      """;

        DynamicParameters p = new();

        RepositoryRunIdPredicate.AddRunIdMatchParameters(p, runId);

        RepositoryScopePredicate.AddScopeTripleIfNeeded(p, scope);

        IEnumerable<GovernancePromotionRecord> rows = await connection.QueryAsync<GovernancePromotionRecord>(
            new CommandDefinition(
                sql,
                p,
                cancellationToken: cancellationToken));

        return [.. rows];
    }

    private void ApplyScopeToNewRow(GovernancePromotionRecord item)
    {
        ScopeContext ctx = scopeContextProvider.GetCurrentScope();

        if (ctx.TenantId == Guid.Empty)
            return;


        item.TenantId = ctx.TenantId;
        item.WorkspaceId = ctx.WorkspaceId;
        item.ProjectId = ctx.ProjectId;
    }
}

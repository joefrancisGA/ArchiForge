using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Infrastructure;

using Dapper;

namespace ArchLucid.Persistence.Data.Repositories;

[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class GovernanceApprovalRequestRepository(
    IDbConnectionFactory connectionFactory,
    IScopeContextProvider scopeContextProvider)
    : IGovernanceApprovalRequestRepository
{
    public async Task CreateAsync(
        GovernanceApprovalRequest item,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(item);

        ApplyScopeToNewRow(item);

        const string sql = """
                           INSERT INTO GovernanceApprovalRequests
                           (
                               ApprovalRequestId,
                               RunId,
                               TenantId,
                               WorkspaceId,
                               ProjectId,
                               ManifestVersion,
                               SourceEnvironment,
                               TargetEnvironment,
                               Status,
                               RequestedBy,
                               RequestedByActorKey,
                               ReviewedBy,
                               ReviewedByActorKey,
                               RequestComment,
                               ReviewComment,
                               RequestedUtc,
                               ReviewedUtc,
                               SlaDeadlineUtc,
                               SlaBreachNotifiedUtc
                           )
                           VALUES
                           (
                               @ApprovalRequestId,
                               @RunId,
                               @TenantId,
                               @WorkspaceId,
                               @ProjectId,
                               @ManifestVersion,
                               @SourceEnvironment,
                               @TargetEnvironment,
                               @Status,
                               @RequestedBy,
                               @RequestedByActorKey,
                               @ReviewedBy,
                               @ReviewedByActorKey,
                               @RequestComment,
                               @ReviewComment,
                               @RequestedUtc,
                               @ReviewedUtc,
                               @SlaDeadlineUtc,
                               @SlaBreachNotifiedUtc
                           );
                           """;

        (IDbConnection conn, bool ownsConnection) =
            await ExternalDbConnection.ResolveAsync(connectionFactory, connection, cancellationToken);

        try
        {
            // Columns are DATETIME2; default SqlClient mapping uses legacy datetime and overflows near DateTime.MaxValue
            // (contract tests use ceiling ticks for stable ORDER BY).
            DynamicParameters parameters = new();
            parameters.Add("ApprovalRequestId", item.ApprovalRequestId);
            parameters.Add("RunId", item.RunId);
            parameters.Add("TenantId", item.TenantId);
            parameters.Add("WorkspaceId", item.WorkspaceId);
            parameters.Add("ProjectId", item.ProjectId);
            parameters.Add("ManifestVersion", item.ManifestVersion);
            parameters.Add("SourceEnvironment", item.SourceEnvironment);
            parameters.Add("TargetEnvironment", item.TargetEnvironment);
            parameters.Add("Status", item.Status);
            parameters.Add("RequestedBy", item.RequestedBy);
            parameters.Add("RequestedByActorKey", item.RequestedByActorKey);
            parameters.Add("ReviewedBy", item.ReviewedBy);
            parameters.Add("ReviewedByActorKey", item.ReviewedByActorKey);
            parameters.Add("RequestComment", item.RequestComment);
            parameters.Add("ReviewComment", item.ReviewComment);
            parameters.Add("RequestedUtc", item.RequestedUtc, DbType.DateTime2);
            parameters.Add("ReviewedUtc", item.ReviewedUtc, DbType.DateTime2);
            parameters.Add("SlaDeadlineUtc", item.SlaDeadlineUtc, DbType.DateTime2);
            parameters.Add("SlaBreachNotifiedUtc", item.SlaBreachNotifiedUtc, DbType.DateTime2);

            // Named CommandDefinition slots so Dapper always enlists transaction (positional + cancellationToken can
            // mis-bind overloads on some SDKs; unenlisted INSERTs break FK checks vs same-session MERGE).
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

    /// <inheritdoc />
    public async Task<bool> TryTransitionFromReviewableAsync(
        string approvalRequestId,
        string newStatus,
        string reviewedBy,
        string? reviewedByActorKey,
        string? reviewComment,
        DateTime reviewedUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalRequestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(newStatus);
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedBy);

        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        string scopeSql = RepositoryScopePredicate.AndTripleWhere(scope);

        // @@ROWCOUNT batch: pooled sessions often inherit SET NOCOUNT ON, so ExecuteAsync's return value is unreliable
        // for matched-row detection under concurrent Serializable transitions (contract test expects exactly one winner).
        string updateSql = $"""
                                 UPDATE dbo.GovernanceApprovalRequests
                                 SET
                                     Status = @NewStatus,
                                     ReviewedBy = @ReviewedBy,
                                     ReviewedByActorKey = @ReviewedByActorKey,
                                     ReviewComment = @ReviewComment,
                                     ReviewedUtc = @ReviewedUtc
                                 WHERE ApprovalRequestId = @ApprovalRequestId
                                   AND (Status = @Draft OR Status = @Submitted){scopeSql};
                                 SELECT @@ROWCOUNT;
                                 """;

        DynamicParameters transitionParams = new();
        transitionParams.Add("ApprovalRequestId", approvalRequestId);
        transitionParams.Add("NewStatus", newStatus);
        transitionParams.Add("ReviewedBy", reviewedBy);
        transitionParams.Add("ReviewedByActorKey", reviewedByActorKey);
        transitionParams.Add("ReviewComment", reviewComment);
        transitionParams.Add("ReviewedUtc", reviewedUtc, DbType.DateTime2);
        transitionParams.Add("Draft", GovernanceApprovalStatus.Draft);
        transitionParams.Add("Submitted", GovernanceApprovalStatus.Submitted);
        RepositoryScopePredicate.AddScopeTripleIfNeeded(transitionParams, scope);

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using IDbTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            int affected = await connection.QuerySingleAsync<int>(
                new CommandDefinition(updateSql, transitionParams, transaction, cancellationToken: cancellationToken));

            transaction.Commit();

            return affected == 1;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateAsync(GovernanceApprovalRequest item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        string scopeSql = RepositoryScopePredicate.AndTripleWhere(scope);

        string sql = $"""
                           UPDATE GovernanceApprovalRequests
                           SET
                               Status = @Status,
                               ReviewedBy = @ReviewedBy,
                               ReviewComment = @ReviewComment,
                               ReviewedUtc = @ReviewedUtc,
                               SlaDeadlineUtc = @SlaDeadlineUtc,
                               SlaBreachNotifiedUtc = @SlaBreachNotifiedUtc
                           WHERE ApprovalRequestId = @ApprovalRequestId{scopeSql};
                           """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        DynamicParameters p = new();
        p.Add("ApprovalRequestId", item.ApprovalRequestId);
        p.Add("Status", item.Status);
        p.Add("ReviewedBy", item.ReviewedBy);
        p.Add("ReviewComment", item.ReviewComment);
        p.Add("ReviewedUtc", item.ReviewedUtc, DbType.DateTime2);
        p.Add("SlaDeadlineUtc", item.SlaDeadlineUtc, DbType.DateTime2);
        p.Add("SlaBreachNotifiedUtc", item.SlaBreachNotifiedUtc, DbType.DateTime2);
        RepositoryScopePredicate.AddScopeTripleIfNeeded(p, scope);

        await connection.ExecuteAsync(new CommandDefinition(sql, p, cancellationToken: cancellationToken));
    }

    public async Task<GovernanceApprovalRequest?> GetByIdAsync(
        string approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        string scopeSql = RepositoryScopePredicate.AndTripleWhere(scope);

        string sql = $"""
                           SELECT
                               ApprovalRequestId,
                               RunId,
                               TenantId,
                               WorkspaceId,
                               ProjectId,
                               ManifestVersion,
                               SourceEnvironment,
                               TargetEnvironment,
                               Status,
                               RequestedBy,
                               RequestedByActorKey,
                               ReviewedBy,
                               ReviewedByActorKey,
                               RequestComment,
                               ReviewComment,
                               RequestedUtc,
                               ReviewedUtc,
                               SlaDeadlineUtc,
                               SlaBreachNotifiedUtc
                           FROM GovernanceApprovalRequests
                           WHERE ApprovalRequestId = @ApprovalRequestId{scopeSql};
                           """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        DynamicParameters p = new DynamicParameters();
        p.Add("ApprovalRequestId", approvalRequestId);
        RepositoryScopePredicate.AddScopeTripleIfNeeded(p, scope);

        return await connection.QuerySingleOrDefaultAsync<GovernanceApprovalRequest>(new CommandDefinition(
            sql,
            p,
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<GovernanceApprovalRequest>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        string scopeSql = RepositoryScopePredicate.AndTripleWhere(scope);

        string sql = $"""
                      SELECT
                          ApprovalRequestId,
                          RunId,
                          TenantId,
                          WorkspaceId,
                          ProjectId,
                          ManifestVersion,
                          SourceEnvironment,
                          TargetEnvironment,
                          Status,
                          RequestedBy,
                          RequestedByActorKey,
                          ReviewedBy,
                          ReviewedByActorKey,
                          RequestComment,
                          ReviewComment,
                          RequestedUtc,
                          ReviewedUtc,
                          SlaDeadlineUtc,
                          SlaBreachNotifiedUtc
                      FROM GovernanceApprovalRequests
                      WHERE {RepositoryRunIdPredicate.WhereClauseMatching("RunId")}{scopeSql}
                      ORDER BY RequestedUtc DESC
                      {SqlPagingSyntax.FirstRowsOnly(200)};
                      """;

        DynamicParameters p = new();

        RepositoryRunIdPredicate.AddRunIdMatchParameters(p, runId);

        RepositoryScopePredicate.AddScopeTripleIfNeeded(p, scope);

        IEnumerable<GovernanceApprovalRequest> rows = await connection.QueryAsync<GovernanceApprovalRequest>(
            new CommandDefinition(
                sql,
                p,
                cancellationToken: cancellationToken));

        return [.. rows];
    }

    public async Task<IReadOnlyList<GovernanceApprovalRequest>> GetPendingAsync(
        int maxRows = 50,
        CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxRows));


        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        string scopeSql = RepositoryScopePredicate.AndTripleWhere(scope);

        string sql = $"""
                           SELECT TOP (@MaxRows)
                               ApprovalRequestId,
                               RunId,
                               TenantId,
                               WorkspaceId,
                               ProjectId,
                               ManifestVersion,
                               SourceEnvironment,
                               TargetEnvironment,
                               Status,
                               RequestedBy,
                               RequestedByActorKey,
                               ReviewedBy,
                               ReviewedByActorKey,
                               RequestComment,
                               ReviewComment,
                               RequestedUtc,
                               ReviewedUtc,
                               SlaDeadlineUtc,
                               SlaBreachNotifiedUtc
                           FROM GovernanceApprovalRequests
                           WHERE Status IN (@Draft, @Submitted){scopeSql}
                           ORDER BY RequestedUtc DESC;
                           """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        DynamicParameters p = new();
        p.Add("MaxRows", maxRows);
        p.Add("Draft", GovernanceApprovalStatus.Draft);
        p.Add("Submitted", GovernanceApprovalStatus.Submitted);
        RepositoryScopePredicate.AddScopeTripleIfNeeded(p, scope);

        IEnumerable<GovernanceApprovalRequest> rows = await connection.QueryAsync<GovernanceApprovalRequest>(
            new CommandDefinition(
                sql,
                p,
                cancellationToken: cancellationToken));

        return [.. rows];
    }

    public async Task<IReadOnlyList<GovernanceApprovalRequest>> GetRecentDecisionsAsync(
        int maxRows = 50,
        CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxRows));


        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        string scopeSql = RepositoryScopePredicate.AndTripleWhere(scope);

        string sql = $"""
                           SELECT TOP (@MaxRows)
                               ApprovalRequestId,
                               RunId,
                               TenantId,
                               WorkspaceId,
                               ProjectId,
                               ManifestVersion,
                               SourceEnvironment,
                               TargetEnvironment,
                               Status,
                               RequestedBy,
                               RequestedByActorKey,
                               ReviewedBy,
                               ReviewedByActorKey,
                               RequestComment,
                               ReviewComment,
                               RequestedUtc,
                               ReviewedUtc,
                               SlaDeadlineUtc,
                               SlaBreachNotifiedUtc
                           FROM GovernanceApprovalRequests
                           WHERE Status IN (@Approved, @Rejected, @Promoted)
                             AND ReviewedUtc IS NOT NULL{scopeSql}
                           ORDER BY ReviewedUtc DESC, ApprovalRequestId DESC;
                           """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        DynamicParameters p = new();
        p.Add("MaxRows", maxRows);
        p.Add("Approved", GovernanceApprovalStatus.Approved);
        p.Add("Rejected", GovernanceApprovalStatus.Rejected);
        p.Add("Promoted", GovernanceApprovalStatus.Promoted);
        RepositoryScopePredicate.AddScopeTripleIfNeeded(p, scope);

        IEnumerable<GovernanceApprovalRequest> rows = await connection.QueryAsync<GovernanceApprovalRequest>(
            new CommandDefinition(
                sql,
                p,
                cancellationToken: cancellationToken));

        return [.. rows];
    }

    public async Task<IReadOnlyList<GovernanceApprovalRequest>> GetPendingSlaBreachedAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        string scopeSql = RepositoryScopePredicate.AndTripleWhere(scope);

        string sql = $"""
                           SELECT TOP 200
                               ApprovalRequestId,
                               RunId,
                               TenantId,
                               WorkspaceId,
                               ProjectId,
                               ManifestVersion,
                               SourceEnvironment,
                               TargetEnvironment,
                               Status,
                               RequestedBy,
                               RequestedByActorKey,
                               ReviewedBy,
                               ReviewedByActorKey,
                               RequestComment,
                               ReviewComment,
                               RequestedUtc,
                               ReviewedUtc,
                               SlaDeadlineUtc,
                               SlaBreachNotifiedUtc
                           FROM GovernanceApprovalRequests
                           WHERE Status IN (@Draft, @Submitted)
                             AND SlaDeadlineUtc IS NOT NULL
                             AND SlaDeadlineUtc <= @UtcNow
                             AND SlaBreachNotifiedUtc IS NULL{scopeSql}
                           ORDER BY SlaDeadlineUtc ASC;
                           """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        DynamicParameters p = new();
        p.Add("UtcNow", utcNow, DbType.DateTime2);
        p.Add("Draft", GovernanceApprovalStatus.Draft);
        p.Add("Submitted", GovernanceApprovalStatus.Submitted);
        RepositoryScopePredicate.AddScopeTripleIfNeeded(p, scope);

        IEnumerable<GovernanceApprovalRequest> rows = await connection.QueryAsync<GovernanceApprovalRequest>(
            new CommandDefinition(
                sql,
                p,
                cancellationToken: cancellationToken));

        return [.. rows];
    }

    public async Task PatchSlaBreachNotifiedAsync(
        string approvalRequestId,
        DateTime slaBreachNotifiedUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalRequestId);

        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        string scopeSql = RepositoryScopePredicate.AndTripleWhere(scope);

        string sql = $"""
                           UPDATE GovernanceApprovalRequests
                           SET SlaBreachNotifiedUtc = @SlaBreachNotifiedUtc
                           WHERE ApprovalRequestId = @ApprovalRequestId{scopeSql};
                           """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        DynamicParameters p = new();
        p.Add("ApprovalRequestId", approvalRequestId);
        p.Add("SlaBreachNotifiedUtc", slaBreachNotifiedUtc, DbType.DateTime2);
        RepositoryScopePredicate.AddScopeTripleIfNeeded(p, scope);

        await connection.ExecuteAsync(new CommandDefinition(sql, p, cancellationToken: cancellationToken));
    }

    private void ApplyScopeToNewRow(GovernanceApprovalRequest item)
    {
        ScopeContext ctx = scopeContextProvider.GetCurrentScope();

        if (ctx.TenantId == Guid.Empty)
            return;


        item.TenantId = ctx.TenantId;
        item.WorkspaceId = ctx.WorkspaceId;
        item.ProjectId = ctx.ProjectId;
    }
}

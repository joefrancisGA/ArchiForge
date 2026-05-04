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
    public async Task CreateAsync(GovernanceApprovalRequest item, CancellationToken cancellationToken = default)
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

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                item.ApprovalRequestId,
                item.RunId,
                item.TenantId,
                item.WorkspaceId,
                item.ProjectId,
                item.ManifestVersion,
                item.SourceEnvironment,
                item.TargetEnvironment,
                item.Status,
                item.RequestedBy,
                item.RequestedByActorKey,
                item.ReviewedBy,
                item.ReviewedByActorKey,
                item.RequestComment,
                item.ReviewComment,
                item.RequestedUtc,
                item.ReviewedUtc,
                item.SlaDeadlineUtc,
                item.SlaBreachNotifiedUtc
            },
            cancellationToken: cancellationToken));
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
                                 """;

        DynamicParameters transitionParams = new();
        transitionParams.Add("ApprovalRequestId", approvalRequestId);
        transitionParams.Add("NewStatus", newStatus);
        transitionParams.Add("ReviewedBy", reviewedBy);
        transitionParams.Add("ReviewedByActorKey", reviewedByActorKey);
        transitionParams.Add("ReviewComment", reviewComment);
        transitionParams.Add("ReviewedUtc", reviewedUtc);
        transitionParams.Add("Draft", GovernanceApprovalStatus.Draft);
        transitionParams.Add("Submitted", GovernanceApprovalStatus.Submitted);
        RepositoryScopePredicate.AddScopeTripleIfNeeded(transitionParams, scope);

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using IDbTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            // Pooled sessions can inherit SET NOCOUNT ON from other callers; ExecuteNonQuery then returns -1
            // regardless of matched rows which breaks concurrency tests (winner rowcount checks).
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "SET NOCOUNT OFF;",
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            int affected = await connection.ExecuteAsync(
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
        p.Add("ReviewedUtc", item.ReviewedUtc);
        p.Add("SlaDeadlineUtc", item.SlaDeadlineUtc);
        p.Add("SlaBreachNotifiedUtc", item.SlaBreachNotifiedUtc);
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
                      WHERE RunId = @RunId{scopeSql}
                      ORDER BY RequestedUtc DESC
                      {SqlPagingSyntax.FirstRowsOnly(200)};
                      """;

        DynamicParameters p = new();
        p.Add("RunId", runId);
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
                           ORDER BY ReviewedUtc DESC;
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
        p.Add("UtcNow", utcNow);
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
        p.Add("SlaBreachNotifiedUtc", slaBreachNotifiedUtc);
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

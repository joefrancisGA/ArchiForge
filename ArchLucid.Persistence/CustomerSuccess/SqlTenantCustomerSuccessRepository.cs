using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Core.CustomerSuccess;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.CustomerSuccess;

/// <summary>
///     SQL-backed health scores and feedback. Maintenance path uses <see cref="SqlRowLevelSecurityBypassAmbient" />
///     together with <see cref="dbo.sp_TenantHealthScores_BatchRefresh" />.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL Server–dependent repository.")]
public sealed class SqlTenantCustomerSuccessRepository(
    ISqlConnectionFactory connectionFactory,
    IRlsSessionContextApplicator rlsSessionContextApplicator) : ITenantCustomerSuccessRepository
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    private readonly IRlsSessionContextApplicator _rlsSessionContextApplicator =
        rlsSessionContextApplicator ?? throw new ArgumentNullException(nameof(rlsSessionContextApplicator));

    /// <inheritdoc />
    public async Task<TenantHealthScoreRecord?> GetHealthScoreAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        await _rlsSessionContextApplicator.ApplyAsync(connection, ct);

        const string sql = """
                           SELECT TenantId,
                                  EngagementScore,
                                  BreadthScore,
                                  QualityScore,
                                  GovernanceScore,
                                  SupportScore,
                                  CompositeScore,
                                  UpdatedUtc
                           FROM dbo.TenantHealthScores
                           WHERE TenantId = @TenantId
                             AND WorkspaceId = @WorkspaceId
                             AND ProjectId = @ProjectId;
                           """;

        TenantHealthScoreSqlRow? row = await connection.QuerySingleOrDefaultAsync<TenantHealthScoreSqlRow>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId, WorkspaceId = workspaceId, ProjectId = projectId },
                cancellationToken: ct));

        if (row is null)
            return null;

        return new TenantHealthScoreRecord(
            row.TenantId,
            row.EngagementScore,
            row.BreadthScore,
            row.QualityScore,
            row.GovernanceScore,
            row.SupportScore,
            row.CompositeScore,
            new DateTimeOffset(row.UpdatedUtc, TimeSpan.Zero));
    }

    /// <inheritdoc />
    public async Task InsertProductFeedbackAsync(ProductFeedbackSubmission submission, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(submission);

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        await _rlsSessionContextApplicator.ApplyAsync(connection, ct);

        const string sql = """
                           INSERT INTO dbo.ProductFeedback (
                               FeedbackId,
                               TenantId, WorkspaceId, ProjectId,
                               FindingRef, RunId, Score, CommentText, CreatedUtc)
                           VALUES (
                               @FeedbackId,
                               @TenantId, @WorkspaceId, @ProjectId,
                               @FindingRef, @RunId, @Score, @CommentText, SYSUTCDATETIME());
                           """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    FeedbackId = Guid.NewGuid(),
                    submission.TenantId,
                    submission.WorkspaceId,
                    submission.ProjectId,
                    submission.FindingRef,
                    submission.RunId,
                    submission.Score,
                    CommentText = submission.Comment
                },
                cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task RefreshAllTenantHealthScoresAsync(CancellationToken ct)
    {
        using IDisposable _ = SqlRowLevelSecurityBypassAmbient.Enter();

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        await _rlsSessionContextApplicator.ApplyAsync(connection, ct);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.sp_TenantHealthScores_BatchRefresh",
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    private sealed record TenantHealthScoreSqlRow(
        Guid TenantId,
        decimal EngagementScore,
        decimal BreadthScore,
        decimal QualityScore,
        decimal GovernanceScore,
        decimal SupportScore,
        decimal CompositeScore,
        DateTime UpdatedUtc);
}

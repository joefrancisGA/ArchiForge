using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.Core.CustomerSuccess;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.CustomerSuccess;

/// <summary>
///     SQL-backed health scores and feedback. Maintenance path uses <see cref="SqlRowLevelSecurityBypassAmbient" />
///     together with <see cref="dbo.sp_TenantHealthScores_Upsert" />.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL Server–dependent repository.")]
public sealed class SqlTenantCustomerSuccessRepository(
    ISqlConnectionFactory connectionFactory,
    IRlsSessionContextApplicator rlsSessionContextApplicator,
    ITenantRepository tenantRepository) : ITenantCustomerSuccessRepository
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    private readonly IRlsSessionContextApplicator _rlsSessionContextApplicator =
        rlsSessionContextApplicator ?? throw new ArgumentNullException(nameof(rlsSessionContextApplicator));

    private readonly ITenantRepository _tenantRepository =
        tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));

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

        return new TenantHealthScoreRecord
        {
            TenantId = row.TenantId,
            EngagementScore = row.EngagementScore,
            BreadthScore = row.BreadthScore,
            QualityScore = row.QualityScore,
            GovernanceScore = row.GovernanceScore,
            SupportScore = row.SupportScore,
            CompositeScore = row.CompositeScore,
            UpdatedUtc = new DateTimeOffset(row.UpdatedUtc, TimeSpan.Zero)
        };
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

        IReadOnlyList<TenantRecord> tenants = await _tenantRepository.ListAsync(ct);

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        await _rlsSessionContextApplicator.ApplyAsync(connection, ct);

        foreach (TenantRecord tenant in tenants)
        {
            TenantWorkspaceLink? link = await _tenantRepository.GetFirstWorkspaceAsync(tenant.Id, ct);

            if (link is null)
                continue;

            int runs7d = await CountRunsLastSevenDaysAsync(
                    connection,
                    tenant.Id,
                    link.WorkspaceId,
                    link.DefaultProjectId,
                    ct)
                .ConfigureAwait(false);

            decimal engagement = EngagementFromRunCount(runs7d);

            // Phase-1: other dimensions default to neutral until telemetry wiring lands (see docs/go-to-market/CUSTOMER_HEALTH_SCORING.md).
            decimal breadth = 3.0M;
            decimal quality = 3.0M;
            decimal governance = 3.0M;
            decimal support = 3.0M;

            decimal composite = CompositeScore(engagement, breadth, quality, governance, support);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "dbo.sp_TenantHealthScores_Upsert",
                    new
                    {
                        TenantId = tenant.Id,
                        link.WorkspaceId,
                        ProjectId = link.DefaultProjectId,
                        EngagementScore = engagement,
                        BreadthScore = breadth,
                        QualityScore = quality,
                        GovernanceScore = governance,
                        SupportScore = support,
                        CompositeScore = composite
                    },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: ct));
        }
    }

    private static async Task<int> CountRunsLastSevenDaysAsync(
        SqlConnection connection,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        const string sql = """
                           SELECT COUNT_BIG(1)
                           FROM dbo.Runs
                           WHERE TenantId = @TenantId
                             AND WorkspaceId = @WorkspaceId
                             AND ScopeProjectId = @ProjectId
                             AND ArchivedUtc IS NULL
                             AND CreatedUtc >= DATEADD(DAY, -7, SYSUTCDATETIME());
                           """;

        long count = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId, WorkspaceId = workspaceId, ProjectId = projectId },
                cancellationToken: ct));

        return (int)Math.Min(int.MaxValue, count);
    }

    private static decimal EngagementFromRunCount(int runs7d)
    {
        return runs7d switch
        {
            0 => 1.0M,
            <= 2 => 2.0M,
            <= 5 => 3.0M,
            <= 9 => 4.0M,
            _ => 5.0M
        };
    }

    private static decimal CompositeScore(
        decimal engagement,
        decimal breadth,
        decimal quality,
        decimal governance,
        decimal support)
    {
        return Math.Round(
            0.30M * engagement
            + 0.20M * breadth
            + 0.15M * quality
            + 0.20M * governance
            + 0.15M * support,
            2,
            MidpointRounding.AwayFromZero);
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

using ArchiForge.Contracts.ProductLearning;
using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.ProductLearning;

/// <summary>Dapper access to <c>dbo.ProductLearningPilotSignals</c>.</summary>
public sealed class DapperProductLearningPilotSignalRepository(ISqlConnectionFactory connectionFactory)
    : IProductLearningPilotSignalRepository
{
    private const int MaxTake = 500;

    public async Task InsertAsync(ProductLearningPilotSignalRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (string.IsNullOrWhiteSpace(record.SubjectType))
        {
            throw new ArgumentException("SubjectType is required.", nameof(record));
        }

        if (string.IsNullOrWhiteSpace(record.Disposition))
        {
            throw new ArgumentException("Disposition is required.", nameof(record));
        }

        Guid signalId = record.SignalId == Guid.Empty ? Guid.NewGuid() : record.SignalId;
        DateTime recordedUtc = record.RecordedUtc == default ? DateTime.UtcNow : record.RecordedUtc;
        string triage = string.IsNullOrWhiteSpace(record.TriageStatus)
            ? ProductLearningTriageStatusValues.Open
            : record.TriageStatus;

        const string sql = """
            INSERT INTO dbo.ProductLearningPilotSignals
            (
                SignalId,
                TenantId,
                WorkspaceId,
                ProjectId,
                ArchitectureRunId,
                AuthorityRunId,
                ManifestVersion,
                SubjectType,
                Disposition,
                PatternKey,
                ArtifactHint,
                CommentShort,
                DetailJson,
                RecordedByUserId,
                RecordedByDisplayName,
                RecordedUtc,
                TriageStatus
            )
            VALUES
            (
                @SignalId,
                @TenantId,
                @WorkspaceId,
                @ProjectId,
                @ArchitectureRunId,
                @AuthorityRunId,
                @ManifestVersion,
                @SubjectType,
                @Disposition,
                @PatternKey,
                @ArtifactHint,
                @CommentShort,
                @DetailJson,
                @RecordedByUserId,
                @RecordedByDisplayName,
                @RecordedUtc,
                @TriageStatus
            );
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    SignalId = signalId,
                    record.TenantId,
                    record.WorkspaceId,
                    record.ProjectId,
                    ArchitectureRunId = record.ArchitectureRunId,
                    AuthorityRunId = record.AuthorityRunId,
                    ManifestVersion = record.ManifestVersion,
                    record.SubjectType,
                    record.Disposition,
                    PatternKey = record.PatternKey,
                    ArtifactHint = record.ArtifactHint,
                    CommentShort = record.CommentShort,
                    DetailJson = record.DetailJson,
                    RecordedByUserId = record.RecordedByUserId,
                    RecordedByDisplayName = record.RecordedByDisplayName,
                    RecordedUtc = recordedUtc,
                    TriageStatus = triage,
                },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<ProductLearningPilotSignalRecord>> ListRecentForScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken cancellationToken)
    {
        int capped = take < 1 ? 1 : Math.Min(take, MaxTake);

        const string sql = """
            SELECT TOP (@Take)
                SignalId,
                TenantId,
                WorkspaceId,
                ProjectId,
                ArchitectureRunId,
                AuthorityRunId,
                ManifestVersion,
                SubjectType,
                Disposition,
                PatternKey,
                ArtifactHint,
                CommentShort,
                DetailJson,
                RecordedByUserId,
                RecordedByDisplayName,
                RecordedUtc,
                TriageStatus
            FROM dbo.ProductLearningPilotSignals
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
            ORDER BY RecordedUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        IEnumerable<ProductLearningPilotSignalRecord> rows = await connection.QueryAsync<ProductLearningPilotSignalRecord>(
            new CommandDefinition(
                sql,
                new { Take = capped, TenantId = tenantId, WorkspaceId = workspaceId, ProjectId = projectId },
                cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<IReadOnlyList<FeedbackAggregate>> ListRunFeedbackAggregatesAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime? sinceUtc,
        int maxAggregates,
        CancellationToken cancellationToken)
    {
        int cap = maxAggregates < 1 ? 1 : Math.Min(maxAggregates, 500);

        const string sql = """
            WITH Scoped AS (
                SELECT *
                FROM dbo.ProductLearningPilotSignals
                WHERE TenantId = @TenantId
                  AND WorkspaceId = @WorkspaceId
                  AND ProjectId = @ProjectId
                  AND (@SinceUtc IS NULL OR RecordedUtc >= @SinceUtc)
            ),
            Agg AS (
                SELECT
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM(ISNULL(PatternKey, N''))), N'') IS NOT NULL
                            THEN LTRIM(RTRIM(PatternKey))
                        ELSE CONCAT(
                                N'subject:',
                                SubjectType,
                                N'|artifact:',
                                COALESCE(NULLIF(LTRIM(RTRIM(ArtifactHint)), N''), N'--'))
                    END AS AggregateKey,
                    MIN(PatternKey) AS PatternKeyRaw,
                    MIN(SubjectType) AS SubjectTypeOrWorkflowArea,
                    COUNT_BIG(*) AS TotalSignalCount,
                    COUNT(DISTINCT CASE
                        WHEN ArchitectureRunId IS NOT NULL AND LTRIM(RTRIM(ArchitectureRunId)) <> N''
                            THEN ArchitectureRunId
                        END) AS DistinctRunCount,
                    SUM(CASE WHEN Disposition = N'Trusted' THEN 1 ELSE 0 END) AS TrustedCount,
                    SUM(CASE WHEN Disposition = N'Rejected' THEN 1 ELSE 0 END) AS RejectedCount,
                    SUM(CASE WHEN Disposition = N'Revised' THEN 1 ELSE 0 END) AS RevisedCount,
                    SUM(CASE WHEN Disposition = N'NeedsFollowUp' THEN 1 ELSE 0 END) AS NeedsFollowUpCount,
                    MIN(NULLIF(LTRIM(RTRIM(CommentShort)), N'')) AS DominantThemeHint,
                    MIN(RecordedUtc) AS FirstSignalRecordedUtc,
                    MAX(RecordedUtc) AS LastSignalRecordedUtc
                FROM Scoped
                GROUP BY
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM(ISNULL(PatternKey, N''))), N'') IS NOT NULL
                            THEN LTRIM(RTRIM(PatternKey))
                        ELSE CONCAT(
                                N'subject:',
                                SubjectType,
                                N'|artifact:',
                                COALESCE(NULLIF(LTRIM(RTRIM(ArtifactHint)), N''), N'--'))
                    END
            )
            SELECT TOP (@MaxAggregates)
                AggregateKey,
                PatternKeyRaw,
                SubjectTypeOrWorkflowArea,
                DistinctRunCount,
                TotalSignalCount,
                TrustedCount,
                RejectedCount,
                RevisedCount,
                NeedsFollowUpCount,
                DominantThemeHint,
                FirstSignalRecordedUtc,
                LastSignalRecordedUtc
            FROM Agg
            ORDER BY LastSignalRecordedUtc DESC, AggregateKey ASC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        IEnumerable<FeedbackAggregateSqlRow> rows = await connection.QueryAsync<FeedbackAggregateSqlRow>(
            new CommandDefinition(
                sql,
                new
                {
                    MaxAggregates = cap,
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    SinceUtc = sinceUtc,
                },
                cancellationToken: cancellationToken));

        return rows.Select(ToFeedbackAggregate).ToList();
    }

    public async Task<IReadOnlyList<ArtifactOutcomeTrend>> ListArtifactOutcomeTrendsAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime? sinceUtc,
        string? windowLabel,
        int maxTrends,
        CancellationToken cancellationToken)
    {
        int cap = maxTrends < 1 ? 1 : Math.Min(maxTrends, 500);

        const string sql = """
            WITH Scoped AS (
                SELECT *
                FROM dbo.ProductLearningPilotSignals
                WHERE TenantId = @TenantId
                  AND WorkspaceId = @WorkspaceId
                  AND ProjectId = @ProjectId
                  AND (@SinceUtc IS NULL OR RecordedUtc >= @SinceUtc)
            ),
            Trend AS (
                SELECT
                    CONCAT(
                        SubjectType,
                        N'|',
                        COALESCE(NULLIF(LTRIM(RTRIM(ArtifactHint)), N''), N'*')) AS TrendKey,
                    COALESCE(NULLIF(LTRIM(RTRIM(ArtifactHint)), N''), SubjectType) AS ArtifactTypeOrHint,
                    SUM(CASE WHEN Disposition = N'Trusted' THEN 1 ELSE 0 END) AS AcceptedOrTrustedCount,
                    SUM(CASE WHEN Disposition = N'Revised' THEN 1 ELSE 0 END) AS RevisionCount,
                    SUM(CASE WHEN Disposition = N'Rejected' THEN 1 ELSE 0 END) AS RejectionCount,
                    SUM(CASE WHEN Disposition = N'NeedsFollowUp' THEN 1 ELSE 0 END) AS NeedsFollowUpCount,
                    COUNT(DISTINCT CASE
                        WHEN ArchitectureRunId IS NOT NULL AND LTRIM(RTRIM(ArchitectureRunId)) <> N''
                            THEN ArchitectureRunId
                        END) AS DistinctRunCount,
                    MIN(NULLIF(LTRIM(RTRIM(CommentShort)), N'')) AS RepeatedThemeIndicator,
                    MIN(RecordedUtc) AS FirstSeenUtc,
                    MAX(RecordedUtc) AS LastSeenUtc,
                    SUM(CASE
                        WHEN Disposition IN (N'Rejected', N'Revised', N'NeedsFollowUp') THEN 1
                        ELSE 0
                    END) AS NegativeSignalWeight
                FROM Scoped
                GROUP BY
                    SubjectType,
                    COALESCE(NULLIF(LTRIM(RTRIM(ArtifactHint)), N''), N'*')
            )
            SELECT TOP (@MaxTrends)
                TrendKey,
                ArtifactTypeOrHint,
                AcceptedOrTrustedCount,
                RevisionCount,
                RejectionCount,
                NeedsFollowUpCount,
                DistinctRunCount,
                RepeatedThemeIndicator,
                FirstSeenUtc,
                LastSeenUtc
            FROM Trend
            ORDER BY NegativeSignalWeight DESC, TrendKey ASC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        IEnumerable<ArtifactOutcomeTrendSqlRow> rows = await connection.QueryAsync<ArtifactOutcomeTrendSqlRow>(
            new CommandDefinition(
                sql,
                new
                {
                    MaxTrends = cap,
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    SinceUtc = sinceUtc,
                },
                cancellationToken: cancellationToken));

        return rows
            .Select(r => ToArtifactOutcomeTrend(r, windowLabel))
            .ToList();
    }

    public async Task<IReadOnlyList<FeedbackAggregate>> ListTopRejectedRevisedArtifactRollupsAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime? sinceUtc,
        int take,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<FeedbackAggregate> all = await ListRunFeedbackAggregatesAsync(
            tenantId,
            workspaceId,
            projectId,
            sinceUtc,
            maxAggregates: 500,
            cancellationToken);

        int cap = take < 1 ? 1 : Math.Min(take, 200);

        return all
            .OrderByDescending(static a => a.RejectedCount + a.RevisedCount)
            .ThenByDescending(static a => a.LastSignalRecordedUtc)
            .ThenBy(static a => a.AggregateKey, StringComparer.Ordinal)
            .Take(cap)
            .ToList();
    }

    public async Task<IReadOnlyList<RepeatedCommentTheme>> ListRepeatedCommentThemesAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime? sinceUtc,
        int minOccurrences,
        int take,
        CancellationToken cancellationToken)
    {
        int min = minOccurrences < 1 ? 1 : minOccurrences;
        int cap = take < 1 ? 1 : Math.Min(take, 200);
        int prefixLen = ProductLearningSignalAggregations.CommentThemePrefixLength;

        string sql = $"""
            SELECT TOP (@Take)
                ThemeKey,
                OccurrenceCount,
                FirstSeenUtc,
                LastSeenUtc,
                SampleCommentShort
            FROM (
                SELECT
                    LEFT(LTRIM(RTRIM(CommentShort)), {prefixLen}) AS ThemeKey,
                    COUNT_BIG(*) AS OccurrenceCount,
                    MIN(RecordedUtc) AS FirstSeenUtc,
                    MAX(RecordedUtc) AS LastSeenUtc,
                    MIN(CommentShort) AS SampleCommentShort
                FROM dbo.ProductLearningPilotSignals
                WHERE TenantId = @TenantId
                  AND WorkspaceId = @WorkspaceId
                  AND ProjectId = @ProjectId
                  AND (@SinceUtc IS NULL OR RecordedUtc >= @SinceUtc)
                  AND CommentShort IS NOT NULL
                  AND LEN(LTRIM(RTRIM(CommentShort))) > 0
                GROUP BY LEFT(LTRIM(RTRIM(CommentShort)), {prefixLen})
                HAVING COUNT_BIG(*) >= @MinOccurrences
            ) t
            ORDER BY OccurrenceCount DESC, ThemeKey ASC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        IEnumerable<RepeatedCommentThemeSqlRow> rows = await connection.QueryAsync<RepeatedCommentThemeSqlRow>(
            new CommandDefinition(
                sql,
                new
                {
                    Take = cap,
                    MinOccurrences = min,
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    SinceUtc = sinceUtc,
                },
                cancellationToken: cancellationToken));

        return rows.Select(ToRepeatedCommentTheme).ToList();
    }

    public async Task<IReadOnlyList<ImprovementOpportunity>> ListImprovementOpportunityCandidatesAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime? sinceUtc,
        int minPoorOutcomeSignals,
        int minRevisedSignals,
        int take,
        CancellationToken cancellationToken)
    {
        int minPoor = minPoorOutcomeSignals < 1 ? 1 : minPoorOutcomeSignals;
        int minRev = minRevisedSignals < 1 ? 1 : minRevisedSignals;
        int cap = take < 1 ? 1 : Math.Min(take, 100);

        IReadOnlyList<FeedbackAggregate> aggregates = await ListRunFeedbackAggregatesAsync(
            tenantId,
            workspaceId,
            projectId,
            sinceUtc,
            maxAggregates: 500,
            cancellationToken);

        List<ImprovementOpportunity> list = aggregates
            .Where(a =>
                a.RejectedCount + a.NeedsFollowUpCount >= minPoor ||
                a.RevisedCount >= minRev)
            .OrderByDescending(static a => a.RejectedCount + a.NeedsFollowUpCount + a.RevisedCount)
            .ThenByDescending(static a => a.LastSignalRecordedUtc)
            .ThenBy(static a => a.AggregateKey, StringComparer.Ordinal)
            .Take(cap)
            .Select((a, i) => ProductLearningSignalAggregations.ToImprovementOpportunityCandidate(a, i + 1))
            .ToList();

        return list;
    }

    private static FeedbackAggregate ToFeedbackAggregate(FeedbackAggregateSqlRow row)
    {
        string? pk = string.IsNullOrWhiteSpace(row.PatternKeyRaw) ? null : row.PatternKeyRaw.Trim();

        return new FeedbackAggregate
        {
            AggregateKey = row.AggregateKey,
            PatternKey = pk,
            SubjectTypeOrWorkflowArea = row.SubjectTypeOrWorkflowArea,
            DistinctRunCount = row.DistinctRunCount,
            TotalSignalCount = row.TotalSignalCount,
            TrustedCount = row.TrustedCount,
            RejectedCount = row.RejectedCount,
            RevisedCount = row.RevisedCount,
            NeedsFollowUpCount = row.NeedsFollowUpCount,
            AverageTrustScore = null,
            AverageUsefulnessScore = null,
            DominantThemeHint = string.IsNullOrWhiteSpace(row.DominantThemeHint)
                ? null
                : TruncateForDisplay(row.DominantThemeHint, 240),
            FirstSignalRecordedUtc = row.FirstSignalRecordedUtc,
            LastSignalRecordedUtc = row.LastSignalRecordedUtc,
        };
    }

    private static ArtifactOutcomeTrend ToArtifactOutcomeTrend(ArtifactOutcomeTrendSqlRow row, string? windowLabel)
    {
        return new ArtifactOutcomeTrend
        {
            TrendKey = row.TrendKey,
            ArtifactTypeOrHint = row.ArtifactTypeOrHint,
            WindowLabel = windowLabel,
            AcceptedOrTrustedCount = row.AcceptedOrTrustedCount,
            RevisionCount = row.RevisionCount,
            RejectionCount = row.RejectionCount,
            NeedsFollowUpCount = row.NeedsFollowUpCount,
            DistinctRunCount = row.DistinctRunCount,
            AverageTrustScore = null,
            AverageUsefulnessScore = null,
            RepeatedThemeIndicator = string.IsNullOrWhiteSpace(row.RepeatedThemeIndicator)
                ? null
                : TruncateForDisplay(row.RepeatedThemeIndicator, 200),
            FirstSeenUtc = row.FirstSeenUtc,
            LastSeenUtc = row.LastSeenUtc,
        };
    }

    private static RepeatedCommentTheme ToRepeatedCommentTheme(RepeatedCommentThemeSqlRow row)
    {
        long n = row.OccurrenceCount;
        int count = n > int.MaxValue ? int.MaxValue : (int)n;

        return new RepeatedCommentTheme
        {
            ThemeKey = row.ThemeKey,
            OccurrenceCount = count,
            FirstSeenUtc = row.FirstSeenUtc,
            LastSeenUtc = row.LastSeenUtc,
            SampleCommentShort = row.SampleCommentShort,
        };
    }

    private static string TruncateForDisplay(string value, int maxChars)
    {
        if (value.Length <= maxChars)
        {
            return value;
        }

        return value[..maxChars];
    }
}

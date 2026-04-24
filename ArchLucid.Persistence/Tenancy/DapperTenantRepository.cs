using System.Data;

using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tenancy;

public sealed class DapperTenantRepository(ISqlConnectionFactory connectionFactory) : ITenantRepository
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    public async Task<TenantRecord?> GetByIdAsync(Guid tenantId, CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           SELECT Id, Name, Slug, Tier, EntraTenantId, CreatedUtc, SuspendedUtc,
                                  TrialStartUtc, TrialExpiresUtc, TrialRunsLimit, TrialRunsUsed, TrialSeatsLimit, TrialSeatsUsed,
                                  TrialStatus, TrialSampleRunId,
                                  TrialArchitecturePreseedEnqueuedUtc, TrialWelcomeRunId, TrialFirstManifestCommittedUtc,
                                  BaselineReviewCycleHours, BaselineReviewCycleSource, BaselineReviewCycleCapturedUtc,
                                  EnterpriseSeatsLimit, EnterpriseSeatsUsed
                           FROM dbo.Tenants
                           WHERE Id = @Id;
                           """;

        TenantRow? row = await connection.QuerySingleOrDefaultAsync<TenantRow>(
            new CommandDefinition(sql, new { Id = tenantId }, cancellationToken: ct));

        return row is null ? null : row.ToRecord();
    }

    public async Task<TenantRecord?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           SELECT Id, Name, Slug, Tier, EntraTenantId, CreatedUtc, SuspendedUtc,
                                  TrialStartUtc, TrialExpiresUtc, TrialRunsLimit, TrialRunsUsed, TrialSeatsLimit, TrialSeatsUsed,
                                  TrialStatus, TrialSampleRunId,
                                  TrialArchitecturePreseedEnqueuedUtc, TrialWelcomeRunId, TrialFirstManifestCommittedUtc,
                                  BaselineReviewCycleHours, BaselineReviewCycleSource, BaselineReviewCycleCapturedUtc,
                                  EnterpriseSeatsLimit, EnterpriseSeatsUsed
                           FROM dbo.Tenants
                           WHERE Slug = @Slug;
                           """;

        TenantRow? row = await connection.QuerySingleOrDefaultAsync<TenantRow>(
            new CommandDefinition(sql, new { Slug = slug.Trim().ToLowerInvariant() }, cancellationToken: ct));

        return row is null ? null : row.ToRecord();
    }

    public async Task<TenantRecord?> GetByEntraTenantIdAsync(Guid entraTenantId, CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           SELECT Id, Name, Slug, Tier, EntraTenantId, CreatedUtc, SuspendedUtc,
                                  TrialStartUtc, TrialExpiresUtc, TrialRunsLimit, TrialRunsUsed, TrialSeatsLimit, TrialSeatsUsed,
                                  TrialStatus, TrialSampleRunId,
                                  TrialArchitecturePreseedEnqueuedUtc, TrialWelcomeRunId, TrialFirstManifestCommittedUtc,
                                  BaselineReviewCycleHours, BaselineReviewCycleSource, BaselineReviewCycleCapturedUtc,
                                  EnterpriseSeatsLimit, EnterpriseSeatsUsed
                           FROM dbo.Tenants
                           WHERE EntraTenantId = @EntraTenantId;
                           """;

        TenantRow? row = await connection.QuerySingleOrDefaultAsync<TenantRow>(
            new CommandDefinition(sql, new { EntraTenantId = entraTenantId }, cancellationToken: ct));

        return row is null ? null : row.ToRecord();
    }

    public async Task<IReadOnlyList<TenantRecord>> ListAsync(CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           SELECT Id, Name, Slug, Tier, EntraTenantId, CreatedUtc, SuspendedUtc,
                                  TrialStartUtc, TrialExpiresUtc, TrialRunsLimit, TrialRunsUsed, TrialSeatsLimit, TrialSeatsUsed,
                                  TrialStatus, TrialSampleRunId,
                                  TrialArchitecturePreseedEnqueuedUtc, TrialWelcomeRunId, TrialFirstManifestCommittedUtc,
                                  BaselineReviewCycleHours, BaselineReviewCycleSource, BaselineReviewCycleCapturedUtc,
                                  EnterpriseSeatsLimit, EnterpriseSeatsUsed
                           FROM dbo.Tenants
                           ORDER BY CreatedUtc DESC;
                           """;

        IEnumerable<TenantRow> rows =
            await connection.QueryAsync<TenantRow>(new CommandDefinition(sql, cancellationToken: ct));

        return rows.Select(static r => r.ToRecord()).ToList();
    }

    /// <inheritdoc />
    public async Task CommitSelfServiceTrialAsync(
        Guid tenantId,
        DateTimeOffset trialStartUtc,
        DateTimeOffset trialExpiresUtc,
        int runsLimit,
        int seatsLimit,
        Guid sampleRunId,
        decimal? baselineReviewCycleHours,
        string? baselineReviewCycleSource,
        DateTimeOffset? baselineReviewCycleCapturedUtc,
        CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           UPDATE dbo.Tenants
                           SET TrialStartUtc = @TrialStartUtc,
                               TrialExpiresUtc = @TrialExpiresUtc,
                               TrialRunsLimit = @TrialRunsLimit,
                               TrialRunsUsed = 0,
                               TrialSeatsLimit = @TrialSeatsLimit,
                               TrialSeatsUsed = 1,
                               TrialStatus = @TrialStatus,
                               TrialSampleRunId = @TrialSampleRunId,
                               BaselineReviewCycleHours = @BaselineReviewCycleHours,
                               BaselineReviewCycleSource = @BaselineReviewCycleSource,
                               BaselineReviewCycleCapturedUtc = @BaselineReviewCycleCapturedUtc
                           WHERE Id = @Id;
                           """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    Id = tenantId,
                    TrialStartUtc = trialStartUtc,
                    TrialExpiresUtc = trialExpiresUtc,
                    TrialRunsLimit = runsLimit,
                    TrialSeatsLimit = seatsLimit,
                    TrialStatus = TrialLifecycleStatus.Active,
                    TrialSampleRunId = sampleRunId,
                    BaselineReviewCycleHours = baselineReviewCycleHours,
                    BaselineReviewCycleSource = baselineReviewCycleSource,
                    BaselineReviewCycleCapturedUtc = baselineReviewCycleCapturedUtc
                },
                cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task MarkTrialConvertedAsync(Guid tenantId, TenantTier? newCommercialTier, CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           UPDATE dbo.Tenants
                           SET TrialStatus = @Converted,
                               Tier = CASE WHEN @NewTier IS NULL THEN Tier ELSE @NewTier END
                           WHERE Id = @Id AND TrialStatus = @Active;
                           """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    Id = tenantId,
                    TrialLifecycleStatus.Active,
                    TrialLifecycleStatus.Converted,
                    NewTier = newCommercialTier is null ? null : newCommercialTier.Value.ToString()
                },
                cancellationToken: ct));
    }

    public async Task InsertTenantAsync(
        Guid tenantId,
        string name,
        string slug,
        TenantTier tier,
        Guid? entraTenantId,
        CancellationToken ct,
        int? enterpriseScimSeatsLimit = null)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        string sql = enterpriseScimSeatsLimit is null
            ? """
              INSERT INTO dbo.Tenants (Id, Name, Slug, Tier, EntraTenantId)
              VALUES (@Id, @Name, @Slug, @Tier, @EntraTenantId);
              """
            : """
              INSERT INTO dbo.Tenants (Id, Name, Slug, Tier, EntraTenantId, EnterpriseSeatsLimit)
              VALUES (@Id, @Name, @Slug, @Tier, @EntraTenantId, @EnterpriseSeatsLimit);
              """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    Id = tenantId,
                    Name = name,
                    Slug = slug,
                    Tier = TenantTierSql.ToTierString(tier),
                    EntraTenantId = entraTenantId,
                    EnterpriseSeatsLimit = enterpriseScimSeatsLimit
                },
                cancellationToken: ct));
    }

    public async Task InsertWorkspaceAsync(
        Guid workspaceId,
        Guid tenantId,
        string name,
        Guid defaultProjectId,
        CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           INSERT INTO dbo.TenantWorkspaces (Id, TenantId, Name, DefaultProjectId)
                           VALUES (@Id, @TenantId, @Name, @DefaultProjectId);
                           """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { Id = workspaceId, TenantId = tenantId, Name = name, DefaultProjectId = defaultProjectId },
                cancellationToken: ct));
    }

    public async Task SuspendTenantAsync(Guid tenantId, CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           UPDATE dbo.Tenants
                           SET SuspendedUtc = SYSUTCDATETIME()
                           WHERE Id = @Id;
                           """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = tenantId }, cancellationToken: ct));
    }

    public async Task<TenantWorkspaceLink?> GetFirstWorkspaceAsync(Guid tenantId, CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           SELECT TOP (1) Id AS WorkspaceId, DefaultProjectId
                           FROM dbo.TenantWorkspaces
                           WHERE TenantId = @TenantId
                           ORDER BY CreatedUtc ASC;
                           """;

        WorkspaceRow? row = await connection.QuerySingleOrDefaultAsync<WorkspaceRow>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: ct));

        if (row is null)
            return null;

        return new TenantWorkspaceLink { WorkspaceId = row.WorkspaceId, DefaultProjectId = row.DefaultProjectId };
    }

    /// <inheritdoc />
    public async Task TryIncrementActiveTrialRunAsync(
        Guid tenantId,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        const string selectSql = """
                                 SELECT TrialStatus, TrialExpiresUtc, TrialRunsLimit, TrialRunsUsed
                                 FROM dbo.Tenants WITH (UPDLOCK, ROWLOCK)
                                 WHERE Id = @Id;
                                 """;

        const string updateSql = """
                                 UPDATE dbo.Tenants
                                 SET TrialRunsUsed = TrialRunsUsed + 1
                                 WHERE Id = @Id
                                   AND TrialStatus = @Active
                                   AND TrialRunsLimit IS NOT NULL
                                   AND TrialExpiresUtc > SYSUTCDATETIME()
                                   AND TrialRunsUsed < TrialRunsLimit;
                                 """;

        if (connection is not null)
        {
            await ApplyTrialRunIncrementAsync(connection, transaction, tenantId, selectSql, updateSql, ct);

            return;
        }

        await using SqlConnection owned = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using SqlTransaction tran = (SqlTransaction)await owned.BeginTransactionAsync(ct);

        try
        {
            await ApplyTrialRunIncrementAsync(owned, tran, tenantId, selectSql, updateSql, ct);
            await tran.CommitAsync(ct);
        }
        catch
        {
            await tran.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task TryClaimTrialSeatAsync(Guid tenantId, string principalKey, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(principalKey);

        string key = principalKey.Trim();

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using SqlTransaction tran = (SqlTransaction)await connection.BeginTransactionAsync(ct);

        const string tenantSql = """
                                 SELECT TrialStatus, TrialSeatsLimit, TrialSeatsUsed, TrialExpiresUtc
                                 FROM dbo.Tenants WITH (UPDLOCK, ROWLOCK)
                                 WHERE Id = @Id;
                                 """;

        TenantSeatRow? t = await connection.QuerySingleOrDefaultAsync<TenantSeatRow>(
            new CommandDefinition(tenantSql, new { Id = tenantId }, tran, cancellationToken: ct));

        if (t is null)
        {
            await tran.CommitAsync(ct);

            return;
        }

        if (!string.Equals(t.TrialStatus, TrialLifecycleStatus.Active, StringComparison.Ordinal) ||
            t.TrialSeatsLimit is null)
        {
            await tran.CommitAsync(ct);

            return;
        }

        if (t.TrialExpiresUtc is { } exp && exp <= DateTimeOffset.UtcNow)
        {
            await tran.RollbackAsync(ct);

            throw new TrialLimitExceededException(
                TrialLimitReason.Expired,
                ComputeDaysRemaining(t.TrialExpiresUtc));
        }

        const string insertSql = """
                                 INSERT INTO dbo.TenantTrialSeatOccupants (TenantId, PrincipalKey, CreatedUtc)
                                 VALUES (@TenantId, @PrincipalKey, SYSUTCDATETIME());
                                 """;

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(insertSql, new { TenantId = tenantId, PrincipalKey = key }, tran,
                    cancellationToken: ct));
        }
        catch (SqlException ex) when (ex.Number == 2627)
        {
            await tran.CommitAsync(ct);

            return;
        }

        const string bumpSql = """
                               UPDATE dbo.Tenants
                               SET TrialSeatsUsed = TrialSeatsUsed + 1
                               WHERE Id = @Id
                                 AND TrialStatus = @Active
                                 AND TrialSeatsUsed < @SeatLimit;
                               """;

        int bumped = await connection.ExecuteAsync(
            new CommandDefinition(
                bumpSql,
                new { Id = tenantId, TrialLifecycleStatus.Active, SeatLimit = t.TrialSeatsLimit.Value },
                tran,
                cancellationToken: ct));

        if (bumped == 0)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM dbo.TenantTrialSeatOccupants
                    WHERE TenantId = @TenantId AND PrincipalKey = @PrincipalKey;
                    """,
                    new { TenantId = tenantId, PrincipalKey = key },
                    tran,
                    cancellationToken: ct));

            await tran.RollbackAsync(ct);

            throw new TrialLimitExceededException(
                TrialLimitReason.SeatsExceeded,
                ComputeDaysRemaining(t.TrialExpiresUtc));
        }

        await tran.CommitAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> ListTrialLifecycleAutomationTenantIdsAsync(CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           SELECT Id
                           FROM dbo.Tenants
                           WHERE TrialExpiresUtc IS NOT NULL
                             AND TrialStatus IS NOT NULL
                             AND TrialStatus <> @Converted
                           ORDER BY CreatedUtc ASC;
                           """;

        IEnumerable<Guid> ids = await connection.QueryAsync<Guid>(
            new CommandDefinition(
                sql,
                new { TrialLifecycleStatus.Converted },
                cancellationToken: ct));

        return ids.ToList();
    }

    /// <inheritdoc />
    public async Task EnqueueTrialArchitecturePreseedAsync(Guid tenantId, CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           UPDATE dbo.Tenants
                           SET TrialArchitecturePreseedEnqueuedUtc = SYSUTCDATETIME()
                           WHERE Id = @Id
                             AND TrialWelcomeRunId IS NULL
                             AND (TrialArchitecturePreseedEnqueuedUtc IS NULL);
                           """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = tenantId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> ListTenantIdsPendingTrialArchitecturePreseedAsync(int take,
        CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           SELECT TOP (@Take) Id
                           FROM dbo.Tenants WITH (UPDLOCK, ROWLOCK)
                           WHERE TrialArchitecturePreseedEnqueuedUtc IS NOT NULL
                             AND TrialWelcomeRunId IS NULL
                             AND TrialStatus = @Active
                           ORDER BY TrialArchitecturePreseedEnqueuedUtc ASC;
                           """;

        IEnumerable<Guid> ids = await connection.QueryAsync<Guid>(
            new CommandDefinition(
                sql,
                new { Take = Math.Clamp(take, 1, 50), TrialLifecycleStatus.Active },
                cancellationToken: ct));

        return ids.ToList();
    }

    /// <inheritdoc />
    public async Task MarkTrialArchitecturePreseedCompletedAsync(Guid tenantId, Guid welcomeRunId, CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           UPDATE dbo.Tenants
                           SET TrialWelcomeRunId = @WelcomeRunId
                           WHERE Id = @Id
                             AND TrialWelcomeRunId IS NULL;
                           """;

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = tenantId, WelcomeRunId = welcomeRunId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> TryRecordTrialLifecycleTransitionAsync(
        Guid tenantId,
        string expectedCurrentStatus,
        string nextStatus,
        string reason,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedCurrentStatus);
        ArgumentException.ThrowIfNullOrWhiteSpace(nextStatus);

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using SqlTransaction tran = (SqlTransaction)await connection.BeginTransactionAsync(ct);

        const string insertLog = """
                                 INSERT INTO dbo.TenantLifecycleTransitions (TenantId, FromStatus, ToStatus, OccurredUtc, Reason)
                                 VALUES (@TenantId, @FromStatus, @ToStatus, SYSUTCDATETIME(), @Reason);
                                 """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertLog,
                new
                {
                    TenantId = tenantId,
                    FromStatus = expectedCurrentStatus,
                    ToStatus = nextStatus,
                    Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()
                },
                tran,
                cancellationToken: ct));

        const string updateTenant = """
                                    UPDATE dbo.Tenants
                                    SET TrialStatus = @NextStatus
                                    WHERE Id = @TenantId AND TrialStatus = @ExpectedStatus;
                                    """;

        int updated = await connection.ExecuteAsync(
            new CommandDefinition(
                updateTenant,
                new { TenantId = tenantId, ExpectedStatus = expectedCurrentStatus, NextStatus = nextStatus },
                tran,
                cancellationToken: ct));

        if (updated == 0)
        {
            await tran.RollbackAsync(ct);

            return false;
        }

        await tran.CommitAsync(ct);

        return true;
    }

    /// <inheritdoc />
    public async Task<TrialFirstManifestCommitOutcome?> TryMarkFirstManifestCommittedAsync(
        Guid tenantId,
        DateTimeOffset committedUtc,
        CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           UPDATE dbo.Tenants
                           SET TrialFirstManifestCommittedUtc = @CommittedUtc
                           OUTPUT INSERTED.TrialRunsUsed,
                                  INSERTED.TrialRunsLimit,
                                  INSERTED.CreatedUtc,
                                  INSERTED.TrialStartUtc
                           WHERE Id = @TenantId
                             AND TrialFirstManifestCommittedUtc IS NULL;
                           """;

        TrialFirstManifestOutputRow? row = await connection.QuerySingleOrDefaultAsync<TrialFirstManifestOutputRow>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId, CommittedUtc = committedUtc },
                cancellationToken: ct));

        if (row is null)
            return null;


        DateTimeOffset anchor = row.TrialStartUtc ?? row.CreatedUtc;
        double seconds = (committedUtc - anchor).TotalSeconds;

        double ratio = 0;

        if (row.TrialRunsLimit is { } lim and > 0)

            ratio = (double)row.TrialRunsUsed / lim;


        return new TrialFirstManifestCommitOutcome { SignupToCommitSeconds = seconds, TrialRunUsageRatio = ratio };
    }

    /// <inheritdoc />
    public async Task E2eHarnessSetTrialExpiresUtcAsync(Guid tenantId, DateTimeOffset expiresUtc, CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           UPDATE dbo.Tenants
                           SET TrialExpiresUtc = @ExpiresUtc
                           WHERE Id = @TenantId;
                           """;

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { TenantId = tenantId, ExpiresUtc = expiresUtc }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> TryIncrementEnterpriseScimSeatAsync(Guid tenantId, CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           UPDATE dbo.Tenants
                           SET EnterpriseSeatsUsed = EnterpriseSeatsUsed + 1
                           WHERE Id = @TenantId
                             AND (EnterpriseSeatsLimit IS NULL OR EnterpriseSeatsUsed < EnterpriseSeatsLimit);
                           """;

        int rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: ct));

        return rows == 1;
    }

    /// <inheritdoc />
    public async Task DecrementEnterpriseScimSeatAsync(Guid tenantId, CancellationToken ct)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = """
                           UPDATE dbo.Tenants
                           SET EnterpriseSeatsUsed = CASE WHEN EnterpriseSeatsUsed > 0 THEN EnterpriseSeatsUsed - 1 ELSE 0 END
                           WHERE Id = @TenantId;
                           """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: ct));
    }

    private static int ComputeDaysRemaining(DateTimeOffset? trialExpiresUtc)
    {
        if (trialExpiresUtc is null)
            return 0;

        double totalDays = (trialExpiresUtc.Value - DateTimeOffset.UtcNow).TotalDays;
        int days = (int)Math.Floor(totalDays);

        return days < 0 ? 0 : days;
    }

    private static async Task ApplyTrialRunIncrementAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        Guid tenantId,
        string selectSql,
        string updateSql,
        CancellationToken ct)
    {
        TrialRunGateRow? row = await connection.QuerySingleOrDefaultAsync<TrialRunGateRow>(
            new CommandDefinition(selectSql, new { Id = tenantId }, transaction, cancellationToken: ct));

        if (row is null)
            return;

        if (!string.Equals(row.TrialStatus, TrialLifecycleStatus.Active, StringComparison.Ordinal) ||
            row.TrialRunsLimit is null)
            return;


        if (row.TrialExpiresUtc is { } exp && exp <= DateTimeOffset.UtcNow)

            throw new TrialLimitExceededException(
                TrialLimitReason.Expired,
                ComputeDaysRemaining(row.TrialExpiresUtc));


        if (row.TrialRunsUsed >= row.TrialRunsLimit.Value)

            throw new TrialLimitExceededException(
                TrialLimitReason.RunsExceeded,
                ComputeDaysRemaining(row.TrialExpiresUtc));


        int updated = await connection.ExecuteAsync(
            new CommandDefinition(
                updateSql,
                new { Id = tenantId, TrialLifecycleStatus.Active },
                transaction,
                cancellationToken: ct));

        if (updated == 0)

            throw new TrialLimitExceededException(
                TrialLimitReason.RunsExceeded,
                ComputeDaysRemaining(row.TrialExpiresUtc));
    }

    private sealed class TrialFirstManifestOutputRow
    {
        public int TrialRunsUsed
        {
            get;
            init;
        }

        public int? TrialRunsLimit
        {
            get;
            init;
        }

        public DateTimeOffset CreatedUtc
        {
            get;
            init;
        }

        public DateTimeOffset? TrialStartUtc
        {
            get;
            init;
        }
    }

    private sealed class TrialRunGateRow
    {
        public string? TrialStatus
        {
            get;
            init;
        }

        public DateTimeOffset? TrialExpiresUtc
        {
            get;
            init;
        }

        public int? TrialRunsLimit
        {
            get;
            init;
        }

        public int TrialRunsUsed
        {
            get;
            init;
        }
    }

    private sealed class TenantSeatRow
    {
        public string? TrialStatus
        {
            get;
            init;
        }

        public int? TrialSeatsLimit
        {
            get;
            init;
        }

        public int TrialSeatsUsed
        {
            get;
            init;
        }

        public DateTimeOffset? TrialExpiresUtc
        {
            get;
            init;
        }
    }

    private sealed class WorkspaceRow
    {
        public Guid WorkspaceId
        {
            get;
            init;
        }

        public Guid DefaultProjectId
        {
            get;
            init;
        }
    }

    private sealed class TenantRow
    {
        public Guid Id
        {
            get;
            init;
        }

        public string Name
        {
            get;
            init;
        } = string.Empty;

        public string Slug
        {
            get;
            init;
        } = string.Empty;

        public string Tier
        {
            get;
            init;
        } = string.Empty;

        public Guid? EntraTenantId
        {
            get;
            init;
        }

        public DateTimeOffset CreatedUtc
        {
            get;
            init;
        }

        public DateTimeOffset? SuspendedUtc
        {
            get;
            init;
        }

        public DateTimeOffset? TrialStartUtc
        {
            get;
            init;
        }

        public DateTimeOffset? TrialExpiresUtc
        {
            get;
            init;
        }

        public int? TrialRunsLimit
        {
            get;
            init;
        }

        public int TrialRunsUsed
        {
            get;
            init;
        }

        public int? TrialSeatsLimit
        {
            get;
            init;
        }

        public int TrialSeatsUsed
        {
            get;
            init;
        }

        public string? TrialStatus
        {
            get;
            init;
        }

        public Guid? TrialSampleRunId
        {
            get;
            init;
        }

        public DateTimeOffset? TrialArchitecturePreseedEnqueuedUtc
        {
            get;
            init;
        }

        public Guid? TrialWelcomeRunId
        {
            get;
            init;
        }

        public DateTimeOffset? TrialFirstManifestCommittedUtc
        {
            get;
            init;
        }

        public decimal? BaselineReviewCycleHours
        {
            get;
            init;
        }

        public string? BaselineReviewCycleSource
        {
            get;
            init;
        }

        public DateTimeOffset? BaselineReviewCycleCapturedUtc
        {
            get;
            init;
        }

        public int? EnterpriseSeatsLimit
        {
            get;
            init;
        }

        public int EnterpriseSeatsUsed
        {
            get;
            init;
        }

        internal TenantRecord ToRecord()
        {
            return new TenantRecord
            {
                Id = Id,
                Name = Name,
                Slug = Slug,
                Tier = TenantTierSql.ParseTier(Tier),
                EntraTenantId = EntraTenantId,
                CreatedUtc = CreatedUtc,
                SuspendedUtc = SuspendedUtc,
                TrialStartUtc = TrialStartUtc,
                TrialExpiresUtc = TrialExpiresUtc,
                TrialRunsLimit = TrialRunsLimit,
                TrialRunsUsed = TrialRunsUsed,
                TrialSeatsLimit = TrialSeatsLimit,
                TrialSeatsUsed = TrialSeatsUsed,
                TrialStatus = TrialStatus,
                TrialSampleRunId = TrialSampleRunId,
                TrialArchitecturePreseedEnqueuedUtc = TrialArchitecturePreseedEnqueuedUtc,
                TrialWelcomeRunId = TrialWelcomeRunId,
                TrialFirstManifestCommittedUtc = TrialFirstManifestCommittedUtc,
                BaselineReviewCycleHours = BaselineReviewCycleHours,
                BaselineReviewCycleSource = BaselineReviewCycleSource,
                BaselineReviewCycleCapturedUtc = BaselineReviewCycleCapturedUtc,
                EnterpriseSeatsLimit = EnterpriseSeatsLimit,
                EnterpriseSeatsUsed = EnterpriseSeatsUsed
            };
        }
    }
}

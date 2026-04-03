using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchiForge.Data.Infrastructure;

using Dapper;

namespace ArchiForge.Data.Repositories;

[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; integration-tested separately.")]
public sealed class BackgroundJobRepository(IDbConnectionFactory connectionFactory) : IBackgroundJobRepository
{
    public async Task InsertAsync(BackgroundJobRow row, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(row);

        const string sql = """
            INSERT INTO dbo.BackgroundJobs
            (
                JobId,
                WorkUnitJson,
                State,
                CreatedUtc,
                StartedUtc,
                CompletedUtc,
                Error,
                FileName,
                ContentType,
                RetryCount,
                MaxRetries,
                ResultBlobName
            )
            VALUES
            (
                @JobId,
                @WorkUnitJson,
                @State,
                @CreatedUtc,
                @StartedUtc,
                @CompletedUtc,
                @Error,
                @FileName,
                @ContentType,
                @RetryCount,
                @MaxRetries,
                @ResultBlobName
            )
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(sql, row, cancellationToken: cancellationToken));
    }

    public async Task<BackgroundJobRow?> GetAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return null;

        const string sql = """
            SELECT
                JobId,
                WorkUnitJson,
                State,
                CreatedUtc,
                StartedUtc,
                CompletedUtc,
                Error,
                FileName,
                ContentType,
                RetryCount,
                MaxRetries,
                ResultBlobName
            FROM dbo.BackgroundJobs
            WHERE JobId = @JobId
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<BackgroundJobRow>(
            new CommandDefinition(sql, new { JobId = jobId }, cancellationToken: cancellationToken));
    }

    public async Task<int> TryMarkRunningAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return 0;

        const string sql = """
            UPDATE dbo.BackgroundJobs
            SET State = N'Running',
                StartedUtc = COALESCE(StartedUtc, SYSUTCDATETIME())
            WHERE JobId = @JobId
              AND State = N'Pending'
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteAsync(
            new CommandDefinition(sql, new { JobId = jobId }, cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task<QueuedBackgroundJobPrepareResult> TryPrepareQueuedJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return new QueuedBackgroundJobPrepareResult(false, true, false, null);
        }

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using IDbTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        try
        {
            const string selectSql = """
                SELECT
                    JobId,
                    WorkUnitJson,
                    State,
                    CreatedUtc,
                    StartedUtc,
                    CompletedUtc,
                    Error,
                    FileName,
                    ContentType,
                    RetryCount,
                    MaxRetries,
                    ResultBlobName
                FROM dbo.BackgroundJobs WITH (UPDLOCK, ROWLOCK)
                WHERE JobId = @JobId
                """;

            BackgroundJobRow? row = await connection.QuerySingleOrDefaultAsync<BackgroundJobRow>(
                new CommandDefinition(
                    selectSql,
                    new { JobId = jobId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (row is null)
            {
                transaction.Commit();

                return new QueuedBackgroundJobPrepareResult(false, true, true, null);
            }

            if (IsTerminalJobState(row.State))
            {
                transaction.Commit();

                return new QueuedBackgroundJobPrepareResult(false, true, false, null);
            }

            if (string.Equals(row.State, "Running", StringComparison.OrdinalIgnoreCase))
            {
                transaction.Commit();

                return new QueuedBackgroundJobPrepareResult(false, true, false, null);
            }

            if (!string.Equals(row.State, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                transaction.Commit();

                return new QueuedBackgroundJobPrepareResult(false, true, false, null);
            }

            const string updateSql = """
                UPDATE dbo.BackgroundJobs
                SET State = N'Running',
                    StartedUtc = COALESCE(StartedUtc, SYSUTCDATETIME())
                WHERE JobId = @JobId
                  AND State = N'Pending'
                """;

            int affected = await connection.ExecuteAsync(
                new CommandDefinition(
                    updateSql,
                    new { JobId = jobId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (affected == 0)
            {
                transaction.Commit();

                return new QueuedBackgroundJobPrepareResult(false, false, false, null);
            }

            BackgroundJobRow runningRow = new()
            {
                JobId = row.JobId,
                WorkUnitJson = row.WorkUnitJson,
                State = "Running",
                CreatedUtc = row.CreatedUtc,
                StartedUtc = DateTimeOffset.UtcNow,
                CompletedUtc = row.CompletedUtc,
                Error = row.Error,
                FileName = row.FileName,
                ContentType = row.ContentType,
                RetryCount = row.RetryCount,
                MaxRetries = row.MaxRetries,
                ResultBlobName = row.ResultBlobName
            };

            transaction.Commit();

            return new QueuedBackgroundJobPrepareResult(true, false, false, runningRow);
        }
        catch
        {
            transaction.Rollback();

            throw;
        }
    }

    private static bool IsTerminalJobState(string state)
    {
        if (string.Equals(state, "Succeeded", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(state, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public async Task MarkSucceededAsync(
        string jobId,
        string resultBlobName,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resultBlobName);
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(contentType);

        const string sql = """
            UPDATE dbo.BackgroundJobs
            SET State = N'Succeeded',
                CompletedUtc = SYSUTCDATETIME(),
                Error = NULL,
                FileName = @FileName,
                ContentType = @ContentType,
                ResultBlobName = @ResultBlobName
            WHERE JobId = @JobId
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    JobId = jobId,
                    ResultBlobName = resultBlobName,
                    FileName = fileName,
                    ContentType = contentType
                },
                cancellationToken: cancellationToken));
    }

    public async Task MarkFailedTerminalAsync(
        string jobId,
        string error,
        int retryCount,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.BackgroundJobs
            SET State = N'Failed',
                CompletedUtc = SYSUTCDATETIME(),
                Error = @Error,
                RetryCount = @RetryCount
            WHERE JobId = @JobId
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { JobId = jobId, Error = error, RetryCount = retryCount },
                cancellationToken: cancellationToken));
    }

    public async Task MarkPendingRetryAsync(
        string jobId,
        int retryCount,
        string error,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.BackgroundJobs
            SET State = N'Pending',
                Error = @Error,
                RetryCount = @RetryCount,
                StartedUtc = NULL
            WHERE JobId = @JobId
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { JobId = jobId, RetryCount = retryCount, Error = error },
                cancellationToken: cancellationToken));
    }

    public async Task<int> CountNonTerminalAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT_BIG(1)
            FROM dbo.BackgroundJobs
            WHERE State IN (N'Pending', N'Running')
            """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        long count = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return count > int.MaxValue ? int.MaxValue : (int)count;
    }
}

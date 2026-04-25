using System.Diagnostics.CodeAnalysis;

using ArchLucid.Core.Feedback;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Feedback;

[ExcludeFromCodeCoverage(Justification = "SQL Server–dependent repository.")]
public sealed class SqlFindingFeedbackRepository(
    ISqlConnectionFactory connectionFactory,
    IRlsSessionContextApplicator rlsSessionContextApplicator) : IFindingFeedbackRepository
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    private readonly IRlsSessionContextApplicator _rlsSessionContextApplicator =
        rlsSessionContextApplicator ?? throw new ArgumentNullException(nameof(rlsSessionContextApplicator));

    /// <inheritdoc />
    public async Task InsertAsync(FindingFeedbackSubmission submission, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await _rlsSessionContextApplicator.ApplyAsync(connection, cancellationToken);

        const string sql = """
                           INSERT INTO dbo.FindingFeedback (
                               FeedbackId,
                               TenantId, WorkspaceId, ProjectId,
                               RunId, FindingId, Score, CreatedUtc)
                           VALUES (
                               @FeedbackId,
                               @TenantId, @WorkspaceId, @ProjectId,
                               @RunId, @FindingId, @Score, SYSUTCDATETIME());
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
                    submission.RunId,
                    submission.FindingId,
                    submission.Score
                },
                cancellationToken: cancellationToken));
    }
}

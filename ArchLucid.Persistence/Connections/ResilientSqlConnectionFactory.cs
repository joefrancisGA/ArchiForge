using Microsoft.Data.SqlClient;

using Polly;

namespace ArchLucid.Persistence.Connections;

/// <summary>
///     Decorator over <see cref="ISqlConnectionFactory" /> that retries transient failures
///     using <see cref="ResiliencePipeline" /> (Microsoft.Extensions.Resilience / Polly).
/// </summary>
public sealed class ResilientSqlConnectionFactory(
    ISqlConnectionFactory inner,
    ResiliencePipeline sqlOpenRetryPipeline) : ISqlConnectionFactory
{
    private readonly ISqlConnectionFactory _inner =
        inner ?? throw new ArgumentNullException(nameof(inner));

    private readonly ResiliencePipeline _sqlOpenRetryPipeline =
        sqlOpenRetryPipeline ?? throw new ArgumentNullException(nameof(sqlOpenRetryPipeline));

    /// <inheritdoc />
    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        return await _sqlOpenRetryPipeline.ExecuteAsync(
            async ct => await _inner.CreateOpenConnectionAsync(ct),
            cancellationToken);
    }
}

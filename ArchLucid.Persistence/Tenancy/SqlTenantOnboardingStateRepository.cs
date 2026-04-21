using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tenancy;

public sealed class SqlTenantOnboardingStateRepository(ISqlConnectionFactory connectionFactory)
    : ITenantOnboardingStateRepository
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public async Task<bool> TryMarkFirstSessionCompletedAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
                           BEGIN TRAN;

                           DECLARE @first bit = 0;

                           IF NOT EXISTS (SELECT 1 FROM dbo.TenantOnboardingState WITH (UPDLOCK, HOLDLOCK) WHERE TenantId = @TenantId)
                           BEGIN
                               INSERT INTO dbo.TenantOnboardingState (TenantId, FirstSessionCompletedUtc)
                               VALUES (@TenantId, SYSUTCDATETIME());
                               SET @first = 1;
                           END
                           ELSE IF EXISTS (SELECT 1 FROM dbo.TenantOnboardingState WHERE TenantId = @TenantId AND FirstSessionCompletedUtc IS NULL)
                           BEGIN
                               UPDATE dbo.TenantOnboardingState
                               SET FirstSessionCompletedUtc = SYSUTCDATETIME()
                               WHERE TenantId = @TenantId AND FirstSessionCompletedUtc IS NULL;
                               SET @first = 1;
                           END

                           COMMIT TRAN;
                           SELECT @first;
                           """;

        bool first = await connection.QuerySingleAsync<bool>(
            new CommandDefinition(sql, new
            {
                TenantId = tenantId
            }, cancellationToken: cancellationToken));

        return first;
    }
}

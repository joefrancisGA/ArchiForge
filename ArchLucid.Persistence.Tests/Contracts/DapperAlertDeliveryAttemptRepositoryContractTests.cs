using ArchLucid.Decisioning.Alerts.Delivery;

using ArchLucid.Persistence;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Tests.Support;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperAlertDeliveryAttemptRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : AlertDeliveryAttemptRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override async Task EnsureDeliveryAttemptParentsExistAsync(
        Guid alertId,
        Guid routingSubscriptionId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        Guid ruleId = Guid.NewGuid();
        DateTime createdUtc = DateTime.UtcNow;
        string deduplicationKey = $"contract-alert-{alertId:N}";

        // Full RLS bypass for seeding: FK validation on dbo.AlertDeliveryAttempts consults dbo.AlertRecords under RLS.
        // A scoped+bypass seed connection can fail that visibility check on some SQL Server configurations.
        await using SqlConnection connection = new(fixture.ConnectionString);
        await connection.OpenAsync(ct);
        await PersistenceIntegrationTestRlsSession.ApplyArchLucidRlsBypassAsync(connection, ct);

        const string insertRuleSql = """
            INSERT INTO dbo.AlertRules
            (
                RuleId, TenantId, WorkspaceId, ProjectId,
                Name, RuleType, Severity, ThresholdValue, IsEnabled,
                TargetChannelType, MetadataJson, CreatedUtc
            )
            VALUES
            (
                @RuleId, @TenantId, @WorkspaceId, @ProjectId,
                @Name, @RuleType, @Severity, @ThresholdValue, @IsEnabled,
                @TargetChannelType, @MetadataJson, @CreatedUtc
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertRuleSql,
                new
                {
                    RuleId = ruleId,
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    Name = "contract-test-rule",
                    RuleType = "test",
                    Severity = "Info",
                    ThresholdValue = 0m,
                    IsEnabled = true,
                    TargetChannelType = "test",
                    MetadataJson = "{}",
                    CreatedUtc = createdUtc
                },
                cancellationToken: ct));

        const string insertAlertSql = """
            INSERT INTO dbo.AlertRecords
            (
                AlertId, RuleId, TenantId, WorkspaceId, ProjectId,
                RunId, ComparedToRunId, RecommendationId,
                Title, Category, Severity, Status,
                TriggerValue, Description, CreatedUtc, LastUpdatedUtc,
                AcknowledgedByUserId, AcknowledgedByUserName, ResolutionComment,
                DeduplicationKey
            )
            VALUES
            (
                @AlertId, @RuleId, @TenantId, @WorkspaceId, @ProjectId,
                @RunId, @ComparedToRunId, @RecommendationId,
                @Title, @Category, @Severity, @Status,
                @TriggerValue, @Description, @CreatedUtc, @LastUpdatedUtc,
                @AcknowledgedByUserId, @AcknowledgedByUserName, @ResolutionComment,
                @DeduplicationKey
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertAlertSql,
                new
                {
                    AlertId = alertId,
                    RuleId = ruleId,
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    RunId = (Guid?)null,
                    ComparedToRunId = (Guid?)null,
                    RecommendationId = (Guid?)null,
                    Title = "contract-test-alert",
                    Category = "test",
                    Severity = "Info",
                    Status = "Open",
                    TriggerValue = "0",
                    Description = "contract test",
                    CreatedUtc = createdUtc,
                    LastUpdatedUtc = (DateTime?)null,
                    AcknowledgedByUserId = (string?)null,
                    AcknowledgedByUserName = (string?)null,
                    ResolutionComment = (string?)null,
                    DeduplicationKey = deduplicationKey
                },
                cancellationToken: ct));

        const string insertRoutingSql = """
            INSERT INTO dbo.AlertRoutingSubscriptions
            (
                RoutingSubscriptionId, TenantId, WorkspaceId, ProjectId,
                Name, ChannelType, Destination, MinimumSeverity, IsEnabled,
                CreatedUtc, LastDeliveredUtc, MetadataJson
            )
            VALUES
            (
                @RoutingSubscriptionId, @TenantId, @WorkspaceId, @ProjectId,
                @Name, @ChannelType, @Destination, @MinimumSeverity, @IsEnabled,
                @CreatedUtc, @LastDeliveredUtc, @MetadataJson
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertRoutingSql,
                new
                {
                    RoutingSubscriptionId = routingSubscriptionId,
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    Name = "contract-test-routing",
                    ChannelType = "test",
                    Destination = "https://example.test/hook",
                    MinimumSeverity = "Info",
                    IsEnabled = true,
                    CreatedUtc = createdUtc,
                    LastDeliveredUtc = (DateTime?)null,
                    MetadataJson = "{}"
                },
                cancellationToken: ct));
    }

    protected override IAlertDeliveryAttemptRepository CreateRepository(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId)
    {
        ISqlConnectionFactory connectionFactory =
            new global::ArchLucid.Persistence.Tests.RlsTenantScopedTestSqlConnectionFactory(
                fixture.ConnectionString,
                tenantId,
                workspaceId,
                projectId);

        return new DapperAlertDeliveryAttemptRepository(connectionFactory);
    }
}

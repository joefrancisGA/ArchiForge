using System.Diagnostics.CodeAnalysis;

using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Telemetry;

/// <summary>
///     SQL-backed <see cref="IFirstTenantFunnelEventStore" />. Writes one row to
///     <c>dbo.FirstTenantFunnelEvents</c> per call. Validates the event name in code (the SQL CHECK
///     constraint is the second line of defence). Inserts only the three minimum columns —
///     <c>TenantId</c>, <c>EventName</c>, <c>OccurredUtc</c> — never <c>UserId</c>, IP, or
///     user-agent.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; covered by integration test.")]
public sealed class SqlFirstTenantFunnelEventStore(ISqlConnectionFactory connectionFactory)
    : IFirstTenantFunnelEventStore
{
    private static readonly HashSet<string> AllowedEvents = new(StringComparer.Ordinal)
    {
        "signup",
        "tour_opt_in",
        "first_run_started",
        "first_run_committed",
        "first_finding_viewed",
        "thirty_minute_milestone"
    };

    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public async Task AppendAsync(string eventName, Guid tenantId, DateTime occurredUtc, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(eventName)) throw new ArgumentException("eventName is required.", nameof(eventName));
        if (!AllowedEvents.Contains(eventName))
            throw new ArgumentOutOfRangeException(nameof(eventName), eventName, "Unknown funnel event name.");
        if (tenantId == Guid.Empty) throw new ArgumentException("tenantId is required.", nameof(tenantId));

        const string sql = """
                           INSERT INTO dbo.FirstTenantFunnelEvents (TenantId, EventName, OccurredUtc)
                           VALUES (@TenantId, @EventName, @OccurredUtc);
                           """;

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        CommandDefinition cmd = new(
            sql,
            new
            {
                TenantId = tenantId,
                EventName = eventName,
                OccurredUtc = occurredUtc
            },
            cancellationToken: ct);

        await connection.ExecuteAsync(cmd);
    }
}

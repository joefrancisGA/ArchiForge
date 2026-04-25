using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;
using ArchLucid.Persistence.Data.Infrastructure;

using Dapper;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
///     Dapper-backed persistence for <see cref="ArchitectureRequest" /> entities, serialising request state as JSON.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class ArchitectureRequestRepository(IDbConnectionFactory connectionFactory)
    : IArchitectureRequestRepository
{
    public async Task CreateAsync(
        ArchitectureRequest request,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        const string sql = """
                           INSERT INTO ArchitectureRequests
                           (
                               RequestId,
                               SystemName,
                               Environment,
                               CloudProvider,
                               RequestJson,
                               CreatedUtc
                           )
                           VALUES
                           (
                               @RequestId,
                               @SystemName,
                               @Environment,
                               @CloudProvider,
                               @RequestJson,
                               @CreatedUtc
                           );
                           """;

        string json = JsonSerializer.Serialize(request, ContractJson.Default);

        (IDbConnection conn, bool ownsConnection) =
            await ExternalDbConnection.ResolveAsync(connectionFactory, connection, cancellationToken);

        try
        {
            await conn.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    request.RequestId,
                    request.SystemName,
                    request.Environment,
                    CloudProvider = request.CloudProvider.ToString(),
                    RequestJson = json,
                    CreatedUtc = DateTime.UtcNow
                },
                transaction,
                cancellationToken: cancellationToken));
        }
        finally
        {
            ExternalDbConnection.DisposeIfOwned(conn, ownsConnection);
        }
    }

    public async Task<ArchitectureRequest?> GetByIdAsync(string requestId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT RequestJson
                           FROM ArchitectureRequests
                           WHERE RequestId = @RequestId;
                           """;

        using IDbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string? json = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            sql,
            new { RequestId = requestId },
            cancellationToken: cancellationToken));

        if (json is null)
            return null;

        ArchitectureRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<ArchitectureRequest>(json, ContractJson.Default);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Request JSON for '{requestId}' could not be deserialized. " +
                "The stored JSON may be corrupt or written by an incompatible schema version.", ex);
        }

        return request
               ?? throw new InvalidOperationException(
                   $"Request JSON for '{requestId}' deserialized to null. " +
                   "The stored JSON may be empty or corrupt.");
    }
}

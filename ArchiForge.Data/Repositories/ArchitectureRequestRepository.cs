using System.Data;
using System.Text.Json;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;
using ArchiForge.Data.Infrastructure;

using Dapper;

namespace ArchiForge.Data.Repositories;

/// <summary>
/// Dapper-backed persistence for <see cref="ArchitectureRequest"/> entities, serialising request state as JSON.
/// </summary>
public sealed class ArchitectureRequestRepository(IDbConnectionFactory connectionFactory)
    : IArchitectureRequestRepository
{
    public async Task CreateAsync(ArchitectureRequest request, CancellationToken cancellationToken = default)
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

        using IDbConnection connection = connectionFactory.CreateConnection();

        await connection.ExecuteAsync(new CommandDefinition(
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
            cancellationToken: cancellationToken));
    }

    public async Task<ArchitectureRequest?> GetByIdAsync(string requestId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT RequestJson
            FROM ArchitectureRequests
            WHERE RequestId = @RequestId;
            """;

        using IDbConnection connection = connectionFactory.CreateConnection();

        string? json = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            sql,
            new
            {
                RequestId = requestId
            },
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

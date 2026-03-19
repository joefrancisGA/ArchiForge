using System.Text.Json;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;
using ArchiForge.Data.Infrastructure;
using Dapper;

namespace ArchiForge.Data.Repositories;

public sealed class ArchitectureRequestRepository(IDbConnectionFactory connectionFactory)
    : IArchitectureRequestRepository
{
    public async Task CreateAsync(ArchitectureRequest request, CancellationToken cancellationToken = default)
    {
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

        var json = JsonSerializer.Serialize(request, ContractJson.Default);

        using var connection = connectionFactory.CreateConnection();

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

        using var connection = connectionFactory.CreateConnection();

        var json = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            sql,
            new { RequestId = requestId },
            cancellationToken: cancellationToken));

        return json is null
            ? null
            : JsonSerializer.Deserialize<ArchitectureRequest>(json, ContractJson.Default);
    }
}
using System.Text.Json;

using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Advisory;

/// <summary>
/// Dapper implementation of <see cref="IRecommendationLearningProfileRepository"/> backed by <c>dbo.RecommendationLearningProfiles</c>.
/// Profiles are serialized to JSON on write and deserialized on read; dictionary comparers are normalized to <see cref="StringComparer.OrdinalIgnoreCase"/> after deserialization.
/// </summary>
/// <param name="connectionFactory">SQL connection factory (scoped in DI).</param>
public sealed class DapperRecommendationLearningProfileRepository(ISqlConnectionFactory connectionFactory)
    : IRecommendationLearningProfileRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public async Task SaveAsync(RecommendationLearningProfile profile, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(profile);

        const string sql = """
            INSERT INTO dbo.RecommendationLearningProfiles
            (
                ProfileId,
                TenantId,
                WorkspaceId,
                ProjectId,
                GeneratedUtc,
                ProfileJson
            )
            VALUES
            (
                @ProfileId,
                @TenantId,
                @WorkspaceId,
                @ProjectId,
                @GeneratedUtc,
                @ProfileJson
            );
            """;

        Guid profileId = Guid.NewGuid();
        string json = JsonSerializer.Serialize(profile, JsonOptions);

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    ProfileId = profileId,
                    profile.TenantId,
                    profile.WorkspaceId,
                    profile.ProjectId,
                    profile.GeneratedUtc,
                    ProfileJson = json
                },
                cancellationToken: ct));
    }

    public async Task<RecommendationLearningProfile?> GetLatestAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (1) ProfileJson
            FROM dbo.RecommendationLearningProfiles
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
            ORDER BY GeneratedUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        string? json = await connection.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId
                },
                cancellationToken: ct));

        if (string.IsNullOrWhiteSpace(json))
            return null;

        RecommendationLearningProfile? profile;
        try
        {
            profile = JsonSerializer.Deserialize<RecommendationLearningProfile>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"RecommendationLearningProfile JSON for tenant={tenantId}/workspace={workspaceId}/project={projectId} is corrupt.", ex);
        }

        return profile is null ? null : NormalizeDictionaryComparers(profile);
    }

    private static RecommendationLearningProfile NormalizeDictionaryComparers(RecommendationLearningProfile profile)
    {
        profile.CategoryWeights = new Dictionary<string, double>(profile.CategoryWeights, StringComparer.OrdinalIgnoreCase);
        profile.UrgencyWeights = new Dictionary<string, double>(profile.UrgencyWeights, StringComparer.OrdinalIgnoreCase);
        profile.SignalTypeWeights = new Dictionary<string, double>(profile.SignalTypeWeights, StringComparer.OrdinalIgnoreCase);
        return profile;
    }
}

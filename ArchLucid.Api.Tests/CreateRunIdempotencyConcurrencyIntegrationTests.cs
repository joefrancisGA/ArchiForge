using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Api.Tests.TestDtos;
using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Parallel POST <c>/v1/architecture/request</c> with the same <c>Idempotency-Key</c> must converge on a single authority run.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
[Collection("ArchLucidEnvMutation")]
public sealed class CreateRunIdempotencyConcurrencyIntegrationTests
{
    private const string SqlUnavailable =
        "API greenfield SQL tests need SQL Server. Set "
        + TestDatabaseEnvironment.ApiIntegrationSqlEnvironmentVariable
        + " or "
        + TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable
        + " (see docs/BUILD.md), or use Windows with LocalDB.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: true) },
    };

    private static bool IsSqlServerConfiguredForApiIntegration()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(TestDatabaseEnvironment.ApiIntegrationSqlEnvironmentVariable)))
            return true;

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable)))
            return true;

        return OperatingSystem.IsWindows();
    }

    private static StringContent JsonContent(object value)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static async Task<HttpResponseMessage[]> PostParallelArchitectureRequestAsync(
        HttpClient client,
        object body,
        string idempotencyKey,
        int parallel)
    {
        Task<HttpResponseMessage>[] tasks = new Task<HttpResponseMessage>[parallel];

        for (int i = 0; i < parallel; i++)
        {
            HttpRequestMessage request = new(HttpMethod.Post, "/v1/architecture/request")
            {
                Content = JsonContent(body),
            };

            request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
            tasks[i] = client.SendAsync(request);
        }

        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Under parallel POST, SQL can briefly return errors mapped to HTTP 503 (see <c>ApplicationProblemMapper</c>).
    /// CI runners are slower than local SQL — retry the whole burst with backoff instead of failing the idempotency assertion.
    /// </summary>
    private static async Task<HttpResponseMessage[]> PostParallelArchitectureRequestWithTransientRetryAsync(
        HttpClient client,
        object body,
        string idempotencyKey,
        int parallel,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 6;
        int delayMilliseconds = 300;
        HttpResponseMessage[] responses = await PostParallelArchitectureRequestAsync(client, body, idempotencyKey, parallel);

        for (int attempt = 0;
             attempt < maxAttempts - 1 && responses.Any(static r => r.StatusCode == HttpStatusCode.ServiceUnavailable);
             attempt++)
        {
            DisposeAll(responses);
            await Task.Delay(delayMilliseconds, cancellationToken);
            delayMilliseconds = Math.Min(delayMilliseconds * 2, 4000);
            responses = await PostParallelArchitectureRequestAsync(client, body, idempotencyKey, parallel);
        }

        return responses;
    }

    private static void DisposeAll(HttpResponseMessage[] responses)
    {
        foreach (HttpResponseMessage response in responses)
        {
            response.Dispose();
        }
    }

    [SkippableFact]
    public async Task Parallel_posts_with_same_idempotency_key_yield_single_run_id()
    {
        Skip.IfNot(IsSqlServerConfiguredForApiIntegration(), SqlUnavailable);

        await using GreenfieldSqlApiFactory factory = new();
        HttpClient client = factory.CreateClient();

        string idempotencyKey = "idem-conc-" + Guid.NewGuid().ToString("N");
        string requestId = "REQ-IDEM-" + Guid.NewGuid().ToString("N")[..12];
        object body = TestRequestFactory.CreateArchitectureRequest(requestId);

        const int parallel = 64;
        HttpResponseMessage[] responses = await PostParallelArchitectureRequestWithTransientRetryAsync(
            client,
            body,
            idempotencyKey,
            parallel,
            CancellationToken.None);

        try
        {
            List<string> runIds = [];

            foreach (HttpResponseMessage response in responses)
            {
                response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

                CreateRunResponseDto? dto = await response.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
                dto.Should().NotBeNull();
                dto.Run.RunId.Should().NotBeNullOrWhiteSpace();
                runIds.Add(dto.Run.RunId);
            }

            runIds.Distinct().Should().ContainSingle();
        }
        finally
        {
            DisposeAll(responses);
        }

        await using SqlConnection connection = new(factory.SqlConnectionString);
        await connection.OpenAsync(CancellationToken.None);

        int authorityRunCount = await CountRunsWithRequestIdAsync(connection, requestId, CancellationToken.None);
        authorityRunCount.Should().Be(1);
    }

    private static async Task<int> CountRunsWithRequestIdAsync(SqlConnection connection, string requestId, CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Runs
            WHERE ArchitectureRequestId = @RequestId;
            """;

        await using SqlCommand cmd = new(sql, connection);
        _ = cmd.Parameters.AddWithValue("@RequestId", requestId);
        object? scalar = await cmd.ExecuteScalarAsync(ct);

        return Convert.ToInt32(scalar);
    }
}

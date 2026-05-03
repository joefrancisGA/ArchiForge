using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Tests.TestDtos;
using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Parallel POST <c>/v1/architecture/request</c> with the same <c>Idempotency-Key</c> must converge on a single
///     authority run (SQL storage).
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

    private static bool IsSqlServerConfiguredForApiIntegration()
    {
        if (!string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(TestDatabaseEnvironment.ApiIntegrationSqlEnvironmentVariable)))
            return true;

        return !string.IsNullOrWhiteSpace(
                   Environment.GetEnvironmentVariable(TestDatabaseEnvironment.PersistenceSqlEnvironmentVariable)) ||
               OperatingSystem.IsWindows();
    }

    [SkippableFact]
    public async Task Parallel_posts_with_same_idempotency_key_yield_single_run_id()
    {
        Skip.IfNot(IsSqlServerConfiguredForApiIntegration(), SqlUnavailable);

        await using GreenfieldSqlApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        // WebApplicationFactory HttpClient defaults to 100s; parallel idempotency waits on sp_getapplock far longer.
        client.Timeout = TimeSpan.FromMinutes(16);
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        string idempotencyKey = "idem-conc-" + Guid.NewGuid().ToString("N");
        string requestId = "REQ-IDEM-" + Guid.NewGuid().ToString("N")[..12];
        object body = TestRequestFactory.CreateArchitectureRequest(requestId);

        const int parallel = 16;
        HttpResponseMessage[] responses =
            await ArchitectureRequestConcurrencyTestSupport.PostParallelArchitectureRequestWithTransientRetryAsync(
                client,
                body,
                idempotencyKey,
                parallel,
                10,
                500,
                CancellationToken.None);

        responses = await ArchitectureRequestConcurrencyTestSupport.ResolveServiceUnavailablePerResponseAsync(
            client,
            body,
            idempotencyKey,
            responses,
            25,
            CancellationToken.None);

        try
        {
            List<string> runIds = [];

            foreach (HttpResponseMessage response in responses)
            {
                response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

                CreateRunResponseDto? dto = await response.Content.ReadFromJsonAsync<CreateRunResponseDto>(
                    ArchitectureRequestConcurrencyTestSupport.JsonOptions);
                dto.Should().NotBeNull();
                dto.Run.RunId.Should().NotBeNullOrWhiteSpace();
                runIds.Add(dto.Run.RunId);
            }

            runIds.Distinct().Should().ContainSingle();
        }
        finally
        {
            ArchitectureRequestConcurrencyTestSupport.DisposeAll(responses);
        }

        await using SqlConnection connection = new(factory.SqlConnectionString);
        await connection.OpenAsync(CancellationToken.None);

        int authorityRunCount = await CountRunsWithRequestIdAsync(connection, requestId, CancellationToken.None);
        authorityRunCount.Should().Be(1);
    }

    private static async Task<int> CountRunsWithRequestIdAsync(SqlConnection connection, string requestId,
        CancellationToken ct)
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

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Api.Tests.TestDtos;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Parallel <c>POST /v1/admin/runs/archive-by-ids</c> for the same run. The default integration host uses
///     <c>ArchLucid:StorageProvider=InMemory</c>, so per-batch bodies can race; we assert HTTP 200 for every caller and
///     that the run no longer appears in <see cref="IRunRepository.ListByProjectAsync" /> (non-archived materialization).
///     Strict single-winner SQL behavior is covered by <c>SqlRunRepositoryArchiveByIdsConcurrencyTests</c>.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class ArchivalConcurrencyIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(null) }
    };

    private static StringContent JsonContent(object value)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    [SkippableFact]
    public async Task Five_parallel_archive_by_ids_all_200_and_run_drops_off_active_project_list()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        HttpResponseMessage createResponse = await client.PostAsync(
            "/v1/architecture/request",
            JsonContent(
                TestRequestFactory.CreateArchitectureRequest("REQ-ARCH-CONC-" + Guid.NewGuid().ToString("N")[..8])));

        createResponse.EnsureSuccessStatusCode();
        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;
        Guid runKey = Guid.Parse(runId);

        HttpResponseMessage executeResponse = await client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        const int parallel = 5;
        object body = new { runIds = new[] { runKey } };
        Task<HttpResponseMessage>[] tasks = new Task<HttpResponseMessage>[parallel];

        for (int i = 0; i < parallel; i++)
        {
            tasks[i] = client.PostAsync("/v1/admin/runs/archive-by-ids", JsonContent(body));
        }

        HttpResponseMessage[] responses = await Task.WhenAll(tasks);

        try
        {
            foreach (HttpResponseMessage response in responses)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            ScopeContext scope = new()
            {
                TenantId = ScopeIds.DefaultTenant,
                WorkspaceId = ScopeIds.DefaultWorkspace,
                ProjectId = ScopeIds.DefaultProject
            };

            IRunRepository runRepository = factory.Services.GetRequiredService<IRunRepository>();
            IReadOnlyList<RunRecord> active = await runRepository.ListByProjectAsync(
                scope,
                "EnterpriseRag",
                200,
                CancellationToken.None);
            active.Should().NotContain(r => r.RunId == runKey);
        }
        finally
        {
            foreach (HttpResponseMessage response in responses)
            {
                response.Dispose();
            }
        }
    }
}

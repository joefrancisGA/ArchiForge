using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Parallel <c>POST /v1/architecture/request</c> with the same <c>Idempotency-Key</c> converges on one authority run
///     id.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class ArchitectureRequestIdempotencyConcurrencyIntegrationTests
{
    [SkippableFact]
    public async Task Sixteen_parallel_posts_same_idempotency_key_single_distinct_run_id()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        string idempotencyKey = "idem-arch-req16-" + Guid.NewGuid().ToString("N");
        string requestId = "REQ-ARCH16-" + Guid.NewGuid().ToString("N")[..10];
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
            HashSet<string> runIds = [];

            foreach (HttpResponseMessage response in responses)
            {
                response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
                CreateRunResponseDto? dto = await response.Content.ReadFromJsonAsync<CreateRunResponseDto>(
                    ArchitectureRequestConcurrencyTestSupport.JsonOptions);
                dto.Should().NotBeNull();
                runIds.Add(dto.Run.RunId);
            }

            runIds.Should().HaveCount(1);
        }
        finally
        {
            ArchitectureRequestConcurrencyTestSupport.DisposeAll(responses);
        }
    }
}

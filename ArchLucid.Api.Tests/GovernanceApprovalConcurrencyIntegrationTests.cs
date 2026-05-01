using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Parallel governance approve calls: exactly one terminal success; peers receive <c>400</c> or <c>409</c>.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class GovernanceApprovalConcurrencyIntegrationTests(ArchLucidApiFactory factory)
    : IntegrationTestBase(factory)
{
    private async Task<string> CreateRunAsync(string requestId)
    {
        HttpResponseMessage response = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest(requestId)));
        response.EnsureSuccessStatusCode();
        CreateRunResponseDto? payload = await response.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        return payload!.Run.RunId;
    }

    [SkippableFact]
    public async Task Thirty_two_parallel_approves_single_ok_terminal_outcome()
    {
        string runId = await CreateRunAsync("REQ-GOV-32-" + Guid.NewGuid().ToString("N")[..8]);
        var submitBody = new
        {
            RunId = runId, ManifestVersion = "v1", SourceEnvironment = "dev", TargetEnvironment = "test"
        };

        HttpResponseMessage submitResponse =
            await Client.PostAsync("/v1/governance/approval-requests", JsonContent(submitBody));
        submitResponse.EnsureSuccessStatusCode();
        GovernanceApprovalResponseDto? submitted =
            await submitResponse.Content.ReadFromJsonAsync<GovernanceApprovalResponseDto>(JsonOptions);

        string url = $"/v1/governance/approval-requests/{submitted!.ApprovalRequestId}/approve";
        var approveBody = new { ReviewedBy = "reviewer-32", ReviewComment = "parallel-32" };

        const int parallel = 32;
        Task<HttpResponseMessage>[] tasks = Enumerable.Range(0, parallel)
            .Select(_ => Client.PostAsync(url, JsonContent(approveBody)))
            .ToArray();

        HttpResponseMessage[] responses = await Task.WhenAll(tasks);

        try
        {
            int ok = responses.Count(static r => r.StatusCode == HttpStatusCode.OK);
            int bad = responses.Count(static r => r.StatusCode == HttpStatusCode.BadRequest);
            int conflict = responses.Count(static r => r.StatusCode == HttpStatusCode.Conflict);
            ok.Should().Be(1);
            (ok + bad + conflict).Should().Be(parallel);
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

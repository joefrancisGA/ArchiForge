using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using ArchLucid.Contracts.Abstractions.Integrations;
using ArchLucid.Core.Comparison;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace ArchLucid.Integrations.AzureDevOps.Tests;

public sealed class AzureDevOpsPullRequestDecoratorTests
{
    private static readonly JsonSerializerOptions CompareBodyJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public async Task PostManifestDeltaAsync_sends_status_and_thread_with_basic_auth()
    {
        List<HttpRequestMessage> captured = [];
        using HttpMessageHandler stub = new CapturingHandler(captured);
        using HttpClient httpClient = new(stub, false);

        AzureDevOpsIntegrationOptions opt = new()
        {
            Organization = "contoso",
            Project = "Fabrikam",
            PersonalAccessToken = "pat-test-token"
        };

        AzureDevOpsPullRequestDecorator sut = new(
            httpClient,
            Options.Create(opt),
            NullLogger<AzureDevOpsPullRequestDecorator>.Instance);

        Guid repoId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        AzureDevOpsPullRequestTarget target = new(repoId, 42);

        AzureDevOpsManifestDeltaRequest request = new(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            null,
            []);

        await sut.PostManifestDeltaAsync(request, target, CancellationToken.None);

        Assert.Equal(2, captured.Count);

        HttpRequestMessage statusReq = captured[0];
        Assert.NotNull(statusReq.Headers.Authorization);
        Assert.Equal("Basic", statusReq.Headers.Authorization.Scheme);
        Assert.Equal(HttpMethod.Post, statusReq.Method);
        Assert.Contains("/pullrequests/42/statuses", statusReq.RequestUri?.ToString(), StringComparison.Ordinal);

        HttpRequestMessage threadReq = captured[1];
        Assert.NotNull(threadReq.Headers.Authorization);
        Assert.Equal("Basic", threadReq.Headers.Authorization.Scheme);
        Assert.Equal(HttpMethod.Post, threadReq.Method);
        Assert.Contains("/pullrequests/42/threads", threadReq.RequestUri?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostManifestDeltaAsync_skips_when_pat_missing()
    {
        List<HttpRequestMessage> captured = [];
        using HttpMessageHandler stub = new CapturingHandler(captured);
        using HttpClient httpClient = new(stub, false);

        AzureDevOpsIntegrationOptions opt = new()
        {
            Organization = "o",
            Project = "p",
            PersonalAccessToken = ""
        };

        AzureDevOpsPullRequestDecorator sut = new(
            httpClient,
            Options.Create(opt),
            NullLogger<AzureDevOpsPullRequestDecorator>.Instance);

        AzureDevOpsManifestDeltaRequest request = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            []);

        await sut.PostManifestDeltaAsync(request, new AzureDevOpsPullRequestTarget(Guid.NewGuid(), 1),
            CancellationToken.None);

        Assert.Empty(captured);
    }

    [Fact]
    public async Task PostManifestDeltaAsync_thread_contains_compare_markdown_and_operator_run_link_when_compare_ok()
    {
        Guid baseRun = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid targetRun = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Guid tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid workspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid projectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        ComparisonResult compareBody = new()
        {
            BaseRunId = baseRun,
            TargetRunId = targetRun,
            TotalDeltaCount = 3,
            SummaryHighlights = ["decisions tightened"]
        };

        string compareJson = JsonSerializer.Serialize(compareBody, CompareBodyJsonOptions);

        using RoutingHandler stub = new(compareJson);
        using HttpClient httpClient = new(stub, false);

        AzureDevOpsIntegrationOptions opt = new()
        {
            Organization = "contoso",
            Project = "Fabrikam",
            PersonalAccessToken = "pat-test-token",
            ArchLucidApiBaseUrl = "https://api.test",
            ArchLucidApiKey = "test-key",
            StatusTargetUrl = "https://ops.example"
        };

        AzureDevOpsPullRequestDecorator sut = new(
            httpClient,
            Options.Create(opt),
            NullLogger<AzureDevOpsPullRequestDecorator>.Instance);

        AzureDevOpsManifestDeltaRequest request = new(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            targetRun,
            tenantId,
            workspaceId,
            projectId,
            baseRun,
            [new AuthorityRunCompletedFindingLink("f1", "https://ops.example/x", "High")]);

        await sut.PostManifestDeltaAsync(
            request,
            new AzureDevOpsPullRequestTarget(Guid.Parse("11111111-1111-1111-1111-111111111112"), 7),
            CancellationToken.None);

        Assert.Equal(3, stub.CallCount);
        Assert.NotNull(stub.CompareUri);
        Assert.EndsWith("/v1/compare", stub.CompareUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains($"baseRunId={baseRun:D}", stub.CompareUri.Query, StringComparison.Ordinal);
        Assert.Contains($"targetRunId={targetRun:D}", stub.CompareUri.Query, StringComparison.Ordinal);
        Assert.Equal("test-key", stub.CompareApiKey);

        Assert.NotNull(stub.ThreadJson);
        using JsonDocument doc = JsonDocument.Parse(stub.ThreadJson!);
        string content = doc.RootElement.GetProperty("comments")[0].GetProperty("content").GetString() ?? "";

        Assert.Contains("decisions tightened", content, StringComparison.Ordinal);
        Assert.Contains($"https://ops.example/runs/{targetRun:D}", content, StringComparison.Ordinal);
    }

    private sealed class RoutingHandler(string compareJson) : HttpMessageHandler
    {
        internal int CallCount
        {
            get;
            private set;
        }

        internal Uri? CompareUri
        {
            get;
            private set;
        }

        internal string? CompareApiKey
        {
            get;
            private set;
        }

        internal string? ThreadJson
        {
            get;
            private set;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            string url = request.RequestUri?.AbsoluteUri ?? string.Empty;

            if (url.Contains("/v1/compare", StringComparison.Ordinal))
            {
                CompareUri = request.RequestUri;
                CompareApiKey = request.Headers.TryGetValues("X-Api-Key", out IEnumerable<string>? keys)
                    ? keys.First()
                    : null;

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(compareJson, Encoding.UTF8, "application/json")
                };
            }

            if (url.Contains("/threads", StringComparison.Ordinal) && request.Content is not null)

                ThreadJson = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly List<HttpRequestMessage> _captured;

        internal CapturingHandler(List<HttpRequestMessage> captured)
        {
            _captured = captured;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpRequestMessage snapshot = new(request.Method, request.RequestUri)
            {
                Version = request.Version
            };

            if (request.Headers.Authorization is not null)

                snapshot.Headers.Authorization = new AuthenticationHeaderValue(
                    request.Headers.Authorization.Scheme,
                    request.Headers.Authorization.Parameter);

            _captured.Add(snapshot);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}

using System.Net;
using System.Net.Http.Headers;

using ArchLucid.Contracts.Abstractions.Integrations;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace ArchLucid.Integrations.AzureDevOps.Tests;

public sealed class AzureDevOpsPullRequestDecoratorTests
{
    [Fact]
    public async Task PostManifestDeltaAsync_sends_status_and_thread_with_basic_auth()
    {
        List<HttpRequestMessage> captured = [];
        using HttpMessageHandler stub = new CapturingHandler(captured);
        using HttpClient httpClient = new(stub, disposeHandler: false);

        AzureDevOpsIntegrationOptions opt = new()
        {
            Organization = "contoso",
            Project = "Fabrikam",
            PersonalAccessToken = "pat-test-token",
        };

        AzureDevOpsPullRequestDecorator sut = new(
            httpClient,
            Options.Create(opt),
            NullLogger<AzureDevOpsPullRequestDecorator>.Instance);

        Guid repoId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        AzureDevOpsPullRequestTarget target = new(repoId, 42);

        await sut.PostManifestDeltaAsync(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            target,
            CancellationToken.None);

        Assert.Equal(2, captured.Count);
        Assert.All(
            captured,
            r =>
            {
                Assert.NotNull(r.Headers.Authorization);
                Assert.Equal("Basic", r.Headers.Authorization?.Scheme);
            });

        HttpRequestMessage statusReq = captured[0];
        Assert.Equal(HttpMethod.Post, statusReq.Method);
        Assert.Contains("/pullrequests/42/statuses", statusReq.RequestUri?.ToString(), StringComparison.Ordinal);

        HttpRequestMessage threadReq = captured[1];
        Assert.Equal(HttpMethod.Post, threadReq.Method);
        Assert.Contains("/pullrequests/42/threads", threadReq.RequestUri?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostManifestDeltaAsync_skips_when_pat_missing()
    {
        List<HttpRequestMessage> captured = [];
        using HttpMessageHandler stub = new CapturingHandler(captured);
        using HttpClient httpClient = new(stub, disposeHandler: false);

        AzureDevOpsIntegrationOptions opt = new()
        {
            Organization = "o",
            Project = "p",
            PersonalAccessToken = "",
        };

        AzureDevOpsPullRequestDecorator sut = new(
            httpClient,
            Options.Create(opt),
            NullLogger<AzureDevOpsPullRequestDecorator>.Instance);

        await sut.PostManifestDeltaAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new AzureDevOpsPullRequestTarget(Guid.NewGuid(), 1),
            CancellationToken.None);

        Assert.Empty(captured);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly List<HttpRequestMessage> _captured;

        internal CapturingHandler(List<HttpRequestMessage> captured)
        {
            _captured = captured;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Clone URI/method for assertions; caller may dispose the request after SendAsync completes.
            HttpRequestMessage snapshot = new(request.Method, request.RequestUri)
            {
                Version = request.Version,
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

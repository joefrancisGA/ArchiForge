using System.Net;

using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class SecurityTrustPublishCommandTests
{
    [Fact]
    public async Task ExecutePublicationAsync_posts_json_and_returns_success()
    {
        CapturingHandler handler = new();
        using HttpClient http = new(handler);
        http.BaseAddress = new Uri("http://localhost:5555/");

        SecurityTrustPublishCommandOptions? opts = SecurityTrustPublishCommandOptions.Parse(
            [
                "--kind",
                "pen-test",
                "--date",
                "2026-07-29",
                "--summary-url",
                "https://example.com/redacted.md",
                "--assessment-code",
                "2026-Q2"
            ],
            out string? error);

        error.Should().BeNull();

        int code = await SecurityTrustPublishCommand.ExecutePublicationAsync(http, opts!, CancellationToken.None);

        code.Should().Be(CliExitCode.Success);
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().Contain("v1/admin/security-trust/publications");

        string body = await handler.LastRequest.Content!.ReadAsStringAsync(CancellationToken.None);
        body.Should().Contain("2026-07-29");
        body.Should().Contain("2026-Q2");
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest
        {
            get;
            private set;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }
    }
}

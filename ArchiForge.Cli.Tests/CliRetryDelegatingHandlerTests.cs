using System.Net;
using System.Net.Http;

using ArchiForge.Cli;

using FluentAssertions;

namespace ArchiForge.Cli.Tests;

/// <summary>
/// Deterministic “chaos” for the CLI HTTP stack: first outbound attempt fails, retry succeeds.
/// Validates <see cref="CliRetryDelegatingHandler"/> + Polly pipeline without external Simmy dependency.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CliRetryDelegatingHandlerTests
{
    [Fact]
    public async Task SendAsync_Retries_after_transient_500()
    {
        int attempt = 0;
        HttpMessageHandler inner = new LambdaHandler((_, _) =>
        {
            attempt++;

            if (attempt == 1)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        using HttpClient http = new(new CliRetryDelegatingHandler { InnerHandler = inner }, disposeHandler: true);

        HttpResponseMessage response = await http.GetAsync("http://localhost/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        attempt.Should().BeGreaterThan(1);
    }

    private sealed class LambdaHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send =
            send ?? throw new ArgumentNullException(nameof(send));

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _send(request, cancellationToken);
        }
    }
}

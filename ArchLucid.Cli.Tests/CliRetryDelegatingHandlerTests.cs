using System.Net;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

/// <summary>
///     Deterministic “chaos” for the CLI HTTP stack: first outbound attempt fails, retry succeeds.
///     Validates <see cref="CliRetryDelegatingHandler" /> + Polly pipeline without external Simmy dependency.
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

            return Task.FromResult(attempt == 1 ? new HttpResponseMessage(HttpStatusCode.InternalServerError) : new HttpResponseMessage(HttpStatusCode.OK));
        });

        using HttpRequestMessage request = new(HttpMethod.Get, "http://localhost/ping");

        using HttpMessageInvoker invoker =
            new(new CliRetryDelegatingHandler { InnerHandler = inner }, disposeHandler: true);

        using HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        attempt.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task SendAsync_with_zero_retries_does_not_retry_on_500()
    {
        int attempt = 0;
        HttpMessageHandler inner = new LambdaHandler((_, _) =>
        {
            attempt++;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        });

        CliResilienceOptions options = new()
        {
            MaxRetryAttempts = 0
        };

        using HttpRequestMessage request = new(HttpMethod.Get, "http://localhost/ping");

        using HttpMessageInvoker invoker =
            new(new CliRetryDelegatingHandler(options) { InnerHandler = inner }, disposeHandler: true);

        using HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        attempt.Should().Be(1);
    }

    private sealed class LambdaHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send =
            send ?? throw new ArgumentNullException(nameof(send));

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _send(request, cancellationToken);
        }
    }
}

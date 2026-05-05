using System.Net;

using ArchLucid.Application.Governance;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Polly;

namespace ArchLucid.Application.Tests.Governance;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class GovernanceSlaEscalationWebhookRetryPipelineTests
{
    private sealed class StatusSequenceHandler(HttpStatusCode status, int successAfterAttemptInclusive)
        : HttpMessageHandler
    {
        private readonly HttpStatusCode _status = status;

        private int _attemptOrdinal;

        public int SendInvocationCount;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _attemptOrdinal++;

            SendInvocationCount++;

            return Task.FromResult(_attemptOrdinal == successAfterAttemptInclusive
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : new HttpResponseMessage(_status));
        }
    }

    [Fact]
    public async Task ExecuteAsync_OnPersistent503_InvokesHandlerFourTimes_before_returning_terminal_503()
    {
        StatusSequenceHandler capturing = new(HttpStatusCode.ServiceUnavailable, successAfterAttemptInclusive: 999);
        using HttpClient httpClient = new(capturing) { Timeout = TimeSpan.FromSeconds(30) };

        ResiliencePipeline<HttpResponseMessage> pipeline =
            GovernanceSlaEscalationWebhookRetryPipeline.Create(
                NullLogger.Instance,
                "test-approval",
                static _ => TimeSpan.Zero);

        using HttpResponseMessage response = await pipeline.ExecuteAsync(
            async ct =>
            {
                using HttpRequestMessage req = new(HttpMethod.Post, "https://example.test/governance/sla");
                return await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            },
            CancellationToken.None);

        capturing.SendInvocationCount.Should().Be(4);
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task ExecuteAsync_OnSecondAttemptSuccess_short_circuits_retries()
    {
        StatusSequenceHandler capturing =
            new(HttpStatusCode.InternalServerError, successAfterAttemptInclusive: 2);
        using HttpClient httpClient = new(capturing) { Timeout = TimeSpan.FromSeconds(30) };

        ResiliencePipeline<HttpResponseMessage> pipeline =
            GovernanceSlaEscalationWebhookRetryPipeline.Create(NullLogger.Instance, "tid", static _ => TimeSpan.Zero);

        using HttpResponseMessage response = await pipeline.ExecuteAsync(
            async ct =>
            {
                using HttpRequestMessage req = new(HttpMethod.Post, "https://example.test/webhook");

                return await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            },
            CancellationToken.None);

        capturing.SendInvocationCount.Should().Be(2);

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_On400_does_not_retry()
    {
        StatusSequenceHandler capturing = new(HttpStatusCode.BadRequest, successAfterAttemptInclusive: 999);
        using HttpClient httpClient = new(capturing) { Timeout = TimeSpan.FromSeconds(30) };

        ResiliencePipeline<HttpResponseMessage> pipeline =
            GovernanceSlaEscalationWebhookRetryPipeline.Create(NullLogger.Instance, "bad", static _ => TimeSpan.Zero);

        using HttpResponseMessage response = await pipeline.ExecuteAsync(
            async ct =>
            {
                using HttpRequestMessage req = new(HttpMethod.Post, "https://example.test/x");

                return await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            },
            CancellationToken.None);

        capturing.SendInvocationCount.Should().Be(1);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

using System.Net;
using System.Text;

using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ComplianceReportAuditLiveSampleFetcherTests
{
    [Fact]
    public async Task TryFetchAsync_counts_event_types_on_200()
    {
        using HttpClient http = new(new OkAuditHandler(), disposeHandler: true) { BaseAddress = new Uri("http://localhost/") };

        ComplianceReportAuditLiveSample sample =
            await ComplianceReportAuditLiveSampleFetcher.TryFetchAsync(http, CancellationToken.None);

        sample.ApiReached.Should().BeTrue();
        sample.EventsInPage.Should().Be(2);
        sample.EventTypeCounts["RunStarted"].Should().Be(2);
        sample.OldestUtc.Should().BeBefore(sample.NewestUtc!.Value);
    }

    [Fact]
    public async Task TryFetchAsync_reports_auth_failure()
    {
        using HttpClient http = new(new StatusHandler(HttpStatusCode.Unauthorized), disposeHandler: true)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        ComplianceReportAuditLiveSample sample =
            await ComplianceReportAuditLiveSampleFetcher.TryFetchAsync(http, CancellationToken.None);

        sample.ApiReached.Should().BeFalse();
        sample.ErrorNote.Should().Contain("401");
    }

    private sealed class OkAuditHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            const string json =
                "{\"items\":[{\"eventType\":\"RunStarted\",\"occurredUtc\":\"2026-05-01T00:00:00Z\"}," +
                "{\"eventType\":\"RunStarted\",\"occurredUtc\":\"2026-05-02T00:00:00Z\"}]}";

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }

    private sealed class StatusHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }
}

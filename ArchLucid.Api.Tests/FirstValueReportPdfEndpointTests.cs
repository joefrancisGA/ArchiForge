using System.Net;
using System.Net.Http.Headers;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     HTTP coverage for <c>POST /v1/pilots/runs/{runId}/first-value-report.pdf</c> â€” the in-product CTA
///     that produces a sponsor-shareable PDF projection of the canonical first-value-report Markdown.
///     404 on unknown run is the stable contract surface (parity with the Markdown sibling).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class FirstValueReportPdfEndpointTests(ArchLucidApiFactory factory)
    : IntegrationTestBase(factory)
{
    [SkippableFact]
    public async Task PostFirstValueReportPdf_WhenRunUnknown_Returns404Problem()
    {
        Guid runId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        HttpRequestMessage request = new(HttpMethod.Post, $"/v1/pilots/runs/{runId:D}/first-value-report.pdf");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

        HttpResponseMessage response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task PostFirstValueReportPdf_NonceRunId_DoesNotRequireStandardTier()
    {
        // Mirrors the Markdown sibling auth shape: a 404 (not 402) for an unknown run id confirms the
        // endpoint is gated only by ReadAuthority and does not silently introduce a Standard-tier paywall.
        HttpRequestMessage request = new(
            HttpMethod.Post,
            "/v1/pilots/runs/00000000-0000-0000-0000-000000000001/first-value-report.pdf");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

        HttpResponseMessage response = await Client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.PaymentRequired);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.OK);
    }
}

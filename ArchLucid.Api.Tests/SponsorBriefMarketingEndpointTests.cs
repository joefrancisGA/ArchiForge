using System.Net;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>HTTP coverage for <c>GET /v1/marketing/sponsor-brief.pdf</c> — anonymous PDF from Executive Sponsor Brief.</summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class SponsorBriefMarketingEndpointTests : IClassFixture<ArchLucidApiFactory>
{
    private readonly ArchLucidApiFactory _factory;

    public SponsorBriefMarketingEndpointTests(ArchLucidApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSponsorBriefPdf_returns_pdf_bytes()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/v1/marketing/sponsor-brief.pdf");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");

        byte[] body = await response.Content.ReadAsByteArrayAsync();
        body.Length.Should().BeGreaterThan(500);
        ReadOnlySpan<byte> head = body.AsSpan(0, 5);
        head.SequenceEqual("%PDF-"u8).Should().BeTrue();
    }
}

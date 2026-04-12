using System.Net;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
/// OWASP ZAP baseline (and common spiders) request <c>/</c>, <c>/robots.txt</c>, and <c>/sitemap.xml</c> and expect HTTP 200.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PublicCrawlerHintsIntegrationTests(OpenApiContractWebAppFactory factory)
    : IClassFixture<OpenApiContractWebAppFactory>
{
    [Theory]
    [InlineData("/")]
    [InlineData("/robots.txt")]
    [InlineData("/sitemap.xml")]
    public async Task Anonymous_GET_returns_OK(string path)
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

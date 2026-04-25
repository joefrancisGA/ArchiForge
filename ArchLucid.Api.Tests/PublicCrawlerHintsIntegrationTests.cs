using System.Net;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     OWASP ZAP baseline (and common spiders) request <c>/</c>, <c>/robots.txt</c>, and <c>/sitemap.xml</c> and expect
///     HTTP 200.
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
        response.Headers.TryGetValues("Cache-Control", out IEnumerable<string>? cc).Should().BeTrue();
        string.Join(", ", cc!).Should().Contain("public", "ZAP 10049-1: crawler stubs use short public caching");
        response.Headers.TryGetValues("Cross-Origin-Embedder-Policy", out IEnumerable<string>? coep).Should().BeTrue();
        string.Join(", ", coep!).Should().Be("require-corp", "ZAP 90004-2");
        response.Headers.TryGetValues("Cross-Origin-Opener-Policy", out IEnumerable<string>? coop).Should().BeTrue();
        string.Join(", ", coop!).Should().Be("same-origin", "ZAP 90004-3");
    }
}

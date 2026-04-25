using System.Net;
using System.Net.Http.Headers;
using System.Text;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     HTTP coverage for <c>POST /v1/pilots/board-pack.pdf</c> — quarterly board pack (
///     <see cref="ArchLucid.Core.Authorization.ArchLucidPolicies.ExecuteAuthority" />, Standard+ tier).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class BoardPackPdfEndpointTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task PostBoardPackPdf_WhenQuarterOutOfRange_Returns400Problem()
    {
        using HttpResponseMessage res = await Client.PostAsync(
            "/v1/pilots/board-pack.pdf",
            JsonContent(new { year = 2026, quarter = 9 }));

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostBoardPackPdf_WhenValidQuarter_ReturnsPdf()
    {
        HttpRequestMessage request = new(HttpMethod.Post, "/v1/pilots/board-pack.pdf")
        {
            Content = JsonContent(new { year = 2026, quarter = 1 })
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

        using HttpResponseMessage res = await Client.SendAsync(request);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        byte[] body = await res.Content.ReadAsByteArrayAsync();
        Encoding.ASCII.GetString(body.AsSpan(0, Math.Min(5, body.Length))).Should().StartWith("%PDF");
    }
}

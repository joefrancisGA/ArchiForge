using System.Net;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Confirms Asp.Versioning response headers are present on versioned admin routes.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class ApiVersioningResponseHeadersIntegrationTests(ArchLucidApiFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Versioned_admin_route_exposes_supported_api_versions_header()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/admin/diagnostics/outboxes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("api-supported-versions", out IEnumerable<string>? values).Should().BeTrue();
        string joined = string.Join(",", values ?? []);
        joined.Should().Contain("1.0");
    }
}

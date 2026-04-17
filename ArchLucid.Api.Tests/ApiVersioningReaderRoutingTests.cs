using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Verifies optional <c>api-version</c> query and header readers work alongside URL segment versioning.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class ApiVersioningReaderRoutingTests(OpenApiContractWebAppFactory factory)
    : IClassFixture<OpenApiContractWebAppFactory>
{
    private const string OpenApiDocumentPath = "/openapi/v1.json";

    [Fact]
    public async Task OpenApi_document_loads_when_api_version_is_sent_as_query_string()
    {
        using HttpClient client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using HttpResponseMessage response =
            await client.GetAsync(OpenApiDocumentPath + "?api-version=1.0");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenApi_document_loads_when_api_version_is_sent_as_header()
    {
        using HttpClient client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using HttpRequestMessage request = new(HttpMethod.Get, OpenApiDocumentPath);
        request.Headers.TryAddWithoutValidation("api-version", "1.0");

        using HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task Version_endpoint_loads_without_version_segment()
    {
        using HttpClient client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using HttpResponseMessage response = await client.GetAsync("/version");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}

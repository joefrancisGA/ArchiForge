using System.Net;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace ArchLucid.Api.Tests;

public sealed class ScalarApiReferenceTests
{
    [Fact]
    public async Task Development_host_serves_scalar_reference()
    {
        await using WebApplicationFactory<Program> factory = new OpenApiContractWebAppFactory();
        HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri("/scalar/v1", UriKind.Relative));

        response.StatusCode.Should()
            .BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.MovedPermanently);
    }
}

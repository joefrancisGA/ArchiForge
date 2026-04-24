using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.Swagger;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Ensures Swashbuckle can materialize the v1 document (same generator as <c>/swagger/v1/swagger.json</c>).
/// </summary>
[Trait("Category", "Integration")]
public sealed class SwaggerDocumentGenerationSmokeTests
{
    [Fact]
    public void GetSwagger_v1_succeeds_for_default_contract_test_host()
    {
        using WebApplicationFactory<Program> factory = new OpenApiContractWebAppFactory();

        using IServiceScope scope = factory.Services.CreateScope();
        ISwaggerProvider swagger = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();

        OpenApiDocument document = swagger.GetSwagger("v1");

        document.Should().NotBeNull();
        document.Paths.Should().NotBeEmpty();
    }
}

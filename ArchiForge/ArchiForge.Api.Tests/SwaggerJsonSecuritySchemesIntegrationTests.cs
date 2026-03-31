using System.Text.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Swashbuckle.AspNetCore.Swagger;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Asserts Swashbuckle output reflects auth mode: JWT (Entra) vs DevelopmentBypass.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SwaggerJsonSecuritySchemesIntegrationTests : IClassFixture<SwaggerJsonJwtBearerWebAppFactory>
{
    private readonly SwaggerJsonJwtBearerWebAppFactory _factory;

    public SwaggerJsonSecuritySchemesIntegrationTests(SwaggerJsonJwtBearerWebAppFactory factory) => _factory = factory;

    [Fact]
    public void Swagger_document_with_JwtBearer_includes_Bearer_scheme_and_document_security()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISwaggerProvider swagger = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();

        Microsoft.OpenApi.OpenApiDocument document = swagger.GetSwagger("v1");

        document.Components.Should().NotBeNull();
        document.Components!.SecuritySchemes.Should().ContainKey("Bearer");

        document.Security.Should().NotBeNull();
        document.Security!.Count.Should().BeGreaterThan(0);

        OpenApiSecurityRequirement first = document.Security[0];
        first.Should().ContainSingle();
        first.Keys.Should().ContainSingle(k => string.Equals(k.ReferenceId, "Bearer", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Swagger_json_with_JwtBearer_round_trips_over_http()
    {
        using HttpClient client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using HttpResponseMessage response = await client.GetAsync("/swagger/v1/swagger.json");
        string json = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue("swagger.json body: {0}", json);

        using JsonDocument doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme")
            .GetString()
            .Should().Be("bearer");
    }

    [Fact]
    public async Task Swagger_json_with_DevelopmentBypass_omits_Bearer_security_scheme()
    {
        using WebApplicationFactory<Program> defaultFactory = new OpenApiContractWebAppFactory();
        using HttpClient client = defaultFactory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using HttpResponseMessage response = await client.GetAsync("/swagger/v1/swagger.json");
        string json = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue("swagger.json body: {0}", json);

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        if (!root.TryGetProperty("components", out JsonElement components))
        {
            return;
        }

        if (!components.TryGetProperty("securitySchemes", out JsonElement schemes))
        {
            return;
        }

        schemes.TryGetProperty("Bearer", out _).Should().BeFalse();
    }
}

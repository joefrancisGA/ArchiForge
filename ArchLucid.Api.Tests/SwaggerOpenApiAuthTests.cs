using ArchLucid.Api.Swagger;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Api.Tests;

public sealed class SwaggerOpenApiAuthTests
{
    [Theory]
    [InlineData("JwtBearer", SwaggerOpenApiAuth.BearerSchemeId)]
    [InlineData("jwtbearer", SwaggerOpenApiAuth.BearerSchemeId)]
    [InlineData("ApiKey", SwaggerOpenApiAuth.ApiKeySchemeId)]
    [InlineData("apikey", SwaggerOpenApiAuth.ApiKeySchemeId)]
    public void ResolveSecuritySchemeId_returns_scheme_for_auth_modes(string mode, string expectedId)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ArchLucidAuth:Mode"] = mode }).Build();

        string? id = SwaggerOpenApiAuth.ResolveSecuritySchemeId(configuration);

        id.Should().Be(expectedId);
    }

    [Fact]
    public void ResolveSecuritySchemeId_returns_null_for_DevelopmentBypass()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ArchLucidAuth:Mode"] = "DevelopmentBypass" }).Build();

        SwaggerOpenApiAuth.ResolveSecuritySchemeId(configuration).Should().BeNull();
    }

    [Fact]
    public void ResolveSecuritySchemeId_returns_null_when_mode_missing()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>()).Build();

        SwaggerOpenApiAuth.ResolveSecuritySchemeId(configuration).Should().BeNull();
    }
}

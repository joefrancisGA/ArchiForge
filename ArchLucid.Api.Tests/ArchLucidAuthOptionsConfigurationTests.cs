using System.Security.Claims;

using ArchiForge.Api.Auth.Models;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

namespace ArchiForge.Api.Tests;

public sealed class ArchiForgeAuthOptionsConfigurationTests
{
    [Fact]
    public void GetSection_binds_name_claim_type_from_configuration()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForgeAuth:Mode"] = "JwtBearer",
            ["ArchiForgeAuth:NameClaimType"] = "preferred_username",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        ArchiForgeAuthOptions? options = configuration
            .GetSection(ArchiForgeAuthOptions.SectionName)
            .Get<ArchiForgeAuthOptions>();

        options.Should().NotBeNull();
        options.NameClaimType.Should().Be("preferred_username");
    }

    [Fact]
    public void GetSection_omitted_name_claim_type_defaults_to_claim_types_name()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForgeAuth:Mode"] = "JwtBearer",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        ArchiForgeAuthOptions? options = configuration
            .GetSection(ArchiForgeAuthOptions.SectionName)
            .Get<ArchiForgeAuthOptions>();

        options.Should().NotBeNull();
        options.NameClaimType.Should().Be(ClaimTypes.Name);
    }
}

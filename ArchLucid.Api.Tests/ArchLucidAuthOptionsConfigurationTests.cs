using System.Security.Claims;

using ArchLucid.Api.Auth.Models;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Api.Tests;

public sealed class ArchLucidAuthOptionsConfigurationTests
{
    [Fact]
    public void GetSection_binds_name_claim_type_from_configuration()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:NameClaimType"] = "preferred_username",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        ArchLucidAuthOptions? options = configuration
            .GetSection(ArchLucidAuthOptions.SectionName)
            .Get<ArchLucidAuthOptions>();

        options.Should().NotBeNull();
        options.NameClaimType.Should().Be("preferred_username");
    }

    [Fact]
    public void GetSection_omitted_name_claim_type_defaults_to_claim_types_name()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucidAuth:Mode"] = "JwtBearer",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        ArchLucidAuthOptions? options = configuration
            .GetSection(ArchLucidAuthOptions.SectionName)
            .Get<ArchLucidAuthOptions>();

        options.Should().NotBeNull();
        options.NameClaimType.Should().Be(ClaimTypes.Name);
    }
}

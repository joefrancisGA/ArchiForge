using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Tenancy;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Unit")]
public sealed class PackagingTierProblemDetailsFactoryTests
{
    [Fact]
    public void CreatePaymentRequired_returns_404_resource_not_found_without_tier_extensions()
    {
        ServiceCollection services = [];
        services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?> { ["ArchLucid:PublicSite:BaseUrl"] = "https://branded.example" })
                .Build());
        ServiceProvider sp = services.BuildServiceProvider();
        DefaultHttpContext http = new()
        {
            RequestServices = sp
        };

        ObjectResult result = PackagingTierProblemDetailsFactory.CreatePaymentRequired(
            http,
            TenantTier.Free,
            TenantTier.Standard,
            "/v1/policy-packs");

        result.StatusCode.Should().Be(404);
        Microsoft.AspNetCore.Mvc.ProblemDetails? problem = result.Value as Microsoft.AspNetCore.Mvc.ProblemDetails;
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemTypes.ResourceNotFound);
        problem.Title.Should().Be("Not Found");
        problem.Detail.Should().Be("The requested resource was not found.");
        problem.Extensions.Should().NotContainKey("upgradeUrl");
        problem.Extensions.Should().NotContainKey("currentTier");
        problem.Extensions["supportHint"].Should().NotBeNull();
    }

    [Fact]
    public void CreatePaymentRequired_obfuscates_even_when_public_site_unconfigured()
    {
        ServiceCollection services = [];
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        ServiceProvider sp = services.BuildServiceProvider();
        DefaultHttpContext http = new()
        {
            RequestServices = sp
        };

        ObjectResult result = PackagingTierProblemDetailsFactory.CreatePaymentRequired(
            http,
            TenantTier.Standard,
            TenantTier.Enterprise,
            null);

        result.StatusCode.Should().Be(404);
        Microsoft.AspNetCore.Mvc.ProblemDetails? problem = result.Value as Microsoft.AspNetCore.Mvc.ProblemDetails;
        problem!.Type.Should().Be(ProblemTypes.ResourceNotFound);
        problem.Extensions.Should().NotContainKey("upgradeUrl");
    }
}

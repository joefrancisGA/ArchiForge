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
    [SkippableFact]
    public void CreateObfuscatedNotFound_returns_404_resource_not_found()
    {
        ServiceCollection services = [];
        services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?> { ["ArchLucid:PublicSite:BaseUrl"] = "https://branded.example" })
                .Build());
        ServiceProvider sp = services.BuildServiceProvider();
        DefaultHttpContext http = new() { RequestServices = sp };

        ObjectResult result = PackagingTierProblemDetailsFactory.CreateObfuscatedNotFound(http, "/v1/policy-packs");

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

    [SkippableFact]
    public void CreateObfuscatedNotFound_when_instance_null_still_problem_json()
    {
        ServiceCollection services = [];
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        ServiceProvider sp = services.BuildServiceProvider();
        DefaultHttpContext http = new() { RequestServices = sp };

        ObjectResult result = PackagingTierProblemDetailsFactory.CreateObfuscatedNotFound(http, null);

        result.StatusCode.Should().Be(404);
        Microsoft.AspNetCore.Mvc.ProblemDetails? problem = result.Value as Microsoft.AspNetCore.Mvc.ProblemDetails;
        problem!.Type.Should().Be(ProblemTypes.ResourceNotFound);
    }

    [SkippableFact]
    public void CreateTenantProductInsufficientTier_returns_403_packaging_tier_problem_without_tier_echo()
    {
        ServiceCollection services = [];
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        ServiceProvider sp = services.BuildServiceProvider();
        DefaultHttpContext http = new() { RequestServices = sp };

        ObjectResult result = PackagingTierProblemDetailsFactory.CreateTenantProductInsufficientTier(
            http,
            TenantTier.Standard,
            "/v1/governance/dashboard");

        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        Microsoft.AspNetCore.Mvc.ProblemDetails? problem = result.Value as Microsoft.AspNetCore.Mvc.ProblemDetails;
        problem!.Type.Should().Be(ProblemTypes.PackagingTierInsufficient);
        problem.Extensions.Should().NotContainKey("currentTier");
    }
}

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
    public void CreatePaymentRequired_sets_upgrade_and_pricing_extensions_from_config()
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

        result.StatusCode.Should().Be(402);
        Microsoft.AspNetCore.Mvc.ProblemDetails? problem = result.Value as Microsoft.AspNetCore.Mvc.ProblemDetails;
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemTypes.PackagingTierInsufficient);
        problem.Extensions["upgradeUrl"].Should().Be("https://branded.example/pricing");
        problem.Extensions["pricingUrl"].Should().Be("https://branded.example/pricing");
        problem.Extensions["supportHint"].Should().NotBeNull();
    }

    [Fact]
    public void CreatePaymentRequired_uses_default_public_site_when_unconfigured()
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

        Microsoft.AspNetCore.Mvc.ProblemDetails? problem = result.Value as Microsoft.AspNetCore.Mvc.ProblemDetails;
        problem!.Extensions["upgradeUrl"].Should().Be("https://archlucid.net/pricing");
    }
}

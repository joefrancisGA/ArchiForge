using ArchLucid.Application;
using ArchLucid.Application.Bootstrap;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Core.Scoping;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Api.Tests;

/// <summary>
///     ADR 0030 PR A3 (2026-04-24): proves both <c>Demo:SeedDepth</c> modes (owner Decision B,
///     2026-04-23 — <c>quickstart | vertical</c>) commit through the authority FK chain and produce a
///     <see cref="ArchitectureRunDetail.Manifest" /> with non-empty Services + Datastores + Relationships.
/// </summary>
/// <remarks>
///     <para>
///         The <c>quickstart</c> mode writes the one-of-each minimum (a single Checkout API service, single
///         Orders datastore, and a single relationship from the service into the datastore). The
///         <c>vertical</c> mode writes the production-realistic depth (additional Payment Gateway service
///         + service-to-service and service-to-datastore relationships). Both must surface non-empty
///         collections through <see cref="IRunDetailQueryService" /> — that is the post-condition the
///         downstream operator UI, export, and governance flows depend on.
///     </para>
///     <para>
///         Uses <see cref="ArchLucidApiFactory" /> (in-memory storage + ephemeral SQL catalog) so the test
///         is identical to the surrounding <c>DemoSeedServiceTests</c> in factory shape; the only delta is
///         the <c>Demo:SeedDepth</c> override applied via a per-test factory subclass.
///     </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class DemoSeedDepthIntegrationTests
{
    [Theory]
    [InlineData("quickstart", false, 1, 1, 1)]
    [InlineData("vertical", true, 2, 1, 3)]
    public async Task SeedAsync_writes_committed_manifest_with_nonempty_services_datastores_relationships(
        string seedDepth,
        bool richSeed,
        int expectedServices,
        int expectedDatastores,
        int expectedRelationships)
    {
        await using SeedDepthApiFactory factory = new(seedDepth);
        using IServiceScope scope = factory.Services.CreateScope();

        IScopeContextProvider scopeProvider = scope.ServiceProvider.GetRequiredService<IScopeContextProvider>();
        ContosoRetailDemoIds demo = ContosoRetailDemoIds.ForTenant(scopeProvider.GetCurrentScope().TenantId);

        await scope.ServiceProvider.GetRequiredService<IDemoSeedService>().SeedAsync();

        IRunDetailQueryService detail = scope.ServiceProvider.GetRequiredService<IRunDetailQueryService>();

        ArchitectureRunDetail? baseline = await detail.GetRunDetailAsync(demo.RunBaseline);
        AssertManifestNonEmpty(baseline, expectedServices, expectedDatastores, expectedRelationships, richSeed);

        ArchitectureRunDetail? hardened = await detail.GetRunDetailAsync(demo.RunHardened);
        AssertManifestNonEmpty(hardened, expectedServices, expectedDatastores, expectedRelationships, richSeed);
    }

    private static void AssertManifestNonEmpty(
        ArchitectureRunDetail? detail,
        int expectedServices,
        int expectedDatastores,
        int expectedRelationships,
        bool richSeed)
    {
        detail.Should().NotBeNull("the demo seed must commit through the authority FK chain in both SeedDepth modes");
        detail!.Run.Status.Should().Be(ArchitectureRunStatus.Committed,
            "ADR 0030 PR A3 wires DemoSeedService through the authority commit orchestrator; both modes must end Committed");

        detail.Manifest.Should().NotBeNull(
            "IUnifiedGoldenManifestReader must project the authority manifest back into the contract for both quickstart and vertical seeds");

        detail.Manifest!.Services.Should().HaveCount(expectedServices);
        detail.Manifest.Datastores.Should().HaveCount(expectedDatastores);
        detail.Manifest.Relationships.Should().HaveCount(expectedRelationships,
            "owner Decision B (2026-04-23) requires both seed depths to produce a non-empty Relationships collection — quickstart writes the minimum service-to-datastore edge, vertical adds the cross-service edges");

        detail.Manifest.Services.Should().NotBeEmpty();
        detail.Manifest.Datastores.Should().NotBeEmpty();
        detail.Manifest.Relationships.Should().NotBeEmpty();

        if (richSeed)
            detail.Manifest.Services.Should().Contain(
                s => s.ServiceId.StartsWith("svc-payment-gateway", StringComparison.Ordinal),
                "vertical mode must include the Payment Gateway service that distinguishes it from quickstart");
    }

    /// <summary>
    ///     ADR 0030 PR A3 helper — clones <see cref="ArchLucidApiFactory" /> and pins
    ///     <c>Demo:SeedDepth</c> to the test value so each <c>[InlineData]</c> exercises the matching
    ///     branch of <c>DemoSeedService.IsVerticalDemoSeedDepth</c>.
    /// </summary>
    private sealed class SeedDepthApiFactory(string seedDepth) : ArchLucidApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Demo:Enabled"] = "true", ["Demo:SeedDepth"] = seedDepth
                });
            });
        }
    }
}

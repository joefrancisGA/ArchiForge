using ArchLucid.Application;
using ArchLucid.Application.Bootstrap;
using ArchLucid.Application.Diffs;
using ArchLucid.Application.Governance.Preview;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Governance.Preview;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Queries;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Api.Tests;

/// <summary>Validates trusted-baseline Contoso demo seed against the integration test SQL Server database.</summary>
[Trait("Category", "Integration")]
public sealed class DemoSeedServiceTests
{
    [Fact]
    public async Task SeedAsync_inserts_run_records_listable_via_IAuthorityQueryService()
    {
        await using ArchLucidApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        IScopeContextProvider scopeProvider = scope.ServiceProvider.GetRequiredService<IScopeContextProvider>();
        ContosoRetailDemoIds demo = ContosoRetailDemoIds.ForTenant(scopeProvider.GetCurrentScope().TenantId);

        await scope.ServiceProvider.GetRequiredService<IDemoSeedService>().SeedAsync();

        IAuthorityQueryService authority = scope.ServiceProvider.GetRequiredService<IAuthorityQueryService>();
        ScopeContext ctx = scopeProvider.GetCurrentScope();

        IReadOnlyList<RunSummaryDto> rows =
            await authority.ListRunsByProjectAsync(ctx, "Contoso Retail Platform", 50, CancellationToken.None);

        rows.Should().Contain(r => r.RunId == demo.AuthorityRunBaselineId);
        rows.Should().Contain(r => r.RunId == demo.AuthorityRunHardenedId);
        rows.Should().OnlyContain(r => r.ProjectId == "Contoso Retail Platform");
    }

    [Fact]
    public async Task SeedAsync_twice_does_not_throw_and_remains_idempotent()
    {
        await using ArchLucidApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        IDemoSeedService seed = scope.ServiceProvider.GetRequiredService<IDemoSeedService>();
        await seed.SeedAsync();
        Func<Task> second = async () => await seed.SeedAsync();
        await second.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SeedAsync_creates_baseline_and_hardened_runs_with_manifests()
    {
        await using ArchLucidApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        IScopeContextProvider scopeProvider = scope.ServiceProvider.GetRequiredService<IScopeContextProvider>();
        ContosoRetailDemoIds demo = ContosoRetailDemoIds.ForTenant(scopeProvider.GetCurrentScope().TenantId);
        IDemoSeedService seed = scope.ServiceProvider.GetRequiredService<IDemoSeedService>();
        await seed.SeedAsync();

        IRunDetailQueryService detail = scope.ServiceProvider.GetRequiredService<IRunDetailQueryService>();

        ArchitectureRunDetail? baseline = await detail.GetRunDetailAsync(demo.RunBaseline);
        baseline.Should().NotBeNull();
        baseline.Manifest.Should().NotBeNull();
        baseline.Run.CurrentManifestVersion.Should().Be(demo.ManifestBaseline);
        baseline.Results.Should().NotBeEmpty();

        ArchitectureRunDetail? hardened = await detail.GetRunDetailAsync(demo.RunHardened);
        hardened.Should().NotBeNull();
        hardened.Manifest.Should().NotBeNull();
        hardened.Run.CurrentManifestVersion.Should().Be(demo.ManifestHardened);
    }

    [Fact]
    public async Task SeedAsync_governance_activations_allow_environment_compare_preview()
    {
        await using ArchLucidApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        IDemoSeedService seed = scope.ServiceProvider.GetRequiredService<IDemoSeedService>();
        await seed.SeedAsync();

        IGovernancePreviewService preview = scope.ServiceProvider.GetRequiredService<IGovernancePreviewService>();
        GovernanceEnvironmentComparisonResult result = await preview.CompareEnvironmentsAsync(
            new GovernanceEnvironmentComparisonRequest { SourceEnvironment = "dev", TargetEnvironment = "test" });

        result.Differences.Should().NotBeEmpty("baseline vs hardened governance should differ");
    }

    [Fact]
    public async Task SeedAsync_lists_both_demo_runs_in_run_summaries()
    {
        await using ArchLucidApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        IScopeContextProvider scopeProvider = scope.ServiceProvider.GetRequiredService<IScopeContextProvider>();
        ContosoRetailDemoIds demo = ContosoRetailDemoIds.ForTenant(scopeProvider.GetCurrentScope().TenantId);
        await scope.ServiceProvider.GetRequiredService<IDemoSeedService>().SeedAsync();

        IRunDetailQueryService detail = scope.ServiceProvider.GetRequiredService<IRunDetailQueryService>();
        IReadOnlyList<RunSummary> summaries = await detail.ListRunSummariesAsync();

        summaries.Select(s => s.RunId).Should().Contain(
        [
            demo.RunBaseline,
            demo.RunHardened
        ]);
    }

    [Fact]
    public async Task SeedAsync_manifest_diff_detects_structural_differences_between_versions()
    {
        await using ArchLucidApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        IScopeContextProvider scopeProvider = scope.ServiceProvider.GetRequiredService<IScopeContextProvider>();
        ContosoRetailDemoIds demo = ContosoRetailDemoIds.ForTenant(scopeProvider.GetCurrentScope().TenantId);
        await scope.ServiceProvider.GetRequiredService<IDemoSeedService>().SeedAsync();

        IRunDetailQueryService detail = scope.ServiceProvider.GetRequiredService<IRunDetailQueryService>();
        IManifestDiffService manifestDiff = scope.ServiceProvider.GetRequiredService<IManifestDiffService>();

        ArchitectureRunDetail? baseline = await detail.GetRunDetailAsync(demo.RunBaseline);
        ArchitectureRunDetail? hardened = await detail.GetRunDetailAsync(demo.RunHardened);

        ManifestDiffResult diff = manifestDiff.Compare(baseline!.Manifest!, hardened!.Manifest!);

        bool hasMeaningfulStructuralDiff =
            diff.AddedServices.Count > 0
            || diff.RemovedServices.Count > 0
            || diff.AddedDatastores.Count > 0
            || diff.RemovedDatastores.Count > 0
            || diff.AddedRequiredControls.Count > 0
            || diff.RemovedRequiredControls.Count > 0;

        hasMeaningfulStructuralDiff.Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_agent_result_compare_produces_deltas()
    {
        await using ArchLucidApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        IScopeContextProvider scopeProvider = scope.ServiceProvider.GetRequiredService<IScopeContextProvider>();
        ContosoRetailDemoIds demo = ContosoRetailDemoIds.ForTenant(scopeProvider.GetCurrentScope().TenantId);
        await scope.ServiceProvider.GetRequiredService<IDemoSeedService>().SeedAsync();

        IRunDetailQueryService detail = scope.ServiceProvider.GetRequiredService<IRunDetailQueryService>();
        IAgentResultDiffService agentDiff = scope.ServiceProvider.GetRequiredService<IAgentResultDiffService>();

        ArchitectureRunDetail? baseline = await detail.GetRunDetailAsync(demo.RunBaseline);
        ArchitectureRunDetail? hardened = await detail.GetRunDetailAsync(demo.RunHardened);

        AgentResultDiffResult diff = agentDiff.Compare(
            demo.RunBaseline,
            baseline!.Results,
            demo.RunHardened,
            hardened!.Results);

        diff.AgentDeltas.Should().NotBeEmpty();
    }
}

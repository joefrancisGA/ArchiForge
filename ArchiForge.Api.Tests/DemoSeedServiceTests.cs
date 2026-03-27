using ArchiForge.Application;
using ArchiForge.Application.Bootstrap;
using ArchiForge.Application.Diffs;
using ArchiForge.Application.Governance.Preview;
using ArchiForge.Contracts.Architecture;
using ArchiForge.Contracts.Governance.Preview;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace ArchiForge.Api.Tests;

/// <summary>Validates trusted-baseline Contoso demo seed against the shared SQLite test database.</summary>
[Trait("Category", "Integration")]
public sealed class DemoSeedServiceTests
{
    [Fact]
    public async Task SeedAsync_twice_does_not_throw_and_remains_idempotent()
    {
        await using ArchiForgeApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        IDemoSeedService seed = scope.ServiceProvider.GetRequiredService<IDemoSeedService>();
        await seed.SeedAsync();
        Func<Task> second = async () => await seed.SeedAsync();
        await second.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SeedAsync_creates_baseline_and_hardened_runs_with_manifests()
    {
        await using ArchiForgeApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        IDemoSeedService seed = scope.ServiceProvider.GetRequiredService<IDemoSeedService>();
        await seed.SeedAsync();

        IRunDetailQueryService detail = scope.ServiceProvider.GetRequiredService<IRunDetailQueryService>();

        ArchitectureRunDetail? baseline = await detail.GetRunDetailAsync(ContosoRetailDemoIdentifiers.RunBaseline);
        baseline.Should().NotBeNull();
        baseline.Manifest.Should().NotBeNull();
        baseline.Run.CurrentManifestVersion.Should().Be(ContosoRetailDemoIdentifiers.ManifestBaseline);
        baseline.Results.Should().NotBeEmpty();

        ArchitectureRunDetail? hardened = await detail.GetRunDetailAsync(ContosoRetailDemoIdentifiers.RunHardened);
        hardened.Should().NotBeNull();
        hardened.Manifest.Should().NotBeNull();
        hardened.Run.CurrentManifestVersion.Should().Be(ContosoRetailDemoIdentifiers.ManifestHardened);
    }

    [Fact]
    public async Task SeedAsync_governance_activations_allow_environment_compare_preview()
    {
        await using ArchiForgeApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        IDemoSeedService seed = scope.ServiceProvider.GetRequiredService<IDemoSeedService>();
        await seed.SeedAsync();

        IGovernancePreviewService preview = scope.ServiceProvider.GetRequiredService<IGovernancePreviewService>();
        GovernanceEnvironmentComparisonResult result = await preview.CompareEnvironmentsAsync(
            new GovernanceEnvironmentComparisonRequest
            {
                SourceEnvironment = "dev",
                TargetEnvironment = "test"
            });

        result.Differences.Should().NotBeEmpty("baseline vs hardened governance should differ");
    }

    [Fact]
    public async Task SeedAsync_lists_both_demo_runs_in_run_summaries()
    {
        await using ArchiForgeApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IDemoSeedService>().SeedAsync();

        IRunDetailQueryService detail = scope.ServiceProvider.GetRequiredService<IRunDetailQueryService>();
        IReadOnlyList<RunSummary> summaries = await detail.ListRunSummariesAsync();

        summaries.Select(s => s.RunId).Should().Contain(
            [
                ContosoRetailDemoIdentifiers.RunBaseline,
                ContosoRetailDemoIdentifiers.RunHardened
            ]);
    }

    [Fact]
    public async Task SeedAsync_manifest_diff_detects_structural_differences_between_versions()
    {
        await using ArchiForgeApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IDemoSeedService>().SeedAsync();

        IRunDetailQueryService detail = scope.ServiceProvider.GetRequiredService<IRunDetailQueryService>();
        IManifestDiffService manifestDiff = scope.ServiceProvider.GetRequiredService<IManifestDiffService>();

        ArchitectureRunDetail? baseline = await detail.GetRunDetailAsync(ContosoRetailDemoIdentifiers.RunBaseline);
        ArchitectureRunDetail? hardened = await detail.GetRunDetailAsync(ContosoRetailDemoIdentifiers.RunHardened);

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
        await using ArchiForgeApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IDemoSeedService>().SeedAsync();

        IRunDetailQueryService detail = scope.ServiceProvider.GetRequiredService<IRunDetailQueryService>();
        IAgentResultDiffService agentDiff = scope.ServiceProvider.GetRequiredService<IAgentResultDiffService>();

        ArchitectureRunDetail? baseline = await detail.GetRunDetailAsync(ContosoRetailDemoIdentifiers.RunBaseline);
        ArchitectureRunDetail? hardened = await detail.GetRunDetailAsync(ContosoRetailDemoIdentifiers.RunHardened);

        AgentResultDiffResult diff = agentDiff.Compare(
            ContosoRetailDemoIdentifiers.RunBaseline,
            baseline!.Results,
            ContosoRetailDemoIdentifiers.RunHardened,
            hardened!.Results);

        diff.AgentDeltas.Should().NotBeEmpty();
    }
}

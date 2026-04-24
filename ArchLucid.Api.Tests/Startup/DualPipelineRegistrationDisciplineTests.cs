using System.Reflection;

using ArchLucid.Application.Runs;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Decisioning.Interfaces;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Api.Tests.Startup;

/// <summary>
///     ADR 0030 PR A3 (2026-04-24) closure invariant — the original ADR 0010 dual-pipeline boundary
///     has fully collapsed onto the authority side. <c>ICoordinatorGoldenManifestRepository</c> and
///     <c>ICoordinatorDecisionTraceRepository</c> were deleted, the legacy
///     <c>ArchitectureRunCommitOrchestrator</c> + <c>RunCommitPathSelector</c> +
///     <c>LegacyRunCommitPathOptions</c> were deleted, and <c>dbo.GoldenManifestVersions</c> was
///     dropped in PR A4 (migration 111). This test now pins the opposite invariant: the legacy
///     coordinator types are gone from the production graph and the only surviving manifest /
///     decision-trace repositories live in the authority namespaces (<c>ArchLucid.Decisioning</c> or
///     <c>ArchLucid.Persistence</c>).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class DualPipelineRegistrationDisciplineTests(OpenApiContractWebAppFactory factory)
    : IClassFixture<OpenApiContractWebAppFactory>
{
    [Fact]
    public void AuthorityGoldenManifestRepository_resolves_to_Decisioning_or_Persistence_concrete()
    {
        IGoldenManifestRepository instance = factory.Services.GetRequiredService<IGoldenManifestRepository>();

        instance.Should().NotBeNull();

        Type concrete = instance.GetType();

        bool inExpectedNamespace =
            (concrete.Namespace ?? string.Empty).StartsWith("ArchLucid.Decisioning", StringComparison.Ordinal) ||
            (concrete.Namespace ?? string.Empty).StartsWith("ArchLucid.Persistence", StringComparison.Ordinal);

        inExpectedNamespace.Should().BeTrue(
            $"authority IGoldenManifestRepository must resolve from ArchLucid.Decisioning or ArchLucid.Persistence; got {concrete.FullName}");
    }

    [Fact]
    public void AuthorityDecisionTraceRepository_resolves_to_Decisioning_or_Persistence_concrete()
    {
        IDecisionTraceRepository instance = factory.Services.GetRequiredService<IDecisionTraceRepository>();

        instance.Should().NotBeNull();

        Type concrete = instance.GetType();
        bool inExpectedNamespace =
            (concrete.Namespace ?? string.Empty).StartsWith("ArchLucid.Decisioning", StringComparison.Ordinal) ||
            (concrete.Namespace ?? string.Empty).StartsWith("ArchLucid.Persistence", StringComparison.Ordinal);

        inExpectedNamespace.Should().BeTrue(
            $"authority IDecisionTraceRepository must resolve from ArchLucid.Decisioning or ArchLucid.Persistence; got {concrete.FullName}");
    }

    [Fact]
    public void IRunCommitOrchestrator_resolves_to_RunCommitOrchestratorFacade()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        IRunCommitOrchestrator facade = scope.ServiceProvider.GetRequiredService<IRunCommitOrchestrator>();

        facade.Should().BeOfType<RunCommitOrchestratorFacade>();
    }

    [Fact]
    public void IArchitectureRunCommitOrchestrator_resolves_to_AuthorityDriven_concrete()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        IArchitectureRunCommitOrchestrator commit =
            scope.ServiceProvider.GetRequiredService<IArchitectureRunCommitOrchestrator>();

        commit.Should().BeOfType<AuthorityDrivenArchitectureRunCommitOrchestrator>(
            "ADR 0030 PR A3 retired RunCommitPathSelector + ArchitectureRunCommitOrchestrator; AuthorityDrivenArchitectureRunCommitOrchestrator is the single implementation");
    }

    [Fact]
    public void UnifiedGoldenManifestReader_resolves_from_Persistence_namespace()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        IUnifiedGoldenManifestReader
            instance = scope.ServiceProvider.GetRequiredService<IUnifiedGoldenManifestReader>();

        instance.Should().NotBeNull();
        (instance.GetType().Namespace ?? string.Empty).Should().StartWith("ArchLucid.Persistence",
            "the read façade still lives in the persistence layer next to the authority repositories");
    }

    /// <summary>
    ///     ADR 0030 PR A3 closure: <c>ICoordinatorGoldenManifestRepository</c> and
    ///     <c>ICoordinatorDecisionTraceRepository</c> were deleted alongside the legacy commit orchestrator.
    ///     No production assembly may reintroduce a type with those simple names under any persistence
    ///     namespace.
    /// </summary>
    [Fact]
    public void Production_assemblies_do_not_define_legacy_coordinator_repository_interfaces()
    {
        IEnumerable<Assembly> productionAssemblies = ProductionAssembliesReachableFromApi();

        List<string> resurrected = [];

        foreach (Assembly assembly in productionAssemblies)
        {
            foreach (Type type in SafeGetTypes(assembly))
            {
                if (type is not { IsInterface: true })
                    continue;

                if (type.Name is "ICoordinatorGoldenManifestRepository" or "ICoordinatorDecisionTraceRepository")
                    resurrected.Add(type.FullName ?? type.Name);
            }
        }

        resurrected.Should().BeEmpty(
            "ADR 0030 PR A3 deleted ICoordinatorGoldenManifestRepository and ICoordinatorDecisionTraceRepository together with the legacy commit orchestrator and dbo.GoldenManifestVersions (PR A4); reintroducing either interface would resurrect the dual-pipeline shape ADR 0030 retired");
    }

    /// <summary>
    ///     The ADR 0010 collision risk (a separate <c>ArchLucid.Persistence.Data.Repositories.IGoldenManifestRepository</c>
    ///     shadowing the authority interface) was eliminated by the 2026-04-05 rename. Pin that the data-layer
    ///     namespace does not re-introduce the unprefixed names.
    /// </summary>
    [Fact]
    public void DataLayer_namespace_does_not_redefine_unprefixed_interface_names_anymore()
    {
        Assembly persistenceAssembly = typeof(IUnifiedGoldenManifestReader).Assembly;

        IEnumerable<Type> dataLayerCollisions = persistenceAssembly
            .GetTypes()
            .Where(type => type is { IsInterface: true, IsPublic: true })
            .Where(type =>
                string.Equals(type.Namespace, "ArchLucid.Persistence.Data.Repositories", StringComparison.Ordinal))
            .Where(type => type.Name is "IGoldenManifestRepository" or "IDecisionTraceRepository");

        dataLayerCollisions.Should().BeEmpty(
            "the 2026-04-05 rename removed unprefixed IGoldenManifestRepository / IDecisionTraceRepository from ArchLucid.Persistence.Data.Repositories; reintroducing them would resurrect the ADR 0010 collision risk and require a new ADR");
    }

    /// <summary>
    ///     Returns every <c>ArchLucid.*</c> assembly currently loaded into the test AppDomain that is part of
    ///     the production graph (excludes <c>*.Tests</c> assemblies).
    /// </summary>
    private static IEnumerable<Assembly> ProductionAssembliesReachableFromApi()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name is { } name
                        && name.StartsWith("ArchLucid.", StringComparison.Ordinal)
                        && !name.EndsWith(".Tests", StringComparison.Ordinal));
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}

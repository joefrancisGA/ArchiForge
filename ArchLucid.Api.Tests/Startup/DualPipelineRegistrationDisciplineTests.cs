using System.Reflection;

using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using DataIDecisionTraceRepository = ArchLucid.Persistence.Data.Repositories.ICoordinatorDecisionTraceRepository;
using DataIGoldenManifestRepository = ArchLucid.Persistence.Data.Repositories.ICoordinatorGoldenManifestRepository;
using DecisioningIDecisionTraceRepository = ArchLucid.Decisioning.Interfaces.IDecisionTraceRepository;
using DecisioningIGoldenManifestRepository = ArchLucid.Decisioning.Interfaces.IGoldenManifestRepository;

namespace ArchLucid.Api.Tests.Startup;

/// <summary>
/// Pins the boundary that <see href="../../docs/adr/0010-dual-manifest-trace-repository-contracts.md">ADR 0010</see>
/// originally guarded with a "fully qualified at registration time" rule.
/// </summary>
/// <remarks>
/// <para>
/// In its original form ADR 0010 worried that the duplicate-named manifest /
/// trace interfaces could cross-wire at DI registration. The 2026-04-05 rename
/// (rename-checklist row "Improvement 5") eliminated the literal collision by
/// renaming the coordinator side to <see cref="DataIGoldenManifestRepository"/>
/// and <see cref="DataIDecisionTraceRepository"/>. This test pins both halves
/// of that invariant against regression: (a) the interface names stay split
/// across the two namespaces, and (b) the coordinator and authority concrete
/// implementations stay distinct objects in the container.
/// </para>
/// <para>
/// Marked <c>Suite=Core</c> so it runs in the fast Core tier alongside the
/// audit-event-collision tests; the test only inspects the service collection,
/// it does not start an HTTP server.
/// </para>
/// </remarks>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class DualPipelineRegistrationDisciplineTests(OpenApiContractWebAppFactory factory)
    : IClassFixture<OpenApiContractWebAppFactory>
{
    [Fact]
    public void CoordinatorGoldenManifestRepository_resolves_to_DataLayer_concrete()
    {
        DataIGoldenManifestRepository instance = factory.Services.GetRequiredService<DataIGoldenManifestRepository>();

        instance.Should().NotBeNull();

        Type concrete = instance.GetType();
        AssertNamespaceStartsWith(concrete, "ArchLucid.Persistence");
    }

    [Fact]
    public void AuthorityGoldenManifestRepository_resolves_to_Decisioning_or_Persistence_concrete()
    {
        DecisioningIGoldenManifestRepository instance = factory.Services.GetRequiredService<DecisioningIGoldenManifestRepository>();

        instance.Should().NotBeNull();

        Type concrete = instance.GetType();

        // Acceptable concrete homes: ArchLucid.Decisioning (in-memory) or
        // ArchLucid.Persistence (Sql / caching wrappers). Any other namespace
        // would be a regression.
        bool inExpectedNamespace =
            (concrete.Namespace ?? string.Empty).StartsWith("ArchLucid.Decisioning", StringComparison.Ordinal) ||
            (concrete.Namespace ?? string.Empty).StartsWith("ArchLucid.Persistence", StringComparison.Ordinal);

        inExpectedNamespace.Should().BeTrue(
            because: $"authority IGoldenManifestRepository must resolve from ArchLucid.Decisioning or ArchLucid.Persistence; got {concrete.FullName}");
    }

    [Fact]
    public void CoordinatorDecisionTraceRepository_resolves_to_DataLayer_concrete()
    {
        DataIDecisionTraceRepository instance = factory.Services.GetRequiredService<DataIDecisionTraceRepository>();

        instance.Should().NotBeNull();

        AssertNamespaceStartsWith(instance.GetType(), "ArchLucid.Persistence");
    }

    [Fact]
    public void AuthorityDecisionTraceRepository_resolves_to_Decisioning_or_Persistence_concrete()
    {
        DecisioningIDecisionTraceRepository instance = factory.Services.GetRequiredService<DecisioningIDecisionTraceRepository>();

        instance.Should().NotBeNull();

        Type concrete = instance.GetType();
        bool inExpectedNamespace =
            (concrete.Namespace ?? string.Empty).StartsWith("ArchLucid.Decisioning", StringComparison.Ordinal) ||
            (concrete.Namespace ?? string.Empty).StartsWith("ArchLucid.Persistence", StringComparison.Ordinal);

        inExpectedNamespace.Should().BeTrue(
            because: $"authority IDecisionTraceRepository must resolve from ArchLucid.Decisioning or ArchLucid.Persistence; got {concrete.FullName}");
    }

    [Fact]
    public void Coordinator_and_authority_GoldenManifest_concretes_are_distinct_types()
    {
        Type coordinatorConcrete = factory.Services.GetRequiredService<DataIGoldenManifestRepository>().GetType();
        Type authorityConcrete = factory.Services.GetRequiredService<DecisioningIGoldenManifestRepository>().GetType();

        coordinatorConcrete.Should().NotBe(authorityConcrete,
            because: "ADR 0010 requires the two manifest repository families to be implemented by distinct concrete types so a single misregistration cannot collapse both pipelines onto one persistence path");
    }

    [Fact]
    public void Coordinator_and_authority_DecisionTrace_concretes_are_distinct_types()
    {
        Type coordinatorConcrete = factory.Services.GetRequiredService<DataIDecisionTraceRepository>().GetType();
        Type authorityConcrete = factory.Services.GetRequiredService<DecisioningIDecisionTraceRepository>().GetType();

        coordinatorConcrete.Should().NotBe(authorityConcrete,
            because: "ADR 0010 requires the two trace repository families to be implemented by distinct concrete types so a single misregistration cannot collapse both pipelines onto one persistence path");
    }

    [Fact]
    public void UnifiedGoldenManifestReader_resolves_from_Persistence_namespace()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        IUnifiedGoldenManifestReader instance = scope.ServiceProvider.GetRequiredService<IUnifiedGoldenManifestReader>();

        instance.Should().NotBeNull();
        (instance.GetType().Namespace ?? string.Empty).Should().StartWith("ArchLucid.Persistence",
            because: "ADR 0021 Phase 1 keeps the read façade in the persistence layer next to the coordinator/authority repositories");
    }

    [Fact]
    public void DataLayer_namespace_does_not_redefine_unprefixed_interface_names_anymore()
    {
        // The original ADR 0010 collision risk was that
        // ArchLucid.Persistence.Data.Repositories defined IGoldenManifestRepository
        // and IDecisionTraceRepository under the same simple name as the
        // Decisioning interfaces. The 2026-04-05 rename eliminated that by adding
        // the "Coordinator" prefix on the data-layer side. Pin that invariant.
        Assembly persistenceAssembly = typeof(DataIGoldenManifestRepository).Assembly;

        IEnumerable<Type> dataLayerCollisions = persistenceAssembly
            .GetTypes()
            .Where(type => type is { IsInterface: true, IsPublic: true })
            .Where(type => string.Equals(type.Namespace, "ArchLucid.Persistence.Data.Repositories", StringComparison.Ordinal))
            .Where(type => type.Name is "IGoldenManifestRepository" or "IDecisionTraceRepository");

        dataLayerCollisions.Should().BeEmpty(
            because: "the 2026-04-05 rename removed unprefixed IGoldenManifestRepository / IDecisionTraceRepository from ArchLucid.Persistence.Data.Repositories; reintroducing them would resurrect the ADR 0010 collision risk and require a new ADR");
    }

    private static void AssertNamespaceStartsWith(Type type, string expectedPrefix)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));

        (type.Namespace ?? string.Empty)
            .StartsWith(expectedPrefix, StringComparison.Ordinal)
            .Should().BeTrue(because: $"expected concrete type {type.FullName} to live in {expectedPrefix}.* per ADR 0010 boundaries");
    }
}

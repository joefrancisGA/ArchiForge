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

    /// <summary>
    /// ADR 0021 Phase 1 **retirement gate**: across <em>every</em> production assembly the API binary
    /// transitively pulls in, the only types that may type-reference
    /// <see cref="DataIGoldenManifestRepository"/> on a constructor parameter, field, or property are:
    /// <list type="bullet">
    ///   <item><description>The unified read façade (<c>UnifiedGoldenManifestReader</c>) — the single allowed read consumer.</description></item>
    ///   <item><description>The three documented write-path orchestrators that ADR 0021 §Phase 3 will retire when the write-side façade lands. These are explicitly allow-listed by full type name so a code reviewer must update this test (and ADR 0021) before introducing a fourth.</description></item>
    ///   <item><description>The interface itself, its concrete implementations, and the DI extension methods that wire them — all of which live in <c>ArchLucid.Persistence</c> or <c>ArchLucid.Host.Composition</c> and are exempted by namespace.</description></item>
    /// </list>
    /// Anything else is a regression — a new internal manifest reader has slipped past the unified
    /// reader and must be migrated to <see cref="IUnifiedGoldenManifestReader"/> instead.
    /// </summary>
    [Fact]
    public void Production_types_outside_allow_list_do_not_reference_ICoordinatorGoldenManifestRepository()
    {
        Type coordinatorRepo = typeof(DataIGoldenManifestRepository);

        // Allow-list of full type names. Each entry must justify its presence in the comment above.
        // When ADR 0021 Phase 3 lands and the write-side façade replaces these orchestrators, this
        // set shrinks to a single entry: ArchLucid.Persistence.Reads.UnifiedGoldenManifestReader.
        HashSet<string> allowList =
        [
            // The single permitted read consumer — the whole point of Phase 1.
            "ArchLucid.Persistence.Reads.UnifiedGoldenManifestReader",
            // Write-path orchestrators retained until ADR 0021 Phase 3 retirement of the interface.
            "ArchLucid.Application.Runs.Orchestration.ArchitectureRunCommitOrchestrator",
            "ArchLucid.Application.ReplayRunService",
            "ArchLucid.Application.Bootstrap.DemoSeedService",
        ];

        // Namespaces that are part of the persistence / DI layer the interface lives in. The interface
        // itself, the InMemory + Dapper implementations, and the unified reader's host registration
        // all sit here — exempting them by namespace keeps the test focused on *consumers* outside
        // the persistence boundary.
        string[] exemptNamespacePrefixes =
        [
            "ArchLucid.Persistence.Data.Repositories",
            "ArchLucid.Host.Composition.Startup",
        ];

        IEnumerable<Assembly> productionAssemblies = ProductionAssembliesReachableFromApi();

        List<string> violations = [];

        foreach (Assembly assembly in productionAssemblies)
        {
            foreach (Type type in SafeGetTypes(assembly))
            {
                string ns = type.Namespace ?? string.Empty;

                if (exemptNamespacePrefixes.Any(prefix => ns.StartsWith(prefix, StringComparison.Ordinal)))
                    continue;

                string fullName = type.FullName ?? type.Name;
                if (allowList.Contains(fullName))
                    continue;


                if (TypeReferencesMember(type, coordinatorRepo, out string? memberDescription))
                    violations.Add($"{fullName} → {memberDescription}");
            }
        }

        violations.Should().BeEmpty(
            because: "ADR 0021 Phase 1 retirement gate — only the unified reader (and the three documented write-path orchestrators retained for Phase 3) may type-reference ICoordinatorGoldenManifestRepository; new readers must use IUnifiedGoldenManifestReader.");
    }

    /// <summary>
    /// Returns every <c>ArchLucid.*</c> assembly currently loaded into the test AppDomain that is part of
    /// the production graph (excludes <c>*.Tests</c> assemblies). The OpenApiContractWebAppFactory has
    /// already booted the API host by the time this test runs, so every assembly the API transitively
    /// references is loaded — that's the right scope for "production class" enforcement.
    /// </summary>
    private static IEnumerable<Assembly> ProductionAssembliesReachableFromApi()
        => AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name is { } name
                        && name.StartsWith("ArchLucid.", StringComparison.Ordinal)
                        && !name.EndsWith(".Tests", StringComparison.Ordinal));

    /// <summary>
    /// Defensive <see cref="Assembly.GetTypes"/> wrapper — some dynamic / reflection-only types may
    /// fail to load and we don't want a single bad type to mask the whole sweep.
    /// </summary>
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

    /// <summary>
    /// True when <paramref name="candidate"/> declares a public/non-public constructor parameter, field,
    /// or property whose type is exactly <paramref name="target"/>. Returns the offending member kind +
    /// name in <paramref name="memberDescription"/> so failures point reviewers at the offending site.
    /// </summary>
    private static bool TypeReferencesMember(Type candidate, Type target, out string? memberDescription)
    {
        memberDescription = null;

        const BindingFlags allInstanceMembers =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        foreach (ConstructorInfo ctor in candidate.GetConstructors(allInstanceMembers))
        {
            ParameterInfo? offending = ctor.GetParameters().FirstOrDefault(p => p.ParameterType == target);
            if (offending is null) continue;

            memberDescription = $"ctor parameter '{offending.Name}'";
            return true;
        }

        foreach (FieldInfo field in candidate.GetFields(allInstanceMembers))
        {
            if (field.FieldType != target) continue;

            memberDescription = $"field '{field.Name}'";
            return true;
        }

        foreach (PropertyInfo property in candidate.GetProperties(allInstanceMembers))
        {
            if (property.PropertyType != target) continue;

            memberDescription = $"property '{property.Name}'";
            return true;
        }

        return false;
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

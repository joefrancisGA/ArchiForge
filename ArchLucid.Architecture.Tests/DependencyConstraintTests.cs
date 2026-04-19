using System.Reflection;
using System.Text.RegularExpressions;

using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.ArtifactSynthesis.Services;
using ArchLucid.Cli;
using ArchLucid.ContextIngestion;
using ArchLucid.Contracts.Abstractions.Evolution;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Integration;
using ArchLucid.Decisioning.Alerts;
using ArchLucid.KnowledgeGraph;
using ArchLucid.Persistence;
using ArchLucid.Persistence.Coordination.Replay;

using FluentAssertions;

using NetArchTest.Rules;

namespace ArchLucid.Architecture.Tests;

/// <summary>NetArchTest + assembly-reference checks for layer boundaries. One <see cref="FactAttribute"/> per rule for clear CI output.</summary>
public sealed class DependencyConstraintTests
{
    // ── Tier 1 — Foundation isolation ─────────────────────────────────────────

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void Core_must_not_depend_on_any_solution_project()
    {
        Assembly core = typeof(IntegrationEventTypes).Assembly;

        TestResult result = Types
            .InAssembly(core)
            .ShouldNot()
            .HaveDependencyOnAny(ArchitectureConstraintNamespaces.ForbiddenFromCore)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "ArchLucid.Core is the foundation leaf; referencing other ArchLucid assemblies couples infrastructure and domain into the kernel. Offending types: {0}",
            FormatFailingTypeNames(result));
    }

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void Contracts_must_not_depend_on_any_solution_project()
    {
        Assembly contracts = typeof(ArchitectureRun).Assembly;

        TestResult result = Types
            .InAssembly(contracts)
            .ShouldNot()
            .HaveDependencyOnAny(ArchitectureConstraintNamespaces.ForbiddenFromContracts)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "ArchLucid.Contracts is a shared DTO leaf; it must not reference application, persistence, or hosts. Offending types: {0}",
            FormatFailingTypeNames(result));
    }

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void ContractsAbstractions_may_only_depend_on_Contracts()
    {
        Assembly abstractions = typeof(ISimulationEngine).Assembly;

        TestResult result = Types
            .InAssembly(abstractions)
            .ShouldNot()
            .HaveDependencyOnAny(ArchitectureConstraintNamespaces.ForbiddenFromContractsAbstractions)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "ArchLucid.Contracts.Abstractions defines cross-cutting service ports and may reference ArchLucid.Contracts only. Offending types: {0}",
            FormatFailingTypeNames(result));
    }

    // ── Tier 2 — Persistence sub-module boundaries (assembly refs; shared RootNamespace) ──

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void Coordination_must_not_reference_Runtime()
    {
        Assembly coordination = typeof(ReplayRequest).Assembly;
        AssemblyName[] references = coordination.GetReferencedAssemblies();

        references.Should().NotContain(
            a => a.Name == "ArchLucid.Persistence.Runtime",
            because: "Persistence.Coordination is a lower-level module; referencing Runtime invites circular orchestration coupling.");
    }

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void Coordination_must_not_reference_Advisory()
    {
        Assembly coordination = typeof(ReplayRequest).Assembly;
        AssemblyName[] references = coordination.GetReferencedAssemblies();

        references.Should().NotContain(
            a => a.Name == "ArchLucid.Persistence.Advisory",
            because: "Coordination must stay independent of advisory persistence to avoid upward feature coupling.");
    }

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void Coordination_must_not_reference_Alerts()
    {
        Assembly coordination = typeof(ReplayRequest).Assembly;
        AssemblyName[] references = coordination.GetReferencedAssemblies();

        references.Should().NotContain(
            a => a.Name == "ArchLucid.Persistence.Alerts",
            because: "Coordination must not depend on alert persistence; keep cross-cutting alerts at higher layers.");
    }

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void Integration_must_not_reference_Runtime()
    {
        Assembly integration = typeof(IIntegrationEventOutboxRepository).Assembly;
        AssemblyName[] references = integration.GetReferencedAssemblies();

        references.Should().NotContain(
            a => a.Name == "ArchLucid.Persistence.Runtime",
            because: "Persistence.Integration (outbox, integration events) must not depend on Runtime orchestration to prevent cycles.");
    }

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void Integration_must_not_reference_Advisory()
    {
        Assembly integration = typeof(IIntegrationEventOutboxRepository).Assembly;
        AssemblyName[] references = integration.GetReferencedAssemblies();

        references.Should().NotContain(
            a => a.Name == "ArchLucid.Persistence.Advisory",
            because: "Integration outbox must remain independent of advisory scans.");
    }

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void Integration_must_not_reference_Alerts()
    {
        Assembly integration = typeof(IIntegrationEventOutboxRepository).Assembly;
        AssemblyName[] references = integration.GetReferencedAssemblies();

        references.Should().NotContain(
            a => a.Name == "ArchLucid.Persistence.Alerts",
            because: "Integration outbox must not depend on alert persistence.");
    }

    // ── Tier 3 — Domain hexagonal boundary ───────────────────────────────────

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void Decisioning_must_not_depend_on_Persistence()
    {
        Assembly decisioning = typeof(AlertRecord).Assembly;

        TestResult result = Types
            .InAssembly(decisioning)
            .ShouldNot()
            .HaveDependencyOn("ArchLucid.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Decisioning is domain logic; any ArchLucid.Persistence dependency (base or sub-module) breaks hexagonal isolation. Offending types: {0}",
            FormatFailingTypeNames(result));
    }

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void KnowledgeGraph_must_not_depend_on_Persistence()
    {
        Assembly knowledgeGraph = typeof(GraphNodeTypes).Assembly;

        TestResult result = Types
            .InAssembly(knowledgeGraph)
            .ShouldNot()
            .HaveDependencyOn("ArchLucid.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "KnowledgeGraph stays in the domain/application seam without SQL/Dapper types. Offending types: {0}",
            FormatFailingTypeNames(result));
    }

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void ContextIngestion_must_not_depend_on_Persistence()
    {
        Assembly contextIngestion = typeof(SupportedContextDocumentContentTypes).Assembly;

        TestResult result = Types
            .InAssembly(contextIngestion)
            .ShouldNot()
            .HaveDependencyOn("ArchLucid.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Context ingestion models documents and must not reference persistence implementations. Offending types: {0}",
            FormatFailingTypeNames(result));
    }

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void ArtifactSynthesis_must_not_depend_on_Persistence()
    {
        Assembly synthesis = typeof(ArtifactSynthesisService).Assembly;

        TestResult result = Types
            .InAssembly(synthesis)
            .ShouldNot()
            .HaveDependencyOn("ArchLucid.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Artifact synthesis generates outputs from domain inputs and must not touch persistence. Offending types: {0}",
            FormatFailingTypeNames(result));
    }

    // ── Tier 4 — CLI isolation ────────────────────────────────────────────────

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void Cli_must_not_depend_on_Persistence()
    {
        Assembly cli = typeof(ManifestValidator).Assembly;

        TestResult result = Types
            .InAssembly(cli)
            .ShouldNot()
            .HaveDependencyOn("ArchLucid.Persistence")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "The CLI is a thin host over HTTP clients and contracts; it must not embed persistence. Offending types: {0}",
            FormatFailingTypeNames(result));
    }

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void Cli_must_not_reference_Api_assembly()
    {
        // NetArchTest HaveDependencyOn("ArchLucid.Api") also matches ArchLucid.Api.Client.* — enforce the host assembly boundary via metadata.
        Assembly cli = typeof(ManifestValidator).Assembly;
        AssemblyName[] references = cli.GetReferencedAssemblies();

        references.Should().NotContain(
            a => a.Name == "ArchLucid.Api",
            because: "The CLI must not reference the ASP.NET host assembly; HTTP types come from ArchLucid.Api.Client only.");
    }

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void Product_code_must_not_call_IIntegrationEventPublisher_PublishAsync_outside_authorized_wrappers()
    {
        string? root = FindRepositoryRootContainingSolution();

        root.Should().NotBeNull(because: "ArchLucid.sln must be discoverable from the test output directory.");

        Regex directPublish = new(@"\.PublishAsync\(", RegexOptions.Compiled);
        List<string> violations = new();

        foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (IsExcludedSourceScanPath(path))
            {
                continue;
            }

            if (IsAuthorizedDirectIntegrationPublishFile(path))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(path);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.TrimStart();

                if (trimmed.StartsWith("//", StringComparison.Ordinal)
                    || trimmed.StartsWith("///", StringComparison.Ordinal)
                    || trimmed.StartsWith('*'))
                {
                    continue;
                }

                if (!directPublish.IsMatch(line))
                {
                    continue;
                }

                violations.Add($"{path}:{i + 1}: {line.Trim()}");
            }
        }

        violations.Should().BeEmpty(
            "integration events must go through OutboxAwareIntegrationEventPublishing.TryPublishOrEnqueueAsync; " +
            "only IntegrationEventPublishing (TryPublishAsync) and IntegrationEventOutboxProcessor may call IIntegrationEventPublisher.PublishAsync directly. Violations:{0}{1}",
            Environment.NewLine,
            violations.Count == 0 ? "(none)" : string.Join(Environment.NewLine, violations));
    }

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void Application_references_Core_for_consolidated_audit_event_type_catalog()
    {
        Assembly application = typeof(ArchitectureRunCreateOrchestrator).Assembly;
        AssemblyName[] references = application.GetReferencedAssemblies();

        references.Should().Contain(
            a => a.Name == "ArchLucid.Core",
            because: "Application orchestrators use ArchLucid.Core.Audit.AuditEventTypes.Baseline for trusted-baseline mutation strings (single catalog with durable AuditEventTypes).");
    }

    private static string? FindRepositoryRootContainingSolution()
    {
        string? dir = Path.GetDirectoryName(typeof(DependencyConstraintTests).Assembly.Location);

        for (int i = 0; i < 24 && dir is not null; i++)
        {
            if (File.Exists(Path.Combine(dir, "ArchLucid.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        return null;
    }

    private static bool IsExcludedSourceScanPath(string fullPath)
    {
        string n = fullPath.Replace('\\', '/');

        return n.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || n.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || n.Contains("/.git/", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Tests/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAuthorizedDirectIntegrationPublishFile(string fullPath)
    {
        string file = Path.GetFileName(fullPath);

        return file.Equals("IntegrationEventPublishing.cs", StringComparison.OrdinalIgnoreCase)
            || file.Equals("IntegrationEventOutboxProcessor.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatFailingTypeNames(TestResult result)
    {
        IReadOnlyList<string>? names = result.FailingTypeNames;

        if (names is null || names.Count == 0)
        {
            return "(none reported)";
        }

        return string.Join(", ", names);
    }
}

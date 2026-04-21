using System.Reflection;

using ArchLucid.Application;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

namespace ArchLucid.Architecture.Tests;

/// <summary>
/// ADR 0021 Phase 1 guard: internal Application-layer types must not take a direct coordinator manifest repository
/// dependency except on the documented write-path allow-list (commit, replay seed, demo seed).
/// </summary>
[Trait("Suite", "Architecture")]
public sealed class DualPipelineInternalReadPathTests
{
    private static readonly HashSet<string> CoordinatorManifestCtorAllowList =
    [
        "ArchLucid.Application.Runs.Orchestration.ArchitectureRunCommitOrchestrator",
        "ArchLucid.Application.ReplayRunService",
        "ArchLucid.Application.Bootstrap.DemoSeedService",
    ];

    [Fact]
    public void Application_constructors_do_not_take_ICoordinatorGoldenManifestRepository_outside_allow_list()
    {
        Assembly application = typeof(RunDetailQueryService).Assembly;
        Type coordinatorRepo = typeof(ICoordinatorGoldenManifestRepository);

        List<string> violations = [];

        foreach (Type type in application.GetTypes())
        {
            if (type.Namespace is null || !type.Namespace.StartsWith("ArchLucid.Application", StringComparison.Ordinal))
                continue;

            if (type.IsAbstract && type.IsSealed)
                continue;


            foreach (ConstructorInfo ctor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public))
            {
                foreach (ParameterInfo parameter in ctor.GetParameters())
                {
                    if (parameter.ParameterType != coordinatorRepo)
                        continue;

                    string fullName = type.FullName ?? type.Name;

                    if (CoordinatorManifestCtorAllowList.Contains(fullName))
                        continue;


                    violations.Add($"{fullName} ctor parameter {parameter.Name}");
                }
            }
        }

        violations.Should().BeEmpty(
            because: "internal reads must go through IUnifiedGoldenManifestReader (ADR 0021 Phase 1); coordinator repository remains for writes on the allow-list only.");
    }
}

using System.Reflection;

using ArchLucid.Contracts.Metadata;

using FluentAssertions;

using NetArchTest.Rules;

namespace ArchLucid.Architecture.Tests;

/// <summary>
/// <c>ArchLucid.Contracts</c> (DTOs and port interfaces under <c>ArchLucid.Contracts.Abstractions.*</c>) is a
/// leaf assembly — it must not pull in framework concretes like ASP.NET Core or SqlClient,
/// because doing so couples every downstream consumer to the same framework choices.
/// </summary>
public sealed class ContractsFrameworkIsolationTests
{
    /// <summary>Forbidden framework namespace prefixes for the Contracts assembly.</summary>
    private static readonly string[] ForbiddenFrameworkNamespaces =
    [
        "Microsoft.AspNetCore",
        "Microsoft.Data.SqlClient",
        "System.Data.SqlClient",
        "Dapper",
        "DbUp",
    ];

    [Fact]
    [Trait("Suite", "Core")]
    [Trait("Category", "Unit")]
    public void Contracts_must_not_reference_AspNetCore_or_data_access_libraries()
    {
        Assembly contracts = typeof(ArchitectureRun).Assembly;

        TestResult result = Types
            .InAssembly(contracts)
            .ShouldNot()
            .HaveDependencyOnAny(ForbiddenFrameworkNamespaces)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "ArchLucid.Contracts is a shared DTO and port-definition leaf consumed by every host (API, Worker, CLI, Api.Client). Adding AspNetCore or SqlClient here couples every consumer. Offending types: {0}",
            FormatFailingTypeNames(result));
    }

    private static string FormatFailingTypeNames(TestResult result)
    {
        IReadOnlyList<string>? names = result.FailingTypeNames;

        if (names is null || names.Count == 0) return "(none reported)";

        return string.Join(", ", names);
    }
}

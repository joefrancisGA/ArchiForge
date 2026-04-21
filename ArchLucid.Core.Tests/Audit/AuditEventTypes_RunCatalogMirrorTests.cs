using System.Reflection;

using ArchLucid.Core.Audit;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Audit;

/// <summary>
/// ADR 0021 Phase 2: every legacy <c>CoordinatorRun*</c> durable constant must have a stable <see cref="AuditEventTypes.Run"/> twin.
/// </summary>
[Trait("Suite", "Core")]
public sealed class AuditEventTypes_RunCatalogMirrorTests
{
    private static readonly IReadOnlyDictionary<string, string> LegacyToCanonical = new Dictionary<string, string>
    {
        [AuditEventTypes.CoordinatorRunCreated] = AuditEventTypes.Run.Created,
        [AuditEventTypes.CoordinatorRunExecuteStarted] = AuditEventTypes.Run.ExecuteStarted,
        [AuditEventTypes.CoordinatorRunExecuteSucceeded] = AuditEventTypes.Run.ExecuteSucceeded,
        [AuditEventTypes.CoordinatorRunCommitCompleted] = AuditEventTypes.Run.CommitCompleted,
        [AuditEventTypes.CoordinatorRunFailed] = AuditEventTypes.Run.Failed,
    };

    [Fact]
    public void Every_coordinator_run_legacy_constant_has_a_Run_catalog_twin_with_distinct_wire_value()
    {
        IReadOnlyDictionary<string, string> legacyCoordinator = GetTopLevelCoordinatorRunConstants();

        legacyCoordinator.Should().NotBeEmpty();

        IReadOnlySet<string> canonicalValues = GetRunNestedConstantValues().ToHashSet();

        foreach (KeyValuePair<string, string> pair in legacyCoordinator)
        {
            string legacyValue = pair.Value;

            LegacyToCanonical.TryGetValue(legacyValue, out string? canonical)
                .Should()
                .BeTrue(because: $"legacy {pair.Key} must map to AuditEventTypes.Run.* (wire: {legacyValue})");

            canonicalValues.Should().Contain(
                canonical!,
                because: $"canonical twin for {legacyValue} must exist as a public const on AuditEventTypes.Run");

            canonical.Should().NotBe(legacyValue, because: "Phase 2 requires distinct wire strings for legacy vs canonical rows");
        }
    }

    [Fact]
    public void Run_nested_catalog_exposes_only_expected_constant_count()
    {
        List<string> runConstants = typeof(AuditEventTypes.Run)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        runConstants.Should().HaveCount(LegacyToCanonical.Count);
    }

    private static IEnumerable<string> GetRunNestedConstantValues() =>
        typeof(AuditEventTypes.Run)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!);

    private static IReadOnlyDictionary<string, string> GetTopLevelCoordinatorRunConstants() =>
        typeof(AuditEventTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Where(f => f.Name.StartsWith("CoordinatorRun", StringComparison.Ordinal))
            .ToDictionary(f => f.Name, f => (string)f.GetRawConstantValue()!);
}

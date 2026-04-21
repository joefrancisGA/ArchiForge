using System.Reflection;

using ArchLucid.Core.Audit;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Audit;

/// <summary>
/// Pins the invariant from <c>docs/AUDIT_COVERAGE_MATRIX.md</c> that the two
/// audit channels (Coordinator orchestration vs Authority pipeline) never
/// silently share an <c>EventType</c> wire value, and that the baseline log
/// channel never collides with the durable SQL channel. Both invariants are
/// described in the design-notes table of that document.
/// </summary>
/// <remarks>
/// This test deliberately uses reflection so it stays correct as
/// <see cref="AuditEventTypes"/> grows. Adding a new constant that breaks the
/// invariant fails the build instead of degrading silently to a duplicated
/// audit row in <c>dbo.AuditEvents</c>.
/// </remarks>
[Trait("Suite", "Core")]
public sealed class AuditEventTypes_DoNotCollideAcrossPipelinesTests
{
    /// <summary>
    /// Coordinator-only event types that have <b>no</b> authority counterpart by design.
    /// Each entry must be justified with a one-line comment citing the rationale; new
    /// entries require a corresponding update to <c>docs/AUDIT_COVERAGE_MATRIX.md</c>.
    /// </summary>
    private static readonly IReadOnlySet<string> CoordinatorOnlyEventTypes = new HashSet<string>
    {
        // Coordinator pipeline executes runs in three discrete orchestrator stages
        // (Create, Execute, Commit). The Authority pipeline is one-shot and does not
        // distinguish "execute started" / "execute succeeded" — these two have no
        // authority equivalent. See docs/AUDIT_COVERAGE_MATRIX.md design row
        // "Coordinator orchestration dual-write".
        AuditEventTypes.CoordinatorRunExecuteStarted,
        AuditEventTypes.CoordinatorRunExecuteSucceeded,

        // The Authority pipeline does not currently raise a top-level "RunFailed"
        // event (it raises domain-specific RunStarted / RunCompleted only). The
        // Coordinator pipeline does raise CoordinatorRunFailed for its own forensic
        // chain. Keep coordinator-only until ADR 0021 collapses the families.
        AuditEventTypes.CoordinatorRunFailed,
    };

    /// <summary>
    /// Coordinator → Authority counterpart mapping for every coordinator constant
    /// that <b>does</b> have an authority equivalent. The strangler plan in proposed
    /// ADR 0021 will rename these on the wire; until then the mapping is the seam
    /// that prevents silent drift.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> CoordinatorToAuthorityMap = new Dictionary<string, string>
    {
        [AuditEventTypes.CoordinatorRunCreated] = AuditEventTypes.RunStarted,
        [AuditEventTypes.CoordinatorRunCommitCompleted] = AuditEventTypes.RunCompleted,
    };

    [Fact]
    public void CoordinatorRunConstants_DoNotShareValuesWithAuthorityRunConstants()
    {
        IReadOnlySet<string> coordinatorValues = GetTopLevelStringConstantValues()
            .Where(pair => pair.Key.StartsWith("CoordinatorRun", StringComparison.Ordinal))
            .Select(pair => pair.Value)
            .ToHashSet();

        IReadOnlySet<string> authorityRunValues = new[] { AuditEventTypes.RunStarted, AuditEventTypes.RunCompleted }
            .ToHashSet();

        coordinatorValues.Should().NotBeEmpty(because: "AuditEventTypes still defines coordinator constants");
        authorityRunValues.Should().NotBeEmpty(because: "AuditEventTypes still defines top-level RunStarted/RunCompleted");

        coordinatorValues.Overlaps(authorityRunValues).Should().BeFalse(
            because: "docs/AUDIT_COVERAGE_MATRIX.md design row 'Coordinator orchestration dual-write' requires distinct CoordinatorRun* and authority RunStarted/RunCompleted values; ADR 0021 will plan the unification");
    }

    [Fact]
    public void BaselineAuditTypes_DoNotShareValuesWithDurableTopLevelTypes()
    {
        IReadOnlySet<string> topLevelValues = GetTopLevelStringConstantValues()
            .Select(pair => pair.Value)
            .ToHashSet();

        List<string> baselineValues = GetNestedStringConstantValues(typeof(AuditEventTypes.Baseline)).ToList();

        baselineValues.Should().NotBeEmpty(because: "AuditEventTypes.Baseline still defines nested constants");

        IEnumerable<string> collisions = baselineValues.Where(topLevelValues.Contains);

        collisions.Should().BeEmpty(
            because: "docs/AUDIT_COVERAGE_MATRIX.md design row 'Single Core catalog for baseline + durable' requires baseline 'Architecture.*' / 'Governance.*' values to differ from top-level durable values");
    }

    [Fact]
    public void AllAuditEventTypeValues_AreUniqueAcrossCatalog()
    {
        List<string> allValues = GetTopLevelStringConstantValues().Select(pair => pair.Value)
            .Concat(GetNestedStringConstantValues(typeof(AuditEventTypes.Baseline)))
            .Concat(GetNestedStringConstantValues(typeof(AuditEventTypes.Run)))
            .ToList();

        IEnumerable<IGrouping<string, string>> duplicates = allValues
            .GroupBy(value => value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1);

        duplicates.Should().BeEmpty(
            because: "Two AuditEventTypes constants must never share a wire value; if a deliberate alias is needed, add it to the test allow-list with an inline ADR citation");
    }

    [Fact]
    public void EveryCoordinatorRunConstant_HasMatchingAuthorityCounterpart_OrIsExplicitlyMarkedCoordinatorOnly()
    {
        IReadOnlyList<string> coordinatorValues = GetTopLevelStringConstantValues()
            .Where(pair => pair.Key.StartsWith("CoordinatorRun", StringComparison.Ordinal))
            .Select(pair => pair.Value)
            .ToList();

        IReadOnlySet<string> topLevelValues = GetTopLevelStringConstantValues()
            .Select(pair => pair.Value)
            .ToHashSet();

        List<string> unmapped = [];

        foreach (string coordinatorValue in coordinatorValues)
        {
            if (CoordinatorOnlyEventTypes.Contains(coordinatorValue)) continue;

            if (!CoordinatorToAuthorityMap.TryGetValue(coordinatorValue, out string? authorityValue))
            {
                unmapped.Add($"{coordinatorValue} (no entry in CoordinatorToAuthorityMap and not in CoordinatorOnlyEventTypes)");
                continue;
            }

            if (!topLevelValues.Contains(authorityValue)) unmapped.Add($"{coordinatorValue} → {authorityValue} (mapped value is not a top-level AuditEventTypes constant)");
        }

        unmapped.Should().BeEmpty(
            because: "ADR 0021 (proposed) plans the strangler rename; this test pins the coordinator→authority mapping in the meantime so no new coordinator constant slips in unmapped");
    }

    private static IEnumerable<KeyValuePair<string, string>> GetTopLevelStringConstantValues() =>
        typeof(AuditEventTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => new KeyValuePair<string, string>(f.Name, (string)f.GetRawConstantValue()!));

    private static IEnumerable<string> GetNestedStringConstantValues(Type containerType)
    {
        if (containerType is null) throw new ArgumentNullException(nameof(containerType));

        IEnumerable<string> direct = containerType
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!);

        IEnumerable<string> nested = containerType
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(GetNestedStringConstantValues);

        return direct.Concat(nested);
    }
}

using System.Reflection;

using ArchLucid.Core.Audit;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Audit;

/// <summary>
///     Pins the invariant from <c>docs/AUDIT_COVERAGE_MATRIX.md</c> that the two
///     audit channels (Run-stage catalog vs Authority pipeline) never
///     silently share an <c>EventType</c> wire value, and that the baseline log
///     channel never collides with the durable SQL channel.
/// </summary>
[Trait("Suite", "Core")]
public sealed class AuditEventTypesDoNotCollideAcrossPipelinesTests
{
    [Fact]
    public void RunCatalogConstants_DoNotShareValuesWithAuthorityRunConstants()
    {
        IReadOnlySet<string> runCatalogValues = typeof(AuditEventTypes.Run)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet();

        IReadOnlySet<string> authorityRunValues = new[] { AuditEventTypes.RunStarted, AuditEventTypes.RunCompleted }
            .ToHashSet();

        runCatalogValues.Should().NotBeEmpty("AuditEventTypes.Run still defines catalog constants");
        authorityRunValues.Should().NotBeEmpty("AuditEventTypes still defines top-level RunStarted/RunCompleted");

        runCatalogValues.Overlaps(authorityRunValues).Should().BeFalse(
            "Run.* durable catalog must stay distinct from authority RunStarted/RunCompleted wire values");
    }

    [Fact]
    public void BaselineAuditTypes_DoNotShareValuesWithDurableTopLevelTypes()
    {
        IReadOnlySet<string> topLevelValues = GetTopLevelStringConstantValues()
            .Select(pair => pair.Value)
            .ToHashSet();

        List<string> baselineValues = GetNestedStringConstantValues(typeof(AuditEventTypes.Baseline)).ToList();

        baselineValues.Should().NotBeEmpty("AuditEventTypes.Baseline still defines nested constants");

        IEnumerable<string> collisions = baselineValues.Where(topLevelValues.Contains);

        collisions.Should().BeEmpty(
            "docs/AUDIT_COVERAGE_MATRIX.md design row 'Single Core catalog for baseline + durable' requires baseline 'Architecture.*' / 'Governance.*' values to differ from top-level durable values");
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
            "Two AuditEventTypes constants must never share a wire value; if a deliberate alias is needed, add it to the test allow-list with an inline ADR citation");
    }

    private static IEnumerable<KeyValuePair<string, string>> GetTopLevelStringConstantValues()
    {
        return typeof(AuditEventTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => new KeyValuePair<string, string>(f.Name, (string)f.GetRawConstantValue()!));
    }

    private static IEnumerable<string> GetNestedStringConstantValues(Type containerType)
    {
        if (containerType is null)
            throw new ArgumentNullException(nameof(containerType));

        IEnumerable<string> direct = containerType
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!);

        IEnumerable<string> nested = containerType
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(GetNestedStringConstantValues);

        return direct.Concat(nested);
    }
}

using ArchLucid.Application.Analysis;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>Unit tests for <see cref="ComparisonDriftAnalyzer"/>.</summary>
[Trait("Category", "Unit")]
public sealed class ComparisonDriftAnalyzerTests
{
    private readonly ComparisonDriftAnalyzer _sut = new();

    [Fact]
    public void Analyze_IdenticalObjects_NoDrift()
    {
        object payload = new { Name = "ArchLucid", Version = 1 };

        DriftAnalysisResult result = _sut.Analyze(payload, payload);

        result.DriftDetected.Should().BeFalse();
        result.Items.Should().BeEmpty();
        result.Summary.Should().Contain("No drift");
    }

    [Fact]
    public void Analyze_ChangedScalarProperty_DetectsValueChange()
    {
        object stored = new { Name = "old" };
        object regenerated = new { Name = "new" };

        DriftAnalysisResult result = _sut.Analyze(stored, regenerated);

        result.DriftDetected.Should().BeTrue();
        result.Items.Should().ContainSingle(i =>
            i.Category == "ValueChange" &&
            i.Path == "$.Name" &&
            i.StoredValue == "old" &&
            i.RegeneratedValue == "new");
    }

    [Fact]
    public void Analyze_AddedProperty_DetectsAdded()
    {
        object stored = new { Name = "x" };
        object regenerated = new { Name = "x", Extra = "added" };

        DriftAnalysisResult result = _sut.Analyze(stored, regenerated);

        result.DriftDetected.Should().BeTrue();
        result.Items.Should().Contain(i => i.Category == "Added" && i.Path == "$.Extra");
    }

    [Fact]
    public void Analyze_RemovedProperty_DetectsRemoved()
    {
        object stored = new { Name = "x", Old = "gone" };
        object regenerated = new { Name = "x" };

        DriftAnalysisResult result = _sut.Analyze(stored, regenerated);

        result.DriftDetected.Should().BeTrue();
        result.Items.Should().Contain(i => i.Category == "Removed" && i.Path == "$.Old");
    }

    [Fact]
    public void Analyze_ArrayLengthChange_DetectsArrayLengthDrift()
    {
        object stored = new { Items = new[] { 1, 2, 3 } };
        object regenerated = new { Items = new[] { 1, 2 } };

        DriftAnalysisResult result = _sut.Analyze(stored, regenerated);

        result.DriftDetected.Should().BeTrue();
        result.Items.Should().Contain(i =>
            i.Category == "ArrayLength" &&
            i.StoredValue == "3" &&
            i.RegeneratedValue == "2");
    }

    [Fact]
    public void Analyze_ArrayElementChange_DetectsValueChange()
    {
        object stored = new { Tags = new[] { "a", "b" } };
        object regenerated = new { Tags = new[] { "a", "X" } };

        DriftAnalysisResult result = _sut.Analyze(stored, regenerated);

        result.DriftDetected.Should().BeTrue();
        result.Items.Should().Contain(i =>
            i.Category == "ValueChange" &&
            i.Path == "$.Tags[1]");
    }

    [Fact]
    public void Analyze_TypeChange_DetectsTypeChange()
    {
        // Force JSON type difference: string vs number at same path.
        object stored = new { Value = "hello" };
        object regenerated = new { Value = 42 };

        DriftAnalysisResult result = _sut.Analyze(stored, regenerated);

        result.DriftDetected.Should().BeTrue();
        result.Items.Should().Contain(i => i.Category == "TypeChange" && i.Path == "$.Value");
    }

    [Fact]
    public void Analyze_NestedObjectDrift_ReportsNestedPath()
    {
        object stored = new { Outer = new { Inner = "a" } };
        object regenerated = new { Outer = new { Inner = "b" } };

        DriftAnalysisResult result = _sut.Analyze(stored, regenerated);

        result.DriftDetected.Should().BeTrue();
        result.Items.Should().Contain(i => i.Path == "$.Outer.Inner");
    }

    [Fact]
    public void Analyze_SummaryIncludesDriftCount()
    {
        object stored = new { A = 1, B = 2 };
        object regenerated = new { A = 9, B = 8 };

        DriftAnalysisResult result = _sut.Analyze(stored, regenerated);

        result.Summary.Should().Contain("2 drift differences");
    }
}

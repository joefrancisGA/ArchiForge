using ArchLucid.Contracts.Findings;
using ArchLucid.Decisioning.Findings;

using FluentAssertions;

namespace ArchLucid.Decisioning.Tests.Findings;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class FindingConfidenceCalculatorTests
{
    private readonly FindingConfidenceCalculator _sut = new();

    [Fact]
    public void Calculate_when_schema_and_reference_pass_and_ratio_zero_scores_75_and_High()
    {
        FindingConfidenceCalculationResult? r = _sut.Calculate(true, true, 0m);

        r.Should().NotBeNull();
        r!.Score.Should().Be(75);
        r.Level.Should().Be(FindingConfidenceLevel.High);
    }

    [Fact]
    public void Calculate_when_schema_and_reference_pass_and_ratio_one_scores_100_and_High()
    {
        FindingConfidenceCalculationResult? r = _sut.Calculate(true, true, 1m);

        r.Should().NotBeNull();
        r!.Score.Should().Be(100);
        r.Level.Should().Be(FindingConfidenceLevel.High);
    }

    [Fact]
    public void Calculate_when_schema_only_and_ratio_full_scores_60_and_Medium()
    {
        FindingConfidenceCalculationResult? r = _sut.Calculate(true, false, 1m);

        r.Should().NotBeNull();
        r!.Score.Should().Be(60);
        r.Level.Should().Be(FindingConfidenceLevel.Medium);
    }

    [Fact]
    public void Calculate_when_reference_only_and_ratio_full_scores_65_and_Medium()
    {
        FindingConfidenceCalculationResult? r = _sut.Calculate(false, true, 1m);

        r.Should().NotBeNull();
        r!.Score.Should().Be(65);
        r.Level.Should().Be(FindingConfidenceLevel.Medium);
    }

    [Fact]
    public void Calculate_medium_lower_boundary_at_45()
    {
        FindingConfidenceCalculationResult? r = _sut.Calculate(true, false, 10m / 25m);

        r.Should().NotBeNull();
        r!.Score.Should().Be(45);
        r.Level.Should().Be(FindingConfidenceLevel.Medium);
    }

    [Fact]
    public void Calculate_low_upper_boundary_at_44()
    {
        FindingConfidenceCalculationResult? r = _sut.Calculate(true, false, 9m / 25m);

        r.Should().NotBeNull();
        r!.Score.Should().Be(44);
        r.Level.Should().Be(FindingConfidenceLevel.Low);
    }

    [Fact]
    public void Calculate_when_neither_flag_and_ratio_null_scores_zero_and_Low()
    {
        FindingConfidenceCalculationResult? r = _sut.Calculate(false, false, null);

        r.Should().NotBeNull();
        r!.Score.Should().Be(0);
        r.Level.Should().Be(FindingConfidenceLevel.Low);
    }

    [Fact]
    public void Calculate_trace_ratio_half_without_flags_scores_13_rounded_and_Low()
    {
        FindingConfidenceCalculationResult? r = _sut.Calculate(false, false, 0.5m);

        r.Should().NotBeNull();
        r!.Score.Should().Be(13);
        r.Level.Should().Be(FindingConfidenceLevel.Low);
    }

    [Fact]
    public void Calculate_trace_ratio_half_with_schema_scores_48_rounded_and_Medium()
    {
        FindingConfidenceCalculationResult? r = _sut.Calculate(true, false, 0.5m);

        r.Should().NotBeNull();
        r!.Score.Should().Be(48);
        r.Level.Should().Be(FindingConfidenceLevel.Medium);
    }

    [Fact]
    public void Calculate_ratio_clamped_above_one_behaves_like_one()
    {
        FindingConfidenceCalculationResult? r = _sut.Calculate(false, false, 3m);

        r.Should().NotBeNull();
        r!.Score.Should().Be(25);
        r.Level.Should().Be(FindingConfidenceLevel.Low);
    }

    [Fact]
    public void Calculate_ratio_negative_clamped_to_zero()
    {
        FindingConfidenceCalculationResult? r = _sut.Calculate(true, false, -2m);

        r.Should().NotBeNull();
        r!.Score.Should().Be(35);
        r.Level.Should().Be(FindingConfidenceLevel.Low);
    }
}

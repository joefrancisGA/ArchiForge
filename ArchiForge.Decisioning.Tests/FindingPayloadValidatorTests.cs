using ArchiForge.Decisioning.Findings;
using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Models;
using ArchiForge.Decisioning.Services;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

[Trait("Category", "Unit")]
public sealed class FindingPayloadValidatorTests
{
    private readonly FindingPayloadValidator _sut = new();

    [Fact]
    public void Validate_WhenFindingIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _sut.Validate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenFindingTypeMissing_Throws(string findingType)
    {
        Finding finding = BaseEnvelope(findingType, "c", "e");

        Action act = () => _sut.Validate(finding);

        act.Should().Throw<InvalidOperationException>().WithMessage("*FindingType*");
    }

    [Fact]
    public void Validate_WhenPayloadTypeSetButPayloadNull_Throws()
    {
        Finding finding = BaseEnvelope(FindingTypes.ComplianceFinding, "c", "e");
        finding.PayloadType = nameof(ComplianceFindingPayload);
        finding.Payload = null;

        Action act = () => _sut.Validate(finding);

        act.Should().Throw<InvalidOperationException>().WithMessage("*PayloadType*");
    }

    [Fact]
    public void Validate_WhenUnknownFindingTypeWithValidEnvelope_DoesNotThrow()
    {
        Finding finding = BaseEnvelope("CustomFinding", "c", "e");
        finding.Payload = null;

        Action act = () => _sut.Validate(finding);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ComplianceFinding_WhenPayloadNull_Throws()
    {
        Finding finding = BaseEnvelope(FindingTypes.ComplianceFinding, "c", "e");
        finding.Payload = null;

        Action act = () => _sut.Validate(finding);

        act.Should().Throw<InvalidOperationException>().WithMessage("*ComplianceFinding*");
    }

    [Fact]
    public void Validate_ComplianceFinding_WhenPayloadValid_DoesNotThrow()
    {
        Finding finding = BaseEnvelope(FindingTypes.ComplianceFinding, "c", "e");
        finding.Payload = new ComplianceFindingPayload
        {
            RulePackId = "rp",
            RulePackVersion = "1",
            RuleId = "r1",
            ControlId = "c1",
            ControlName = "cn",
            AppliesToCategory = "cat",
        };
        finding.PayloadType = nameof(ComplianceFindingPayload);

        Action act = () => _sut.Validate(finding);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_RequirementFinding_WhenPayloadNull_Throws()
    {
        Finding finding = BaseEnvelope(FindingTypes.RequirementFinding, "c", "e");
        finding.Payload = null;

        Action act = () => _sut.Validate(finding);

        act.Should().Throw<InvalidOperationException>().WithMessage("*RequirementFinding*");
    }

    [Fact]
    public void Validate_AllRegisteredPayloadKinds_WhenMinimalValid_DoesNotThrow()
    {
        (string Type, object Payload)[] cases =
        [
            (FindingTypes.RequirementFinding, new RequirementFindingPayload { RequirementText = "t", RequirementName = "n" }),
            (FindingTypes.TopologyGap, new TopologyGapFindingPayload { GapCode = "g", Description = "d", Impact = "i" }),
            (FindingTypes.SecurityControlFinding, new SecurityControlFindingPayload { ControlId = "c", ControlName = "n", Status = "s", Impact = "i" }),
            (FindingTypes.CostConstraintFinding, new CostConstraintFindingPayload { BudgetName = "b", CostRisk = "low" }),
            (FindingTypes.PolicyApplicabilityFinding, new PolicyApplicabilityFindingPayload()),
            (FindingTypes.TopologyCoverageFinding, new TopologyCoverageFindingPayload()),
            (FindingTypes.SecurityCoverageFinding, new SecurityCoverageFindingPayload()),
            (FindingTypes.PolicyCoverageFinding, new PolicyCoverageFindingPayload()),
            (FindingTypes.RequirementCoverageFinding, new RequirementCoverageFindingPayload()),
        ];

        foreach ((string type, object payload) in cases)
        {
            Finding finding = BaseEnvelope(type, "c", "e");
            finding.Payload = payload;

            Action act = () => _sut.Validate(finding);

            act.Should().NotThrow($"for {type}");
        }
    }

    private static Finding BaseEnvelope(string findingType, string category, string engineType) =>
        new()
        {
            FindingType = findingType,
            Category = category,
            EngineType = engineType,
            Title = "t",
            Rationale = "r",
            Severity = FindingSeverity.Info,
        };
}

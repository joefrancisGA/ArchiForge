using ArchiForge.Decisioning.Compliance.Models;
using ArchiForge.Decisioning.Governance.PolicyPacks;
using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

public sealed class ComplianceRulePackGovernanceFilterTests
{
    private static ComplianceRulePack Pack(params ComplianceRule[] rules) =>
        new()
        {
            RulePackId = "test-pack",
            Name = "Test",
            Version = "1",
            Rules = rules.ToList(),
        };

    private static ComplianceRule Rule(string id) =>
        new()
        {
            RuleId = id,
            ControlId = "c",
            ControlName = "n",
            AppliesToCategory = "cat",
            RequiredNodeType = "t",
            RequiredEdgeType = "e",
            Description = "d",
        };

    [Fact]
    public void Filter_WhenNoComplianceRestrictions_ReturnsOriginalPack()
    {
        var source = Pack(Rule("a"), Rule("b"));
        var effective = new PolicyPackContentDocument();

        var filtered = ComplianceRulePackGovernanceFilter.Filter(source, effective);

        filtered.Rules.Should().HaveCount(2);
        filtered.RulePackId.Should().Be(source.RulePackId);
    }

    [Fact]
    public void Filter_ByComplianceRuleKeys_KeepsOnlyMatches_CaseInsensitive()
    {
        var source = Pack(Rule("Alpha-Rule"), Rule("beta-rule"), Rule("gamma"));
        var effective = new PolicyPackContentDocument
        {
            ComplianceRuleKeys = ["ALPHA-RULE", "gamma"],
        };

        var filtered = ComplianceRulePackGovernanceFilter.Filter(source, effective);

        filtered.Rules.Select(r => r.RuleId).Should().BeEquivalentTo("Alpha-Rule", "gamma");
    }

    [Fact]
    public void Filter_ByComplianceRuleIds_ParsesGuidFromRuleIdString()
    {
        var g = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var source = Pack(Rule(g.ToString("D")), Rule("other"));
        var effective = new PolicyPackContentDocument
        {
            ComplianceRuleIds = [g],
        };

        var filtered = ComplianceRulePackGovernanceFilter.Filter(source, effective);

        filtered.Rules.Should().ContainSingle(r => r.RuleId == g.ToString("D"));
    }

    [Fact]
    public void Filter_CombinesKeysAndIds()
    {
        var g = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var source = Pack(Rule("by-key"), Rule(g.ToString("D")), Rule("drop-me"));
        var effective = new PolicyPackContentDocument
        {
            ComplianceRuleKeys = ["by-key"],
            ComplianceRuleIds = [g],
        };

        var filtered = ComplianceRulePackGovernanceFilter.Filter(source, effective);

        filtered.Rules.Select(r => r.RuleId).Should().BeEquivalentTo("by-key", g.ToString("D"));
    }
}

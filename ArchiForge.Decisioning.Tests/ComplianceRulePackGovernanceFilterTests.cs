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
        ComplianceRulePack source = Pack(Rule("a"), Rule("b"));
        PolicyPackContentDocument effective = new();

        ComplianceRulePack filtered = ComplianceRulePackGovernanceFilter.Filter(source, effective);

        filtered.Rules.Should().HaveCount(2);
        filtered.RulePackId.Should().Be(source.RulePackId);
    }

    [Fact]
    public void Filter_ByComplianceRuleKeys_KeepsOnlyMatches_CaseInsensitive()
    {
        ComplianceRulePack source = Pack(Rule("Alpha-Rule"), Rule("beta-rule"), Rule("gamma"));
        PolicyPackContentDocument effective = new()
        {
            ComplianceRuleKeys = ["ALPHA-RULE", "gamma"],
        };

        ComplianceRulePack filtered = ComplianceRulePackGovernanceFilter.Filter(source, effective);

        filtered.Rules.Select(r => r.RuleId).Should().BeEquivalentTo("Alpha-Rule", "gamma");
    }

    [Fact]
    public void Filter_ByComplianceRuleIds_ParsesGuidFromRuleIdString()
    {
        Guid g = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        ComplianceRulePack source = Pack(Rule(g.ToString("D")), Rule("other"));
        PolicyPackContentDocument effective = new()
        {
            ComplianceRuleIds = [g],
        };

        ComplianceRulePack filtered = ComplianceRulePackGovernanceFilter.Filter(source, effective);

        filtered.Rules.Should().ContainSingle(r => r.RuleId == g.ToString("D"));
    }

    [Fact]
    public void Filter_CombinesKeysAndIds()
    {
        Guid g = Guid.Parse("11111111-2222-3333-4444-555555555555");
        ComplianceRulePack source = Pack(Rule("by-key"), Rule(g.ToString("D")), Rule("drop-me"));
        PolicyPackContentDocument effective = new()
        {
            ComplianceRuleKeys = ["by-key"],
            ComplianceRuleIds = [g],
        };

        ComplianceRulePack filtered = ComplianceRulePackGovernanceFilter.Filter(source, effective);

        filtered.Rules.Select(r => r.RuleId).Should().BeEquivalentTo("by-key", g.ToString("D"));
    }
}

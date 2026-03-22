using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Composite;
using ArchiForge.Decisioning.Governance.PolicyPacks;
using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

public sealed class PolicyPackGovernanceFilterTests
{
    private static AlertRule Rule(Guid id, string name = "r") =>
        new()
        {
            RuleId = id,
            Name = name,
            RuleType = "FindingCount",
            Severity = "Warning",
        };

    private static CompositeAlertRule Composite(Guid id, string name = "c") =>
        new()
        {
            CompositeRuleId = id,
            Name = name,
            Severity = "Warning",
            Operator = "And",
            Conditions = [],
        };

    [Fact]
    public void FilterAlertRules_EmptyPackList_ReturnsAll()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var rules = new List<AlertRule> { Rule(a), Rule(b) };
        var effective = new PolicyPackContentDocument();

        var filtered = PolicyPackGovernanceFilter.FilterAlertRules(rules, effective);

        filtered.Should().HaveCount(2);
    }

    [Fact]
    public void FilterAlertRules_NonEmptyPackList_KeepsOnlyListedIds()
    {
        var keep = Guid.NewGuid();
        var drop = Guid.NewGuid();
        var rules = new List<AlertRule> { Rule(keep), Rule(drop) };
        var effective = new PolicyPackContentDocument { AlertRuleIds = [keep] };

        var filtered = PolicyPackGovernanceFilter.FilterAlertRules(rules, effective);

        filtered.Should().ContainSingle(r => r.RuleId == keep);
    }

    [Fact]
    public void FilterCompositeRules_EmptyPackList_ReturnsAll()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var rules = new List<CompositeAlertRule> { Composite(a), Composite(b) };
        var effective = new PolicyPackContentDocument();

        var filtered = PolicyPackGovernanceFilter.FilterCompositeRules(rules, effective);

        filtered.Should().HaveCount(2);
    }

    [Fact]
    public void FilterCompositeRules_NonEmptyPackList_KeepsOnlyListedIds()
    {
        var keep = Guid.NewGuid();
        var drop = Guid.NewGuid();
        var rules = new List<CompositeAlertRule> { Composite(keep), Composite(drop) };
        var effective = new PolicyPackContentDocument { CompositeAlertRuleIds = [keep] };

        var filtered = PolicyPackGovernanceFilter.FilterCompositeRules(rules, effective);

        filtered.Should().ContainSingle(r => r.CompositeRuleId == keep);
    }
}

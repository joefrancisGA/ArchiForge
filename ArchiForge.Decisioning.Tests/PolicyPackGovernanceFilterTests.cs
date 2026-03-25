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
        Guid a = Guid.NewGuid();
        Guid b = Guid.NewGuid();
        List<AlertRule> rules = new List<AlertRule> { Rule(a), Rule(b) };
        PolicyPackContentDocument effective = new PolicyPackContentDocument();

        List<AlertRule> filtered = PolicyPackGovernanceFilter.FilterAlertRules(rules, effective);

        filtered.Should().HaveCount(2);
    }

    [Fact]
    public void FilterAlertRules_NonEmptyPackList_KeepsOnlyListedIds()
    {
        Guid keep = Guid.NewGuid();
        Guid drop = Guid.NewGuid();
        List<AlertRule> rules = new List<AlertRule> { Rule(keep), Rule(drop) };
        PolicyPackContentDocument effective = new PolicyPackContentDocument { AlertRuleIds = [keep] };

        List<AlertRule> filtered = PolicyPackGovernanceFilter.FilterAlertRules(rules, effective);

        filtered.Should().ContainSingle(r => r.RuleId == keep);
    }

    [Fact]
    public void FilterCompositeRules_EmptyPackList_ReturnsAll()
    {
        Guid a = Guid.NewGuid();
        Guid b = Guid.NewGuid();
        List<CompositeAlertRule> rules = new List<CompositeAlertRule> { Composite(a), Composite(b) };
        PolicyPackContentDocument effective = new PolicyPackContentDocument();

        List<CompositeAlertRule> filtered = PolicyPackGovernanceFilter.FilterCompositeRules(rules, effective);

        filtered.Should().HaveCount(2);
    }

    [Fact]
    public void FilterCompositeRules_NonEmptyPackList_KeepsOnlyListedIds()
    {
        Guid keep = Guid.NewGuid();
        Guid drop = Guid.NewGuid();
        List<CompositeAlertRule> rules = new List<CompositeAlertRule> { Composite(keep), Composite(drop) };
        PolicyPackContentDocument effective = new PolicyPackContentDocument { CompositeAlertRuleIds = [keep] };

        List<CompositeAlertRule> filtered = PolicyPackGovernanceFilter.FilterCompositeRules(rules, effective);

        filtered.Should().ContainSingle(r => r.CompositeRuleId == keep);
    }
}

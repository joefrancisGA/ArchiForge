using ArchLucid.Decisioning.Alerts;
using ArchLucid.Decisioning.Alerts.Composite;

using FsCheck;
using FsCheck.Xunit;

namespace ArchLucid.Decisioning.Tests.Alerts;

/// <summary>Property tests for <see cref="CompositeAlertDeduplicationKeyBuilder"/>.</summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class CompositeAlertDeduplicationKeyPropertyTests
{
    [Property(MaxTest = 200)]
    public void Build_is_deterministic_for_same_inputs(
        Guid ruleId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        NonEmptyString dedupeScope,
        Guid? runId,
        Guid? comparedToRunId)
    {
        CompositeAlertRule rule = CreateRule(ruleId, tenantId, workspaceId, projectId, dedupeScope.Get);
        AlertEvaluationContext ctx = CreateContext(tenantId, workspaceId, projectId, runId, comparedToRunId);

        string a = CompositeAlertDeduplicationKeyBuilder.Build(rule, ctx);
        string b = CompositeAlertDeduplicationKeyBuilder.Build(rule, ctx);

        Assert.Equal(a, b);
    }

    [Property(MaxTest = 200)]
    public void RuleAndRun_key_changes_when_run_id_changes(
        Guid ruleId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid runA,
        Guid runB)
    {
        if (runA == runB)
        {
            runB = Guid.NewGuid();
        }

        CompositeAlertRule rule = CreateRule(ruleId, tenantId, workspaceId, projectId, CompositeDedupeScope.RuleAndRun);
        AlertEvaluationContext ctxA = CreateContext(tenantId, workspaceId, projectId, runA, null);
        AlertEvaluationContext ctxB = CreateContext(tenantId, workspaceId, projectId, runB, null);

        string keyA = CompositeAlertDeduplicationKeyBuilder.Build(rule, ctxA);
        string keyB = CompositeAlertDeduplicationKeyBuilder.Build(rule, ctxB);

        Assert.NotEqual(keyA, keyB);
    }

    [Property(MaxTest = 200)]
    public void RuleOnly_key_ignores_run_id(
        Guid ruleId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid runA,
        Guid runB)
    {
        CompositeAlertRule rule = CreateRule(ruleId, tenantId, workspaceId, projectId, CompositeDedupeScope.RuleOnly);
        AlertEvaluationContext ctxA = CreateContext(tenantId, workspaceId, projectId, runA, null);
        AlertEvaluationContext ctxB = CreateContext(tenantId, workspaceId, projectId, runB, null);

        string keyA = CompositeAlertDeduplicationKeyBuilder.Build(rule, ctxA);
        string keyB = CompositeAlertDeduplicationKeyBuilder.Build(rule, ctxB);

        Assert.Equal(keyA, keyB);
    }

    private static CompositeAlertRule CreateRule(
        Guid compositeRuleId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string dedupeScope)
    {
        return new CompositeAlertRule
        {
            CompositeRuleId = compositeRuleId,
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            Name = "prop-test",
            DedupeScope = dedupeScope,
        };
    }

    private static AlertEvaluationContext CreateContext(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid? runId,
        Guid? comparedToRunId)
    {
        return new AlertEvaluationContext
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            RunId = runId,
            ComparedToRunId = comparedToRunId,
        };
    }
}

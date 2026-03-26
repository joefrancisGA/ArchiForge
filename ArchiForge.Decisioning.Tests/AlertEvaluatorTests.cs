using ArchiForge.Core.Comparison;
using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Advisory.Workflow;
using ArchiForge.Decisioning.Alerts;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

/// <summary>
/// Unit tests for <see cref="AlertEvaluator"/>: one test per rule type covering
/// below-threshold (no alert), at-threshold (alert fires), and boundary conditions.
/// </summary>
[Trait("Category", "Unit")]
public sealed class AlertEvaluatorTests
{
    private readonly AlertEvaluator _sut = new();

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static AlertRule MakeRule(string ruleType, decimal threshold) => new()
    {
        RuleId = Guid.NewGuid(),
        RuleType = ruleType,
        ThresholdValue = threshold,
        IsEnabled = true,
        Severity = AlertSeverity.Warning
    };

    private static AlertEvaluationContext EmptyContext() => new()
    {
        TenantId = Guid.NewGuid(),
        WorkspaceId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid()
    };

    // ──────────────────────────────────────────────────────────────────────────
    // Disabled rule
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_DisabledRule_ProducesNoAlert()
    {
        AlertRule rule = MakeRule(AlertRuleType.CriticalRecommendationCount, threshold: 1);
        rule.IsEnabled = false;

        AlertEvaluationContext ctx = EmptyContext();
        ctx.ImprovementPlan = new ImprovementPlan
        {
            Recommendations = [new ImprovementRecommendation { Urgency = AlertUrgencies.Critical }]
        };

        IReadOnlyList<AlertRecord> result = _sut.Evaluate([rule], ctx);

        result.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CriticalRecommendationCount
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_CriticalRecommendationCount_BelowThreshold_NoAlert()
    {
        AlertRule rule = MakeRule(AlertRuleType.CriticalRecommendationCount, threshold: 3);
        AlertEvaluationContext ctx = EmptyContext();
        ctx.ImprovementPlan = new ImprovementPlan
        {
            Recommendations =
            [
                new ImprovementRecommendation { Urgency = AlertUrgencies.Critical },
                new ImprovementRecommendation { Urgency = "Low" }
            ]
        };

        IReadOnlyList<AlertRecord> result = _sut.Evaluate([rule], ctx);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_CriticalRecommendationCount_AtThreshold_AlertFires()
    {
        AlertRule rule = MakeRule(AlertRuleType.CriticalRecommendationCount, threshold: 2);
        AlertEvaluationContext ctx = EmptyContext();
        ctx.ImprovementPlan = new ImprovementPlan
        {
            Recommendations =
            [
                new ImprovementRecommendation { Urgency = AlertUrgencies.Critical },
                new ImprovementRecommendation { Urgency = AlertUrgencies.High }
            ]
        };

        IReadOnlyList<AlertRecord> result = _sut.Evaluate([rule], ctx);

        result.Should().ContainSingle();
        result[0].Category.Should().Be(AlertCategories.Advisory);
        result[0].TriggerValue.Should().Be("2");
    }

    [Fact]
    public void Evaluate_CriticalRecommendationCount_NullPlan_NoAlert()
    {
        AlertRule rule = MakeRule(AlertRuleType.CriticalRecommendationCount, threshold: 1);
        AlertEvaluationContext ctx = EmptyContext();

        IReadOnlyList<AlertRecord> result = _sut.Evaluate([rule], ctx);

        result.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // NewComplianceGapCount
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_NewComplianceGapCount_BelowThreshold_NoAlert()
    {
        AlertRule rule = MakeRule(AlertRuleType.NewComplianceGapCount, threshold: 5);
        AlertEvaluationContext ctx = EmptyContext();
        ctx.ComparisonResult = new ComparisonResult
        {
            SecurityChanges = [new SecurityDelta { ControlName = "gap" }]
        };

        IReadOnlyList<AlertRecord> result = _sut.Evaluate([rule], ctx);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_NewComplianceGapCount_AtThreshold_AlertFires()
    {
        AlertRule rule = MakeRule(AlertRuleType.NewComplianceGapCount, threshold: 2);
        AlertEvaluationContext ctx = EmptyContext();
        ctx.ComparisonResult = new ComparisonResult
        {
            SecurityChanges = [new SecurityDelta { ControlName = "gap-1" }, new SecurityDelta { ControlName = "gap-2" }]
        };

        IReadOnlyList<AlertRecord> result = _sut.Evaluate([rule], ctx);

        result.Should().ContainSingle();
        result[0].Category.Should().Be(AlertCategories.Compliance);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CostIncreasePercent
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_CostIncreasePercent_NoDelta_NoAlert()
    {
        AlertRule rule = MakeRule(AlertRuleType.CostIncreasePercent, threshold: 10);
        AlertEvaluationContext ctx = EmptyContext();

        IReadOnlyList<AlertRecord> result = _sut.Evaluate([rule], ctx);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_CostIncreasePercent_BelowThreshold_NoAlert()
    {
        AlertRule rule = MakeRule(AlertRuleType.CostIncreasePercent, threshold: 20);
        AlertEvaluationContext ctx = EmptyContext();
        ctx.ComparisonResult = new ComparisonResult
        {
            CostChanges = [new CostDelta { BaseCost = 100m, TargetCost = 110m }]
        };

        IReadOnlyList<AlertRecord> result = _sut.Evaluate([rule], ctx);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_CostIncreasePercent_AtThreshold_AlertFires()
    {
        AlertRule rule = MakeRule(AlertRuleType.CostIncreasePercent, threshold: 10);
        AlertEvaluationContext ctx = EmptyContext();
        ctx.ComparisonResult = new ComparisonResult
        {
            CostChanges = [new CostDelta { BaseCost = 100m, TargetCost = 110m }]
        };

        IReadOnlyList<AlertRecord> result = _sut.Evaluate([rule], ctx);

        result.Should().ContainSingle();
        result[0].Category.Should().Be(AlertCategories.Cost);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DeferredHighPriorityRecommendationAgeDays
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_DeferredHighPriorityAge_NotOldEnough_NoAlert()
    {
        AlertRule rule = MakeRule(AlertRuleType.DeferredHighPriorityRecommendationAgeDays, threshold: 30);
        AlertEvaluationContext ctx = EmptyContext();
        ctx.RecommendationRecords =
        [
            new RecommendationRecord
            {
                Status = RecommendationStatus.Deferred,
                PriorityScore = 90,
                // Updated 5 days ago — inside the 30-day threshold
                LastUpdatedUtc = DateTime.UtcNow.AddDays(-5),
                Title = "Rec",
                RecommendationId = Guid.NewGuid()
            }
        ];

        IReadOnlyList<AlertRecord> result = _sut.Evaluate([rule], ctx);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_DeferredHighPriorityAge_OldEnough_AlertFires()
    {
        AlertRule rule = MakeRule(AlertRuleType.DeferredHighPriorityRecommendationAgeDays, threshold: 30);
        AlertEvaluationContext ctx = EmptyContext();
        ctx.RecommendationRecords =
        [
            new RecommendationRecord
            {
                Status = RecommendationStatus.Deferred,
                PriorityScore = 85,
                // Updated 40 days ago — beyond the 30-day threshold
                LastUpdatedUtc = DateTime.UtcNow.AddDays(-40),
                Title = "Old Rec",
                RecommendationId = Guid.NewGuid()
            }
        ];

        IReadOnlyList<AlertRecord> result = _sut.Evaluate([rule], ctx);

        result.Should().ContainSingle();
        result[0].Category.Should().Be(AlertCategories.Recommendation);
    }

    [Fact]
    public void Evaluate_DeferredHighPriorityAge_LowPriorityScore_NoAlert()
    {
        AlertRule rule = MakeRule(AlertRuleType.DeferredHighPriorityRecommendationAgeDays, threshold: 10);
        AlertEvaluationContext ctx = EmptyContext();
        ctx.RecommendationRecords =
        [
            new RecommendationRecord
            {
                Status = RecommendationStatus.Deferred,
                PriorityScore = 50, // below the 80 floor
                LastUpdatedUtc = DateTime.UtcNow.AddDays(-30),
                Title = "Low Pri Rec",
                RecommendationId = Guid.NewGuid()
            }
        ];

        IReadOnlyList<AlertRecord> result = _sut.Evaluate([rule], ctx);

        result.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // RejectedSecurityRecommendation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_RejectedSecurityRecommendation_RejectedNonSecurity_NoAlert()
    {
        AlertRule rule = MakeRule(AlertRuleType.RejectedSecurityRecommendation, threshold: 0);
        AlertEvaluationContext ctx = EmptyContext();
        ctx.RecommendationRecords =
        [
            new RecommendationRecord
            {
                Status = RecommendationStatus.Rejected,
                Category = "Cost",
                Title = "Cost Rec",
                RecommendationId = Guid.NewGuid()
            }
        ];

        IReadOnlyList<AlertRecord> result = _sut.Evaluate([rule], ctx);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_RejectedSecurityRecommendation_RejectedSecurity_AlertFires()
    {
        AlertRule rule = MakeRule(AlertRuleType.RejectedSecurityRecommendation, threshold: 0);
        AlertEvaluationContext ctx = EmptyContext();
        ctx.RecommendationRecords =
        [
            new RecommendationRecord
            {
                Status = RecommendationStatus.Rejected,
                Category = AlertCategories.Security,
                Title = "MFA Rec",
                RecommendationId = Guid.NewGuid()
            }
        ];

        IReadOnlyList<AlertRecord> result = _sut.Evaluate([rule], ctx);

        result.Should().ContainSingle();
        result[0].Category.Should().Be(AlertCategories.Security);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AcceptanceRateDrop
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_AcceptanceRateDrop_NullProfile_NoAlert()
    {
        AlertRule rule = MakeRule(AlertRuleType.AcceptanceRateDrop, threshold: 50);
        AlertEvaluationContext ctx = EmptyContext();

        IReadOnlyList<AlertRecord> result = _sut.Evaluate([rule], ctx);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_AcceptanceRateDrop_AboveThreshold_NoAlert()
    {
        AlertRule rule = MakeRule(AlertRuleType.AcceptanceRateDrop, threshold: 50);
        AlertEvaluationContext ctx = EmptyContext();
        ctx.LearningProfile = new RecommendationLearningProfile
        {
            CategoryStats =
            [
                new RecommendationOutcomeStats { ProposedCount = 10, AcceptedCount = 8 }
            ]
        };

        IReadOnlyList<AlertRecord> result = _sut.Evaluate([rule], ctx);

        // 80 % acceptance > 50 % threshold → no alert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_AcceptanceRateDrop_BelowThreshold_AlertFires()
    {
        AlertRule rule = MakeRule(AlertRuleType.AcceptanceRateDrop, threshold: 50);
        AlertEvaluationContext ctx = EmptyContext();
        ctx.LearningProfile = new RecommendationLearningProfile
        {
            CategoryStats =
            [
                new RecommendationOutcomeStats { ProposedCount = 10, AcceptedCount = 3 }
            ]
        };

        IReadOnlyList<AlertRecord> result = _sut.Evaluate([rule], ctx);

        // 30 % acceptance <= 50 % threshold → alert fires
        result.Should().ContainSingle();
        result[0].Category.Should().Be(AlertCategories.Learning);
    }
}

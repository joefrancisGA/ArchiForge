using ArchLucid.Core.Comparison;
using ArchLucid.Decisioning.Advisory.Learning;
using ArchLucid.Decisioning.Advisory.Models;
using ArchLucid.Decisioning.Advisory.Workflow;
using ArchLucid.Decisioning.Governance.PolicyPacks;

namespace ArchLucid.Decisioning.Alerts;

/// <summary>
///     Inputs for simple and composite alert evaluation: scope, optional run comparison, advisory plan, and optional
///     preloaded merged policy content.
/// </summary>
/// <remarks>
///     Built by <see cref="AlertEvaluationContextFactory" /> (advisory scan) or
///     <c>ArchLucid.Persistence.Alerts.Simulation.AlertSimulationContextProvider</c> (what-if).
///     <see cref="IAlertEvaluator" /> and composite rule evaluators read metrics from the plan, comparison, and learning
///     profile; they do not load governance themselves.
/// </remarks>
public class AlertEvaluationContext
{
    /// <summary>Tenant for rule lookup and persistence.</summary>
    public Guid TenantId
    {
        get;
        set;
    }

    /// <summary>Workspace within the tenant.</summary>
    public Guid WorkspaceId
    {
        get;
        set;
    }

    /// <summary>Project within the workspace.</summary>
    public Guid ProjectId
    {
        get;
        set;
    }

    /// <summary>Authority or advisory run being evaluated; may be null for synthetic scenarios.</summary>
    public Guid? RunId
    {
        get;
        set;
    }

    /// <summary>Baseline run when a comparison-driven plan exists; drives delta-style rules.</summary>
    public Guid? ComparedToRunId
    {
        get;
        set;
    }

    /// <summary>Improvement plan (recommendations, costs, etc.) used by metric-based alert rules.</summary>
    public ImprovementPlan? ImprovementPlan
    {
        get;
        set;
    }

    /// <summary>Optional manifest comparison when the plan was generated with a baseline run.</summary>
    public ComparisonResult? ComparisonResult
    {
        get;
        set;
    }

    /// <summary>Historical recommendation rows for the run; used for aging / deferral style rules.</summary>
    public IReadOnlyList<RecommendationRecord> RecommendationRecords
    {
        get;
        set;
    } = [];

    /// <summary>Optional learning profile affecting advisory metrics surfaced to composite rules.</summary>
    public RecommendationLearningProfile? LearningProfile
    {
        get;
        set;
    }

    /// <summary>
    ///     When set (e.g. advisory scan), alert services reuse this merged document instead of calling
    ///     <see cref="IEffectiveGovernanceLoader.LoadEffectiveContentAsync" /> again.
    /// </summary>
    /// <remarks>
    ///     When <c>null</c>, persistence-layer alert services load effective content per evaluation via
    ///     <see cref="IEffectiveGovernanceLoader" />.
    /// </remarks>
    public PolicyPackContentDocument? EffectiveGovernanceContent
    {
        get;
        set;
    }
}

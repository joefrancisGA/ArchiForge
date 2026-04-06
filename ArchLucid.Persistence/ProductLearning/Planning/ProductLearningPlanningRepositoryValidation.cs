using ArchiForge.Contracts.ProductLearning;
using ArchiForge.Contracts.ProductLearning.Planning;

namespace ArchiForge.Persistence.ProductLearning.Planning;

internal static class ProductLearningPlanningRepositoryValidation
{
    internal const int MaxTake = 500;

    internal const int MaxActionSteps = 20;

    internal const int MaxTitleLength = 512;

    internal const int MaxThemeKeyLength = 256;

    internal const int MaxDerivationRuleVersionLength = 64;

    internal const int MaxArtifactHintLength = 512;

    internal static void EnsureScope(ProductLearningScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
    }

    internal static void EnsureTake(int take)
    {
        if (take < 1 || take > MaxTake)
        
            throw new ArgumentOutOfRangeException(nameof(take), take, "take must be between 1 and " + MaxTake + ".");
        
    }

    internal static string NormalizeThemeStatus(string? status)
    {
        string resolved = string.IsNullOrWhiteSpace(status)
            ? ProductLearningImprovementThemeStatusValues.Proposed
            : status;

        ValidateThemeStatus(resolved);

        return resolved;
    }

    internal static string NormalizePlanStatus(string? status)
    {
        string resolved = string.IsNullOrWhiteSpace(status)
            ? ProductLearningImprovementPlanStatusValues.Proposed
            : status;

        ValidatePlanStatus(resolved);

        return resolved;
    }

    internal static void EnsureTheme(ProductLearningImprovementThemeRecord theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        if (theme.TenantId == Guid.Empty || theme.WorkspaceId == Guid.Empty || theme.ProjectId == Guid.Empty)
        
            throw new ArgumentException("TenantId, WorkspaceId, and ProjectId are required on themes.", nameof(theme));
        

        if (string.IsNullOrWhiteSpace(theme.ThemeKey))
        
            throw new ArgumentException("ThemeKey is required.", nameof(theme));
        

        if (theme.ThemeKey.Length > MaxThemeKeyLength)
        
            throw new ArgumentException("ThemeKey exceeds max length.", nameof(theme));
        

        if (string.IsNullOrWhiteSpace(theme.Title))
        
            throw new ArgumentException("Title is required.", nameof(theme));
        

        if (theme.Title.Length > MaxTitleLength)
        
            throw new ArgumentException("Title exceeds max length.", nameof(theme));
        

        if (string.IsNullOrWhiteSpace(theme.Summary))
        
            throw new ArgumentException("Summary is required.", nameof(theme));
        

        if (string.IsNullOrWhiteSpace(theme.AffectedArtifactTypeOrWorkflowArea))
        
            throw new ArgumentException("AffectedArtifactTypeOrWorkflowArea is required.", nameof(theme));
        

        if (string.IsNullOrWhiteSpace(theme.SeverityBand))
        
            throw new ArgumentException("SeverityBand is required.", nameof(theme));
        

        if (string.IsNullOrWhiteSpace(theme.DerivationRuleVersion))
        
            throw new ArgumentException("DerivationRuleVersion is required.", nameof(theme));
        

        if (theme.DerivationRuleVersion.Length > MaxDerivationRuleVersionLength)
        
            throw new ArgumentException("DerivationRuleVersion exceeds max length.", nameof(theme));
        

        if (theme.EvidenceSignalCount < 0 || theme.DistinctRunCount < 0)
        
            throw new ArgumentException("Counts must be non-negative.", nameof(theme));
        

        if (!string.IsNullOrWhiteSpace(theme.Status))
        
            ValidateThemeStatus(theme.Status);
        
    }

    internal static void EnsurePlan(ProductLearningImprovementPlanRecord plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.TenantId == Guid.Empty || plan.WorkspaceId == Guid.Empty || plan.ProjectId == Guid.Empty)
        
            throw new ArgumentException("TenantId, WorkspaceId, and ProjectId are required on plans.", nameof(plan));
        

        if (plan.ThemeId == Guid.Empty)
        
            throw new ArgumentException("ThemeId is required.", nameof(plan));
        

        if (string.IsNullOrWhiteSpace(plan.Title))
        
            throw new ArgumentException("Title is required.", nameof(plan));
        

        if (plan.Title.Length > MaxTitleLength)
        
            throw new ArgumentException("Title exceeds max length.", nameof(plan));
        

        if (string.IsNullOrWhiteSpace(plan.Summary))
        
            throw new ArgumentException("Summary is required.", nameof(plan));
        

        if (!string.IsNullOrWhiteSpace(plan.Status))
        
            ValidatePlanStatus(plan.Status);
        

        EnsureActionSteps(plan.ActionSteps);
    }

    internal static void EnsureActionSteps(IReadOnlyList<ProductLearningImprovementPlanActionStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        if (steps.Count == 0)
        
            throw new ArgumentException("At least one action step is required.", nameof(steps));
        

        if (steps.Count > MaxActionSteps)
        
            throw new ArgumentException("Action steps exceed bounded maximum (" + MaxActionSteps + ").", nameof(steps));
        

        HashSet<int> ordinals = [];

        foreach (ProductLearningImprovementPlanActionStep step in steps)
        {
            ArgumentNullException.ThrowIfNull(step);

            if (step.Ordinal < 1)
            
                throw new ArgumentException("Each step.Ordinal must be >= 1.", nameof(steps));
            

            if (!ordinals.Add(step.Ordinal))
            
                throw new ArgumentException("Duplicate action Ordinal values are not allowed.", nameof(steps));
            

            if (string.IsNullOrWhiteSpace(step.ActionType))
            
                throw new ArgumentException("Each step requires ActionType.", nameof(steps));
            

            if (string.IsNullOrWhiteSpace(step.Description))
            
                throw new ArgumentException("Each step requires Description.", nameof(steps));
            
        }
    }

    internal static void EnsureRunLink(ProductLearningImprovementPlanRunLinkRecord link)
    {
        ArgumentNullException.ThrowIfNull(link);

        if (link.PlanId == Guid.Empty)
        
            throw new ArgumentException("PlanId is required.", nameof(link));
        

        if (string.IsNullOrWhiteSpace(link.ArchitectureRunId))
        
            throw new ArgumentException("ArchitectureRunId is required.", nameof(link));
        
    }

    internal static void EnsureSignalLink(ProductLearningImprovementPlanSignalLinkRecord link)
    {
        ArgumentNullException.ThrowIfNull(link);

        if (link.PlanId == Guid.Empty)
        
            throw new ArgumentException("PlanId is required.", nameof(link));
        

        if (link.SignalId == Guid.Empty)
        
            throw new ArgumentException("SignalId is required.", nameof(link));
        

        if (link.TriageStatusSnapshot is not null)
        
            ValidateTriageSnapshot(link.TriageStatusSnapshot);
        
    }

    internal static void EnsureArtifactLink(ProductLearningImprovementPlanArtifactLinkRecord link)
    {
        ArgumentNullException.ThrowIfNull(link);

        if (link.PlanId == Guid.Empty)
        
            throw new ArgumentException("PlanId is required.", nameof(link));
        

        bool authority = link.AuthorityBundleId is not null && link.AuthorityArtifactSortOrder is not null;
        bool authorityPartial =
            link.AuthorityBundleId is not null ^ link.AuthorityArtifactSortOrder is not null;

        if (authorityPartial)
        
            throw new ArgumentException(
                "AuthorityBundleId and AuthorityArtifactSortOrder must both be set or both omitted.",
                nameof(link));
        

        if (!authority && string.IsNullOrWhiteSpace(link.PilotArtifactHint))
        
            throw new ArgumentException(
                "Provide authority bundle coordinates or PilotArtifactHint.",
                nameof(link));
        

        if (link.PilotArtifactHint is not null && link.PilotArtifactHint.Length > MaxArtifactHintLength)
        
            throw new ArgumentException("PilotArtifactHint exceeds max length.", nameof(link));
        
    }

    private static void ValidateThemeStatus(string status)
    {
        if (status != ProductLearningImprovementThemeStatusValues.Proposed &&
            status != ProductLearningImprovementThemeStatusValues.Accepted &&
            status != ProductLearningImprovementThemeStatusValues.Superseded &&
            status != ProductLearningImprovementThemeStatusValues.Archived)
        
            throw new ArgumentException("Unknown theme status: " + status, nameof(status));
        
    }

    private static void ValidatePlanStatus(string status)
    {
        if (status != ProductLearningImprovementPlanStatusValues.Proposed &&
            status != ProductLearningImprovementPlanStatusValues.UnderReview &&
            status != ProductLearningImprovementPlanStatusValues.Approved &&
            status != ProductLearningImprovementPlanStatusValues.Rejected &&
            status != ProductLearningImprovementPlanStatusValues.Completed)
        
            throw new ArgumentException("Unknown plan status: " + status, nameof(status));
        
    }

    private static void ValidateTriageSnapshot(string snapshot)
    {
        if (snapshot != ProductLearningTriageStatusValues.Open &&
            snapshot != ProductLearningTriageStatusValues.Triaged &&
            snapshot != ProductLearningTriageStatusValues.Backlog &&
            snapshot != ProductLearningTriageStatusValues.Done &&
            snapshot != ProductLearningTriageStatusValues.WontFix)
        
            throw new ArgumentException("Unknown triage snapshot: " + snapshot, nameof(snapshot));
        
    }
}

using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Manifest.Sections;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Queries;

namespace ArchLucid.Persistence.Coordination.Compare;

/// <summary>
///     <see cref="IAuthorityCompareService" /> implementation: manifest diff across requirements, topology, security,
///     cost, issues, assumptions, warnings, and decisions.
/// </summary>
/// <remarks>
///     Run comparison uses <see cref="IAuthorityQueryService.GetRunSummaryAsync" />; manifest comparison uses
///     <see cref="IGoldenManifestRepository.GetByIdAsync" />.
///     String-set diffs use case-insensitive equality; run-level <see cref="AddRunDiff" /> uses ordinal comparison.
/// </remarks>
public sealed class AuthorityCompareService(
    IGoldenManifestRepository manifestRepository,
    IAuthorityQueryService queryService)
    : IAuthorityCompareService
{
    /// <inheritdoc />
    public async Task<ManifestComparisonResult?> CompareManifestsAsync(
        ScopeContext scope,
        Guid leftManifestId,
        Guid rightManifestId,
        CancellationToken ct)
    {
        GoldenManifest? left = await manifestRepository.GetByIdAsync(scope, leftManifestId, ct);
        GoldenManifest? right = await manifestRepository.GetByIdAsync(scope, rightManifestId, ct);

        if (left is null || right is null)
            return null;

        if (left.TenantId != right.TenantId ||
            left.WorkspaceId != right.WorkspaceId ||
            left.ProjectId != right.ProjectId)
            throw new InvalidOperationException(
                $"Cannot compare manifests across different scopes. " +
                $"Left scope: {left.TenantId}/{left.WorkspaceId}/{left.ProjectId}, " +
                $"Right scope: {right.TenantId}/{right.WorkspaceId}/{right.ProjectId}.");

        ManifestComparisonResult result = new()
        {
            LeftManifestId = left.ManifestId,
            RightManifestId = right.ManifestId,
            LeftManifestHash = left.ManifestHash,
            RightManifestHash = right.ManifestHash
        };

        CompareRequirements(left, right, result);
        CompareTopology(left, right, result);
        CompareSecurity(left, right, result);
        CompareCost(left, right, result);
        CompareIssues(left, right, result);
        CompareAssumptions(left, right, result);
        CompareWarnings(left, right, result);
        CompareDecisions(left, right, result);

        return result;
    }

    /// <inheritdoc />
    public async Task<RunComparisonResult?> CompareRunsAsync(
        ScopeContext scope,
        Guid leftRunId,
        Guid rightRunId,
        CancellationToken ct)
    {
        RunSummaryDto? leftRun = await queryService.GetRunSummaryAsync(scope, leftRunId, ct);
        RunSummaryDto? rightRun = await queryService.GetRunSummaryAsync(scope, rightRunId, ct);

        if (leftRun is null || rightRun is null)
            return null;

        RunComparisonResult result = new()
        {
            LeftRunId = leftRunId, RightRunId = rightRunId, LeftRun = leftRun, RightRun = rightRun
        };

        AddRunDiff(result.RunLevelDiffs, "Run", "ProjectId", leftRun.ProjectId, rightRun.ProjectId);
        AddRunDiff(result.RunLevelDiffs, "Run", "Description", leftRun.Description, rightRun.Description);
        AddRunDiff(
            result.RunLevelDiffs,
            "Run",
            "GoldenManifestId",
            leftRun.GoldenManifestId?.ToString(),
            rightRun.GoldenManifestId?.ToString());

        if (leftRun.GoldenManifestId.HasValue && rightRun.GoldenManifestId.HasValue)

            result.ManifestComparison = await CompareManifestsAsync(
                scope,
                leftRun.GoldenManifestId.Value,
                rightRun.GoldenManifestId.Value,
                ct);


        return result;
    }

    /// <inheritdoc />
    public void AddRunDiff(
        IList<DiffItem> diffs,
        string section,
        string key,
        string? beforeValue,
        string? afterValue)
    {
        if (!string.Equals(beforeValue, afterValue, StringComparison.Ordinal))

            diffs.Add(new DiffItem
            {
                Section = section,
                Key = key,
                DiffKind = DiffKind.Changed,
                BeforeValue = beforeValue,
                AfterValue = afterValue
            });
    }

    private static void CompareRequirements(
        GoldenManifest left,
        GoldenManifest right,
        ManifestComparisonResult result)
    {
        Dictionary<string, RequirementCoverageItem> leftMap = ToFirstWins(left.Requirements.Covered,
            x => x.RequirementName);
        Dictionary<string, RequirementCoverageItem> rightMap =
            ToFirstWins(right.Requirements.Covered, x => x.RequirementName);

        CompareKeyedSets(
            result,
            "Requirements",
            leftMap,
            rightMap,
            l => l.CoverageStatus,
            r => r.CoverageStatus,
            l => l.RequirementText,
            r => r.RequirementText);
    }

    private static void CompareTopology(
        GoldenManifest left,
        GoldenManifest right,
        ManifestComparisonResult result)
    {
        CompareStringLists(result, "Topology.Gaps", left.Topology.Gaps, right.Topology.Gaps);
        CompareStringLists(result, "Topology.Resources", left.Topology.Resources, right.Topology.Resources);
        CompareStringLists(result, "Topology.Patterns", left.Topology.SelectedPatterns,
            right.Topology.SelectedPatterns);
    }

    private static void CompareSecurity(
        GoldenManifest left,
        GoldenManifest right,
        ManifestComparisonResult result)
    {
        Dictionary<string, SecurityPostureItem> leftMap = ToFirstWins(left.Security.Controls, x => x.ControlName);
        Dictionary<string, SecurityPostureItem> rightMap = ToFirstWins(right.Security.Controls, x => x.ControlName);

        CompareKeyedSets(
            result,
            "Security.Controls",
            leftMap,
            rightMap,
            l => l.Status,
            r => r.Status,
            l => l.Impact,
            r => r.Impact);

        CompareStringLists(result, "Security.Gaps", left.Security.Gaps, right.Security.Gaps);
    }

    private static void CompareCost(
        GoldenManifest left,
        GoldenManifest right,
        ManifestComparisonResult result)
    {
        AddDiff(
            result,
            "Cost",
            "MaxMonthlyCost",
            left.Cost.MaxMonthlyCost?.ToString(),
            right.Cost.MaxMonthlyCost?.ToString());

        CompareStringLists(result, "Cost.Risks", left.Cost.CostRisks, right.Cost.CostRisks);
        CompareStringLists(result, "Cost.Notes", left.Cost.Notes, right.Cost.Notes);
    }

    private static void CompareIssues(
        GoldenManifest left,
        GoldenManifest right,
        ManifestComparisonResult result)
    {
        Dictionary<string, ManifestIssue> leftMap = ToFirstWins(left.UnresolvedIssues.Items, x => x.Title);
        Dictionary<string, ManifestIssue> rightMap = ToFirstWins(right.UnresolvedIssues.Items, x => x.Title);

        CompareKeyedSets(
            result,
            "Issues",
            leftMap,
            rightMap,
            l => l.Severity,
            r => r.Severity,
            l => l.Description,
            r => r.Description);
    }

    private static void CompareAssumptions(
        GoldenManifest left,
        GoldenManifest right,
        ManifestComparisonResult result)
    {
        CompareStringLists(result, "Assumptions", left.Assumptions, right.Assumptions);
    }

    private static void CompareWarnings(
        GoldenManifest left,
        GoldenManifest right,
        ManifestComparisonResult result)
    {
        CompareStringLists(result, "Warnings", left.Warnings, right.Warnings);
    }

    private static void CompareDecisions(
        GoldenManifest left,
        GoldenManifest right,
        ManifestComparisonResult result)
    {
        Dictionary<string, ResolvedArchitectureDecision> leftMap = ToFirstWins(left.Decisions,
            x => $"{x.Category}:{x.Title}");
        Dictionary<string, ResolvedArchitectureDecision> rightMap = ToFirstWins(right.Decisions,
            x => $"{x.Category}:{x.Title}");

        CompareKeyedSets(
            result,
            "Decisions",
            leftMap,
            rightMap,
            l => l.SelectedOption,
            r => r.SelectedOption,
            l => l.Rationale,
            r => r.Rationale);
    }

    private static void CompareStringLists(
        ManifestComparisonResult result,
        string section,
        IEnumerable<string>? left,
        IEnumerable<string>? right)
    {
        HashSet<string> leftSet = new(left ?? [], StringComparer.OrdinalIgnoreCase);
        HashSet<string> rightSet = new(right ?? [], StringComparer.OrdinalIgnoreCase);

        foreach (string item in leftSet.Except(rightSet, StringComparer.OrdinalIgnoreCase))

            result.Diffs.Add(new DiffItem
            {
                Section = section, Key = item, DiffKind = DiffKind.Removed, BeforeValue = item
            });


        foreach (string item in rightSet.Except(leftSet, StringComparer.OrdinalIgnoreCase))

            result.Diffs.Add(new DiffItem
            {
                Section = section, Key = item, DiffKind = DiffKind.Added, AfterValue = item
            });
    }

    private static void CompareKeyedSets<T>(
        ManifestComparisonResult result,
        string section,
        IDictionary<string, T> left,
        IDictionary<string, T> right,
        Func<T, string?> primaryLeft,
        Func<T, string?> primaryRight,
        Func<T, string?> notesLeft,
        Func<T, string?> notesRight)
    {
        foreach (string key in left.Keys.Except(right.Keys, StringComparer.OrdinalIgnoreCase))

            result.Diffs.Add(new DiffItem
            {
                Section = section,
                Key = key,
                DiffKind = DiffKind.Removed,
                BeforeValue = primaryLeft(left[key]),
                Notes = notesLeft(left[key])
            });


        foreach (string key in right.Keys.Except(left.Keys, StringComparer.OrdinalIgnoreCase))

            result.Diffs.Add(new DiffItem
            {
                Section = section,
                Key = key,
                DiffKind = DiffKind.Added,
                AfterValue = primaryRight(right[key]),
                Notes = notesRight(right[key])
            });


        foreach (string key in left.Keys.Intersect(right.Keys, StringComparer.OrdinalIgnoreCase))
        {
            string? leftValue = primaryLeft(left[key]);
            string? rightValue = primaryRight(right[key]);
            string? leftNotes = notesLeft(left[key]);
            string? rightNotes = notesRight(right[key]);

            if (!string.Equals(leftValue, rightValue, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(leftNotes, rightNotes, StringComparison.Ordinal))

                result.Diffs.Add(new DiffItem
                {
                    Section = section,
                    Key = key,
                    DiffKind = DiffKind.Changed,
                    BeforeValue = leftValue,
                    AfterValue = rightValue,
                    Notes = $"Before: {leftNotes} | After: {rightNotes}"
                });
        }
    }

    private static void AddDiff(
        ManifestComparisonResult result,
        string section,
        string key,
        string? beforeValue,
        string? afterValue)
    {
        if (!string.Equals(beforeValue, afterValue, StringComparison.Ordinal))

            result.Diffs.Add(new DiffItem
            {
                Section = section,
                Key = key,
                DiffKind = DiffKind.Changed,
                BeforeValue = beforeValue,
                AfterValue = afterValue
            });
    }

    /// <summary>
    ///     Builds a case-insensitive dictionary from <paramref name="source" />, taking the first element
    ///     when duplicate keys are present. Prevents <see cref="ArgumentException" /> on bad persisted data.
    /// </summary>
    private static Dictionary<string, T> ToFirstWins<T>(
        IEnumerable<T> source,
        Func<T, string> keySelector)
    {
        return source
            .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }
}

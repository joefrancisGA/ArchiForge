using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Queries;

namespace ArchiForge.Persistence.Compare;

/// <summary>
/// <see cref="IAuthorityCompareService"/> implementation: manifest diff across requirements, topology, security, cost, issues, assumptions, warnings, and decisions.
/// </summary>
/// <remarks>
/// Run comparison uses <see cref="IAuthorityQueryService.GetRunSummaryAsync"/>; manifest comparison uses <see cref="IGoldenManifestRepository.GetByIdAsync"/>.
/// String-set diffs use case-insensitive equality; run-level <see cref="AddRunDiff"/> uses ordinal comparison.
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
        var left = await manifestRepository.GetByIdAsync(scope, leftManifestId, ct);
        var right = await manifestRepository.GetByIdAsync(scope, rightManifestId, ct);

        if (left is null || right is null)
            return null;

        if (left.TenantId != right.TenantId ||
            left.WorkspaceId != right.WorkspaceId ||
            left.ProjectId != right.ProjectId)
            return null;

        var result = new ManifestComparisonResult
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

    public async Task<RunComparisonResult?> CompareRunsAsync(
        ScopeContext scope,
        Guid leftRunId,
        Guid rightRunId,
        CancellationToken ct)
    {
        var leftRun = await queryService.GetRunSummaryAsync(scope, leftRunId, ct);
        var rightRun = await queryService.GetRunSummaryAsync(scope, rightRunId, ct);

        if (leftRun is null || rightRun is null)
            return null;

        var result = new RunComparisonResult
        {
            LeftRunId = leftRunId,
            RightRunId = rightRunId,
            LeftRun = leftRun,
            RightRun = rightRun
        };

        AddRunDiff(result.RunLevelDiffs, "Run", "ProjectId", leftRun.ProjectId, rightRun.ProjectId);
        AddRunDiff(result.RunLevelDiffs, "Run", "Description", leftRun.Description, rightRun.Description);

        if (leftRun.GoldenManifestId.HasValue && rightRun.GoldenManifestId.HasValue)
        {
            result.ManifestComparison = await CompareManifestsAsync(
                scope,
                leftRun.GoldenManifestId.Value,
                rightRun.GoldenManifestId.Value,
                ct);
        }

        return result;
    }

    private static void CompareRequirements(
        GoldenManifest left,
        GoldenManifest right,
        ManifestComparisonResult result)
    {
        var leftMap = left.Requirements.Covered
            .ToDictionary(x => x.RequirementName, StringComparer.OrdinalIgnoreCase);

        var rightMap = right.Requirements.Covered
            .ToDictionary(x => x.RequirementName, StringComparer.OrdinalIgnoreCase);

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
        CompareStringLists(result, "Topology.Patterns", left.Topology.SelectedPatterns, right.Topology.SelectedPatterns);
    }

    private static void CompareSecurity(
        GoldenManifest left,
        GoldenManifest right,
        ManifestComparisonResult result)
    {
        var leftMap = left.Security.Controls
            .ToDictionary(x => x.ControlName, StringComparer.OrdinalIgnoreCase);

        var rightMap = right.Security.Controls
            .ToDictionary(x => x.ControlName, StringComparer.OrdinalIgnoreCase);

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
        var leftMap = left.UnresolvedIssues.Items
            .ToDictionary(x => x.Title, StringComparer.OrdinalIgnoreCase);

        var rightMap = right.UnresolvedIssues.Items
            .ToDictionary(x => x.Title, StringComparer.OrdinalIgnoreCase);

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
        var leftMap = left.Decisions
            .ToDictionary(x => $"{x.Category}:{x.Title}", StringComparer.OrdinalIgnoreCase);

        var rightMap = right.Decisions
            .ToDictionary(x => $"{x.Category}:{x.Title}", StringComparer.OrdinalIgnoreCase);

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
        var leftSet = new HashSet<string>(left ?? [], StringComparer.OrdinalIgnoreCase);
        var rightSet = new HashSet<string>(right ?? [], StringComparer.OrdinalIgnoreCase);

        foreach (var item in leftSet.Except(rightSet, StringComparer.OrdinalIgnoreCase))
        {
            result.Diffs.Add(new DiffItem
            {
                Section = section,
                Key = item,
                DiffKind = DiffKind.Removed,
                BeforeValue = item
            });
        }

        foreach (var item in rightSet.Except(leftSet, StringComparer.OrdinalIgnoreCase))
        {
            result.Diffs.Add(new DiffItem
            {
                Section = section,
                Key = item,
                DiffKind = DiffKind.Added,
                AfterValue = item
            });
        }
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
        foreach (var key in left.Keys.Except(right.Keys, StringComparer.OrdinalIgnoreCase))
        {
            result.Diffs.Add(new DiffItem
            {
                Section = section,
                Key = key,
                DiffKind = DiffKind.Removed,
                BeforeValue = primaryLeft(left[key]),
                Notes = notesLeft(left[key])
            });
        }

        foreach (var key in right.Keys.Except(left.Keys, StringComparer.OrdinalIgnoreCase))
        {
            result.Diffs.Add(new DiffItem
            {
                Section = section,
                Key = key,
                DiffKind = DiffKind.Added,
                AfterValue = primaryRight(right[key]),
                Notes = notesRight(right[key])
            });
        }

        foreach (var key in left.Keys.Intersect(right.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var leftValue = primaryLeft(left[key]);
            var rightValue = primaryRight(right[key]);
            var leftNotes = notesLeft(left[key]);
            var rightNotes = notesRight(right[key]);

            if (!string.Equals(leftValue, rightValue, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(leftNotes, rightNotes, StringComparison.Ordinal))
            {
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
    }

    private static void AddDiff(
        ManifestComparisonResult result,
        string section,
        string key,
        string? beforeValue,
        string? afterValue)
    {
        if (!string.Equals(beforeValue, afterValue, StringComparison.Ordinal))
        {
            result.Diffs.Add(new DiffItem
            {
                Section = section,
                Key = key,
                DiffKind = DiffKind.Changed,
                BeforeValue = beforeValue,
                AfterValue = afterValue
            });
        }
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
        {
            diffs.Add(new DiffItem
            {
                Section = section,
                Key = key,
                DiffKind = DiffKind.Changed,
                BeforeValue = beforeValue,
                AfterValue = afterValue
            });
        }
    }
}

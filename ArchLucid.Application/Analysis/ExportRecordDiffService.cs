using ArchLucid.Contracts.Metadata;

namespace ArchLucid.Application.Analysis;

/// <summary>
///     Compares two <see cref="RunExportRecord" /> instances and produces an <see cref="ExportRecordDiffResult" />
///     describing changes to top-level export fields and the embedded analysis request options.
/// </summary>
public sealed class ExportRecordDiffService : IExportRecordDiffService
{
    /// <summary>
    ///     Compares <paramref name="left" /> (baseline) against <paramref name="right" /> (candidate) and returns
    ///     a diff of top-level export metadata and any changes to the analysis request options.
    /// </summary>
    public ExportRecordDiffResult Compare(
        RunExportRecord left,
        RunExportRecord right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        ExportRecordDiffResult result = new()
        {
            LeftExportRecordId = left.ExportRecordId,
            RightExportRecordId = right.ExportRecordId,
            LeftRunId = left.RunId,
            RightRunId = right.RunId
        };

        CompareTopLevel(left, right, result);

        PersistedAnalysisExportRequest? leftRequest = AnalysisExportRequestRehydrator.Rehydrate(left);
        PersistedAnalysisExportRequest? rightRequest = AnalysisExportRequestRehydrator.Rehydrate(right);

        result.RequestDiff = CompareRequests(leftRequest, rightRequest);

        if (!string.Equals(left.RunId, right.RunId, StringComparison.OrdinalIgnoreCase))

            result.Warnings.Add("The compared export records belong to different runs.");

        if (!string.Equals(left.ExportType, right.ExportType, StringComparison.OrdinalIgnoreCase))

            result.Warnings.Add("The compared export records use different export types.");

        if (leftRequest is null || rightRequest is null)

            result.Warnings.Add("One or both export records did not contain a persisted analysis request.");

        return result;
    }

    private static void CompareTopLevel(
        RunExportRecord left,
        RunExportRecord right,
        ExportRecordDiffResult result)
    {
        AddIfChanged(result.ChangedTopLevelFields, "ExportType", left.ExportType, right.ExportType);
        AddIfChanged(result.ChangedTopLevelFields, "Format", left.Format, right.Format);
        AddIfChanged(result.ChangedTopLevelFields, "FileName", left.FileName, right.FileName);
        AddIfChanged(result.ChangedTopLevelFields, "TemplateProfile", left.TemplateProfile, right.TemplateProfile);
        AddIfChanged(result.ChangedTopLevelFields, "TemplateProfileDisplayName", left.TemplateProfileDisplayName,
            right.TemplateProfileDisplayName);
        AddIfChanged(result.ChangedTopLevelFields, "WasAutoSelected", left.WasAutoSelected, right.WasAutoSelected);
        AddIfChanged(result.ChangedTopLevelFields, "ResolutionReason", left.ResolutionReason, right.ResolutionReason);
        AddIfChanged(result.ChangedTopLevelFields, "ManifestVersion", left.ManifestVersion, right.ManifestVersion);
        AddIfChanged(result.ChangedTopLevelFields, "IncludedEvidence", left.IncludedEvidence, right.IncludedEvidence);
        AddIfChanged(result.ChangedTopLevelFields, "IncludedExecutionTraces", left.IncludedExecutionTraces,
            right.IncludedExecutionTraces);
        AddIfChanged(result.ChangedTopLevelFields, "IncludedManifest", left.IncludedManifest, right.IncludedManifest);
        AddIfChanged(result.ChangedTopLevelFields, "IncludedDiagram", left.IncludedDiagram, right.IncludedDiagram);
        AddIfChanged(result.ChangedTopLevelFields, "IncludedSummary", left.IncludedSummary, right.IncludedSummary);
        AddIfChanged(result.ChangedTopLevelFields, "IncludedDeterminismCheck", left.IncludedDeterminismCheck,
            right.IncludedDeterminismCheck);
        AddIfChanged(result.ChangedTopLevelFields, "DeterminismIterations", left.DeterminismIterations,
            right.DeterminismIterations);
        AddIfChanged(result.ChangedTopLevelFields, "IncludedManifestCompare", left.IncludedManifestCompare,
            right.IncludedManifestCompare);
        AddIfChanged(result.ChangedTopLevelFields, "CompareManifestVersion", left.CompareManifestVersion,
            right.CompareManifestVersion);
        AddIfChanged(result.ChangedTopLevelFields, "IncludedAgentResultCompare", left.IncludedAgentResultCompare,
            right.IncludedAgentResultCompare);
        AddIfChanged(result.ChangedTopLevelFields, "CompareRunId", left.CompareRunId, right.CompareRunId);
    }

    private static ExportRecordRequestDiff CompareRequests(
        PersistedAnalysisExportRequest? left,
        PersistedAnalysisExportRequest? right)
    {
        ExportRecordRequestDiff diff = new() { LeftRequest = left, RightRequest = right };

        if (left is null || right is null)
            return diff;

        AddIfChanged(diff.ChangedValues, "TemplateProfile", left.TemplateProfile, right.TemplateProfile);
        AddIfChanged(diff.ChangedValues, "Audience", left.Audience, right.Audience);

        AddIfChanged(diff.ChangedFlags, "ExternalDelivery", left.ExternalDelivery, right.ExternalDelivery);
        AddIfChanged(diff.ChangedFlags, "ExecutiveFriendly", left.ExecutiveFriendly, right.ExecutiveFriendly);
        AddIfChanged(diff.ChangedFlags, "RegulatedEnvironment", left.RegulatedEnvironment, right.RegulatedEnvironment);
        AddIfChanged(diff.ChangedFlags, "NeedDetailedEvidence", left.NeedDetailedEvidence, right.NeedDetailedEvidence);
        AddIfChanged(diff.ChangedFlags, "NeedExecutionTraces", left.NeedExecutionTraces, right.NeedExecutionTraces);
        AddIfChanged(diff.ChangedFlags, "NeedDeterminismOrCompareAppendices", left.NeedDeterminismOrCompareAppendices,
            right.NeedDeterminismOrCompareAppendices);

        AddIfChanged(diff.ChangedFlags, "IncludeEvidence", left.IncludeEvidence, right.IncludeEvidence);
        AddIfChanged(diff.ChangedFlags, "IncludeExecutionTraces", left.IncludeExecutionTraces,
            right.IncludeExecutionTraces);
        AddIfChanged(diff.ChangedFlags, "IncludeManifest", left.IncludeManifest, right.IncludeManifest);
        AddIfChanged(diff.ChangedFlags, "IncludeDiagram", left.IncludeDiagram, right.IncludeDiagram);
        AddIfChanged(diff.ChangedFlags, "IncludeSummary", left.IncludeSummary, right.IncludeSummary);
        AddIfChanged(diff.ChangedFlags, "IncludeDeterminismCheck", left.IncludeDeterminismCheck,
            right.IncludeDeterminismCheck);
        AddIfChanged(diff.ChangedFlags, "IncludeManifestCompare", left.IncludeManifestCompare,
            right.IncludeManifestCompare);
        AddIfChanged(diff.ChangedFlags, "IncludeAgentResultCompare", left.IncludeAgentResultCompare,
            right.IncludeAgentResultCompare);

        AddIfChanged(diff.ChangedValues, "DeterminismIterations", left.DeterminismIterations,
            right.DeterminismIterations);
        AddIfChanged(diff.ChangedValues, "CompareManifestVersion", left.CompareManifestVersion,
            right.CompareManifestVersion);
        AddIfChanged(diff.ChangedValues, "CompareRunId", left.CompareRunId, right.CompareRunId);

        return diff;
    }

    private static void AddIfChanged<T>(
        List<string> target,
        string fieldName,
        T left,
        T right)
    {
        if (!AreEqual(left, right))

            target.Add(fieldName);
    }

    private static bool AreEqual<T>(T left, T right)
    {
        if (left is string ls && right is string rs)
            return string.Equals(ls, rs, StringComparison.OrdinalIgnoreCase);

        return EqualityComparer<T>.Default.Equals(left, right);
    }
}

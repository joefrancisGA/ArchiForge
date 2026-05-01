using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Application.Runs.Mapping;

/// <summary>
///     Maps authority <see cref="RunRecord" /> + task ids into the API contract <see cref="ArchitectureRun" />.
/// </summary>
public static class RunRecordToArchitectureRunMapper
{
    /// <summary>
    ///     Builds <see cref="ArchitectureRun" /> from <paramref name="record" /> and ordered <paramref name="taskIds" />.
    /// </summary>
    public static ArchitectureRun ToArchitectureRun(RunRecord record, IReadOnlyList<string> taskIds)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(taskIds);

        ArchitectureRunStatus status = ParseStatus(record.LegacyRunStatus);

        return new ArchitectureRun
        {
            RunId = record.RunId.ToString("N"),
            RequestId = record.ArchitectureRequestId ?? string.Empty,
            Status = status,
            CreatedUtc = record.CreatedUtc,
            CompletedUtc = record.CompletedUtc,
            CurrentManifestVersion = record.CurrentManifestVersion,
            ContextSnapshotId = record.ContextSnapshotId?.ToString("N"),
            GraphSnapshotId = record.GraphSnapshotId,
            FindingsSnapshotId = record.FindingsSnapshotId,
            GoldenManifestId = record.GoldenManifestId,
            DecisionTraceId = record.DecisionTraceId,
            ArtifactBundleId = record.ArtifactBundleId,
            OtelTraceId = record.OtelTraceId,
            TaskIds = [.. taskIds],
            RealModeFellBackToSimulator = record.RealModeFellBackToSimulator,
            PilotAoaiDeploymentSnapshot = record.PilotAoaiDeploymentSnapshot
        };
    }

    private static ArchitectureRunStatus ParseStatus(string? legacyRunStatus)
    {
        if (string.IsNullOrWhiteSpace(legacyRunStatus))
            return ArchitectureRunStatus.Created;

        if (!Enum.TryParse(legacyRunStatus, true, out ArchitectureRunStatus parsed))

            throw new InvalidOperationException(
                $"Unrecognised ArchitectureRunStatus '{legacyRunStatus}'. " +
                "The authority row may have been written by a newer version of the application.");

        return parsed;
    }
}

using System.Diagnostics;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Persistence.Conversation;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using Microsoft.Extensions.Logging;

using Serilog.Context;

namespace ArchLucid.Persistence.Archival;

/// <inheritdoc cref="IDataArchivalCoordinator" />
public sealed class DataArchivalCoordinator(
    IRunRepository runRepository,
    IArchitectureDigestRepository digestRepository,
    IConversationThreadRepository conversationThreadRepository,
    ILogger<DataArchivalCoordinator> logger) : IDataArchivalCoordinator
{
    private readonly IConversationThreadRepository _conversationThreadRepository =
        conversationThreadRepository ?? throw new ArgumentNullException(nameof(conversationThreadRepository));

    private readonly IArchitectureDigestRepository _digestRepository =
        digestRepository ?? throw new ArgumentNullException(nameof(digestRepository));

    private readonly ILogger<DataArchivalCoordinator> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IRunRepository _runRepository =
        runRepository ?? throw new ArgumentNullException(nameof(runRepository));

    /// <inheritdoc />
    public async Task RunOnceAsync(DataArchivalOptions options, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(options);

        using Activity? activity = ArchLucidInstrumentation.DataArchival.StartActivity("DataArchival.RunOnce");
        string correlationId = FormattableString.Invariant($"data-archival:{DateTime.UtcNow:yyyyMMddHHmmss}");
        activity?.SetTag(ActivityCorrelation.LogicalCorrelationIdTag, correlationId);

        using IDisposable _ = LogContext.PushProperty("CorrelationId", correlationId);

        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (options.RunsRetentionDays > 0)
        {
            DateTimeOffset cutoff = now.AddDays(-options.RunsRetentionDays);
            RunArchiveBatchResult archivedRuns = await _runRepository.ArchiveRunsCreatedBeforeAsync(cutoff, ct);
            _logger.LogInformation(
                "Data archival: archived {Count} runs created before {Cutoff:O}.",
                archivedRuns.UpdatedCount,
                cutoff);

            if (archivedRuns.UpdatedCount > 0)
            {
                RunArchiveChildCascadeCounts c = archivedRuns.ChildCascade;
                _logger.LogInformation(
                    "Data archival: child ArchivedUtc cascade counts — GoldenManifests={Golden}, FindingsSnapshots={Findings}, " +
                    "ContextSnapshots={Context}, GraphSnapshots={Graph}, DecisioningTraces={Decisioning}, " +
                    "ArtifactBundles={Artifacts}, AgentExecutionTraces={Traces}, ComparisonRecords={Comparisons}.",
                    c.GoldenManifests,
                    c.FindingsSnapshots,
                    c.ContextSnapshots,
                    c.GraphSnapshots,
                    c.DecisioningTraces,
                    c.ArtifactBundles,
                    c.AgentExecutionTraces,
                    c.ComparisonRecords);
            }
        }

        if (options.DigestsRetentionDays > 0)
        {
            DateTimeOffset cutoff = now.AddDays(-options.DigestsRetentionDays);
            int n = await _digestRepository.ArchiveDigestsGeneratedBeforeAsync(cutoff, ct);
            _logger.LogInformation("Data archival: archived {Count} digests generated before {Cutoff:O}.", n, cutoff);
        }

        if (options.ConversationsRetentionDays > 0)
        {
            DateTimeOffset cutoff = now.AddDays(-options.ConversationsRetentionDays);
            int n = await _conversationThreadRepository.ArchiveThreadsLastUpdatedBeforeAsync(cutoff, ct);
            _logger.LogInformation(
                "Data archival: archived {Count} conversation threads last updated before {Cutoff:O}.",
                n,
                cutoff);
        }
    }
}

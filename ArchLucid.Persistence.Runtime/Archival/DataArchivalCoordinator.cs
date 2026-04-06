using ArchiForge.Persistence.Advisory;
using ArchiForge.Persistence.Conversation;
using ArchiForge.Persistence.Interfaces;

using Microsoft.Extensions.Logging;

namespace ArchiForge.Persistence.Archival;

/// <inheritdoc cref="IDataArchivalCoordinator" />
public sealed class DataArchivalCoordinator(
    IRunRepository runRepository,
    IArchitectureDigestRepository digestRepository,
    IConversationThreadRepository conversationThreadRepository,
    ILogger<DataArchivalCoordinator> logger) : IDataArchivalCoordinator
{
    private readonly IRunRepository _runRepository =
        runRepository ?? throw new ArgumentNullException(nameof(runRepository));

    private readonly IArchitectureDigestRepository _digestRepository =
        digestRepository ?? throw new ArgumentNullException(nameof(digestRepository));

    private readonly IConversationThreadRepository _conversationThreadRepository =
        conversationThreadRepository ?? throw new ArgumentNullException(nameof(conversationThreadRepository));

    private readonly ILogger<DataArchivalCoordinator> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task RunOnceAsync(DataArchivalOptions options, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(options);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (options.RunsRetentionDays > 0)
        {
            DateTimeOffset cutoff = now.AddDays(-options.RunsRetentionDays);
            int n = await _runRepository.ArchiveRunsCreatedBeforeAsync(cutoff, ct);
            _logger.LogInformation("Data archival: archived {Count} runs created before {Cutoff:O}.", n, cutoff);
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

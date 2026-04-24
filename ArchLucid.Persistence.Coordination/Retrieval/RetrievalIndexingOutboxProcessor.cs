using System.Diagnostics;

using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.Queries;
using ArchLucid.Provenance;
using ArchLucid.Retrieval.Indexing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog.Context;

namespace ArchLucid.Persistence.Coordination.Retrieval;

/// <inheritdoc cref="IRetrievalIndexingOutboxProcessor" />
public sealed class RetrievalIndexingOutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<RetrievalIndexingOutboxProcessor> logger) : IRetrievalIndexingOutboxProcessor
{
    private readonly ILogger<RetrievalIndexingOutboxProcessor> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    /// <inheritdoc />
    public async Task ProcessPendingBatchAsync(CancellationToken ct)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IRetrievalIndexingOutboxRepository outbox =
            scope.ServiceProvider.GetRequiredService<IRetrievalIndexingOutboxRepository>();
        IAuthorityQueryService query = scope.ServiceProvider.GetRequiredService<IAuthorityQueryService>();
        IRetrievalRunCompletionIndexer indexer =
            scope.ServiceProvider.GetRequiredService<IRetrievalRunCompletionIndexer>();
        IProvenanceBuilder provenanceBuilder = scope.ServiceProvider.GetRequiredService<IProvenanceBuilder>();

        IReadOnlyList<RetrievalIndexingOutboxEntry> batch = await outbox.DequeuePendingAsync(25, ct);

        foreach (RetrievalIndexingOutboxEntry entry in batch)
        {
            using Activity? activity = ArchLucidInstrumentation.RetrievalIndexingOutbox.StartActivity(
                "RetrievalIndexingOutbox.ProcessEntry");
            string correlationId = FormattableString.Invariant($"retrieval-outbox:{entry.OutboxId:D}");
            activity?.SetTag(ActivityCorrelation.LogicalCorrelationIdTag, correlationId);
            activity?.SetTag("archlucid.run_id", entry.RunId.ToString("D"));
            activity?.SetTag("archlucid.outbox_id", entry.OutboxId.ToString("D"));

            using IDisposable _ = LogContext.PushProperty("CorrelationId", correlationId);

            try
            {
                ScopeContext scopeContext = new()
                {
                    TenantId = entry.TenantId, WorkspaceId = entry.WorkspaceId, ProjectId = entry.ProjectId
                };

                RunDetailDto? detail = await query.GetRunDetailAsync(scopeContext, entry.RunId, ct);

                if (detail?.GoldenManifest is null ||
                    detail.GraphSnapshot is null ||
                    detail.FindingsSnapshot is null ||
                    detail.AuthorityTrace is null)
                {
                    _logger.LogWarning(
                        "Skipping retrieval indexing for run {RunId}: incomplete run detail.",
                        entry.RunId);
                    await outbox.MarkProcessedAsync(entry.OutboxId, ct);
                    continue;
                }

                GoldenManifest manifest = detail.GoldenManifest;
                GraphSnapshot graphSnapshot = detail.GraphSnapshot;
                FindingsSnapshot findings = detail.FindingsSnapshot;
                IReadOnlyList<SynthesizedArtifact> artifacts = detail.ArtifactBundle?.Artifacts ?? [];

                DecisionProvenanceGraph graph = provenanceBuilder.Build(
                    detail.Run.RunId,
                    findings,
                    graphSnapshot,
                    manifest,
                    detail.AuthorityTrace,
                    artifacts);

                await indexer.IndexAuthorityRunAsync(
                    entry.TenantId,
                    entry.WorkspaceId,
                    entry.ProjectId,
                    manifest,
                    artifacts,
                    graph,
                    ct);

                await outbox.MarkProcessedAsync(entry.OutboxId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Retrieval outbox processing failed for outbox {OutboxId}, run {RunId}.",
                    entry.OutboxId,
                    entry.RunId);
            }
        }
    }
}

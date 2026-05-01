using ArchLucid.Application.Agents;
using ArchLucid.Application.Authority;
using ArchLucid.Application.Common;
using ArchLucid.Contracts.Abstractions.Agents;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Transactions;
using ArchLucid.Decisioning.Merge;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application;

/// <summary>
///     Replays an existing architecture run by cloning its tasks and evidence, re-executing agents,
///     and optionally committing the result as a new manifest version. Persists the replay run id to
///     <c>dbo.Runs</c> via <see cref="IRunRepository" /> (no legacy <c>ArchitectureRuns</c> insert).
///     Used by <see cref="ArchLucid.Application.Determinism.DeterminismCheckService" /> for multi-iteration
///     determinism checks and by comparison services for regenerating stored payloads.
/// </summary>
public sealed class ReplayRunService(
    IAgentExecutorResolver agentExecutorResolver,
    IDecisionEngineService decisionEngineService,
    IArchitectureRequestRepository requestRepository,
    IRunDetailQueryService runDetailQueryService,
    IRunRepository authorityRunRepository,
    IScopeContextProvider scopeContextProvider,
    IAuthorityCommittedManifestChainWriter authorityCommittedManifestChainWriter,
    IAgentEvidencePackageRepository agentEvidencePackageRepository,
    IArchLucidUnitOfWorkFactory unitOfWorkFactory,
    IAuditService auditService,
    IActorContext actorContext,
    ILogger<ReplayRunService> logger)
    : IReplayRunService
{
    private readonly IActorContext
        _actorContext = actorContext ?? throw new ArgumentNullException(nameof(actorContext));

    private readonly IAuditService
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly IAuthorityCommittedManifestChainWriter _authorityCommittedManifestChainWriter =
        authorityCommittedManifestChainWriter
        ?? throw new ArgumentNullException(nameof(authorityCommittedManifestChainWriter));

    private readonly ILogger<ReplayRunService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    ///     Creates a new run record seeded from <paramref name="originalRunId" />, re-executes agents,
    ///     and (when <paramref name="commitReplay" /> is <c>true</c>) commits a new manifest.
    /// </summary>
    /// <exception cref="RunNotFoundException">Thrown when <paramref name="originalRunId" /> does not exist.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the original run has no tasks, no evidence package, or merge fails.
    /// </exception>
    public async Task<ReplayRunResult> ReplayAsync(
        string originalRunId,
        string executionMode = ExecutionModes.Current,
        bool commitReplay = false,
        string? manifestVersionOverride = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(executionMode);
        ArgumentNullException.ThrowIfNull(authorityRunRepository);
        ArgumentNullException.ThrowIfNull(scopeContextProvider);

        ArchitectureRunDetail sourceDetail =
            await runDetailQueryService.GetRunDetailAsync(originalRunId, cancellationToken)
            ?? throw new RunNotFoundException(originalRunId);

        ArchitectureRun originalRun = sourceDetail.Run;
        List<AgentTask> tasks = sourceDetail.Tasks;

        cancellationToken.ThrowIfCancellationRequested();

        if (tasks.Count == 0)
            throw new InvalidOperationException($"No tasks found for run '{originalRunId}'.");

        ArchitectureRequest request = await requestRepository.GetByIdAsync(originalRun.RequestId, cancellationToken)
                                      ?? throw new InvalidOperationException(
                                          $"Request '{originalRun.RequestId}' not found.");

        AgentEvidencePackage evidence =
            await agentEvidencePackageRepository.GetByRunIdAsync(originalRunId, cancellationToken)
            ?? throw new InvalidOperationException($"Evidence package for run '{originalRunId}' not found.");

        string replayRunId = Guid.NewGuid().ToString("N");
        Guid replayGuid = Guid.Parse(replayRunId);

        ScopeContext scope = scopeContextProvider.GetCurrentScope();
        RunRecord? sourceAuthorityRun = null;

        if (Guid.TryParse(originalRunId, out Guid originalGuid))

            sourceAuthorityRun = await authorityRunRepository.GetByIdAsync(scope, originalGuid, cancellationToken);

        RunRecord replayAuthority = ReplayAuthorityRunRecordFactory.CreateForReplay(
            replayGuid,
            scope,
            sourceAuthorityRun,
            request);

        await authorityRunRepository.SaveAsync(replayAuthority, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        List<AgentTask> replayTasks = tasks
            .Select(t => new AgentTask
            {
                TaskId = Guid.NewGuid().ToString("N"),
                RunId = replayRunId,
                AgentType = t.AgentType,
                Objective = t.Objective,
                Status = AgentTaskStatus.Created,
                CreatedUtc = DateTime.UtcNow,
                CompletedUtc = null,
                EvidenceBundleRef = t.EvidenceBundleRef,
                AllowedTools = t.AllowedTools.ToList(),
                AllowedSources = t.AllowedSources.ToList()
            })
            .ToList();

        AgentEvidencePackage replayEvidence = CloneEvidenceForReplay(evidence, replayRunId);

        IAgentExecutor executor = agentExecutorResolver.Resolve(executionMode);
        IReadOnlyList<AgentResult> results = await executor.ExecuteAsync(
            replayRunId,
            request,
            replayEvidence,
            replayTasks,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        GoldenManifest? manifest = null;
        List<DecisionTrace> decisionTraces = [];
        List<string> warnings = [];

        if (!commitReplay)
            return new ReplayRunResult
            {
                OriginalRunId = originalRunId,
                ReplayRunId = replayRunId,
                ExecutionMode = executionMode,
                Results = results.ToList(),
                Manifest = manifest,
                DecisionTraces = decisionTraces,
                Warnings = warnings
            };

        string manifestVersion = string.IsNullOrWhiteSpace(manifestVersionOverride)
            ? BuildReplayManifestVersion(originalRun.CurrentManifestVersion)
            : manifestVersionOverride;

        DecisionMergeResult merge = decisionEngineService.MergeResults(
            replayRunId,
            request,
            manifestVersion,
            results,
            [],
            [],
            originalRun.CurrentManifestVersion);

        if (!merge.Success)

            throw new InvalidOperationException(
                $"Replay merge failed: {string.Join("; ", merge.Errors)}");

        manifest = merge.Manifest;
        decisionTraces = merge.DecisionTraces;
        warnings = merge.Warnings;

        Guid manifestId = Guid.NewGuid();
        Guid contextSnapshotId = Guid.NewGuid();
        Guid graphSnapshotId = Guid.NewGuid();
        Guid findingsSnapshotId = Guid.NewGuid();
        Guid authorityDecisionTraceId = Guid.NewGuid();
        AuthorityChainKeying chainKeying = new(
            manifestId,
            contextSnapshotId,
            graphSnapshotId,
            findingsSnapshotId,
            authorityDecisionTraceId);

        await using IArchLucidUnitOfWork uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

        try
        {
            AuthorityManifestPersistResult chainPersisted;

            // ADR 0030 PR A3 (2026-04-24): the legacy ICoordinatorDecisionTraceRepository second write to
            // dbo.DecisionTraces was removed along with the interface itself. The Authority FK chain writer
            // already persists the committed decision trace (chainKeying.DecisionTraceId → dbo.AuthorityDecisionTraces);
            // RunDetailQueryService now reads decision traces from the authority table only.
            if (uow.SupportsExternalTransaction)
                chainPersisted = await _authorityCommittedManifestChainWriter.PersistCommittedChainAsync(
                    scope,
                    replayGuid,
                    request.SystemName,
                    manifest,
                    chainKeying,
                    DateTime.UtcNow,
                    true,
                    cancellationToken,
                    uow.Connection,
                    uow.Transaction);
            else
                chainPersisted = await _authorityCommittedManifestChainWriter.PersistCommittedChainAsync(
                    scope,
                    replayGuid,
                    request.SystemName,
                    manifest,
                    chainKeying,
                    DateTime.UtcNow,
                    true,
                    cancellationToken,
                    null,
                    null);

            await uow.CommitAsync(cancellationToken);

            await AuthorityCommittedChainDurableAudit.TryLogAsync(
                _auditService,
                scopeContextProvider,
                _actorContext,
                _logger,
                replayGuid,
                request.SystemName,
                chainPersisted,
                "replay-commit",
                true,
                cancellationToken);
        }
        catch
        {
            await uow.RollbackAsync(cancellationToken);
            throw;
        }

        return new ReplayRunResult
        {
            OriginalRunId = originalRunId,
            ReplayRunId = replayRunId,
            ExecutionMode = executionMode,
            Results = results.ToList(),
            Manifest = manifest,
            DecisionTraces = decisionTraces,
            Warnings = warnings
        };
    }

    /// <summary>
    ///     Creates a deep copy of <paramref name="original" /> bound to <paramref name="replayRunId" />.
    ///     A clone is required so the replay run's evidence is isolated from the original run's mutable
    ///     collections — shared references would corrupt both runs if either were mutated.
    /// </summary>
    private static AgentEvidencePackage CloneEvidenceForReplay(
        AgentEvidencePackage original,
        string replayRunId)
    {
        return new AgentEvidencePackage
        {
            EvidencePackageId = Guid.NewGuid().ToString("N"),
            RunId = replayRunId,
            RequestId = original.RequestId,
            SystemName = original.SystemName,
            Environment = original.Environment,
            CloudProvider = original.CloudProvider,
            Request =
                new RequestEvidence
                {
                    Description = original.Request.Description,
                    Constraints = original.Request.Constraints.ToList(),
                    RequiredCapabilities = original.Request.RequiredCapabilities.ToList(),
                    Assumptions = original.Request.Assumptions.ToList()
                },
            Policies =
                original.Policies.Select(p => new PolicyEvidence
                {
                    PolicyId = p.PolicyId,
                    Title = p.Title,
                    Summary = p.Summary,
                    RequiredControls = p.RequiredControls.ToList(),
                    Tags = p.Tags.ToList()
                }).ToList(),
            ServiceCatalog = original.ServiceCatalog.Select(s => new ServiceCatalogEvidence
            {
                ServiceId = s.ServiceId,
                ServiceName = s.ServiceName,
                Category = s.Category,
                Summary = s.Summary,
                Tags = s.Tags.ToList(),
                RecommendedUseCases = s.RecommendedUseCases.ToList()
            }).ToList(),
            Patterns = original.Patterns.Select(p => new PatternEvidence
            {
                PatternId = p.PatternId,
                Name = p.Name,
                Summary = p.Summary,
                ApplicableCapabilities = p.ApplicableCapabilities.ToList(),
                SuggestedServices = p.SuggestedServices.ToList()
            }).ToList(),
            PriorManifest = original.PriorManifest is null
                ? null
                : new PriorManifestEvidence
                {
                    ManifestVersion = original.PriorManifest.ManifestVersion,
                    Summary = original.PriorManifest.Summary,
                    ExistingServices = original.PriorManifest.ExistingServices.ToList(),
                    ExistingDatastores = original.PriorManifest.ExistingDatastores.ToList(),
                    ExistingRequiredControls = original.PriorManifest.ExistingRequiredControls.ToList()
                },
            Notes =
                original.Notes.Select(n => new EvidenceNote { NoteType = n.NoteType, Message = n.Message }).ToList(),
            CreatedUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     Derives a replay manifest version by appending <c>-replay</c> to the current version,
    ///     or returns <c>v1-replay</c> when no current version exists.
    /// </summary>
    private static string BuildReplayManifestVersion(string? currentManifestVersion)
    {
        return string.IsNullOrWhiteSpace(currentManifestVersion) ? "v1-replay" : $"{currentManifestVersion}-replay";
    }
}

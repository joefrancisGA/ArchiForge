using ArchLucid.Application.Explanation;
using ArchLucid.Core.Explanation;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Models;
using ArchLucid.Provenance;

namespace ArchLucid.Host.Core.Demo;

/// <summary>
/// Default <see cref="IDemoReadModelClient"/>. Resolves the latest committed demo-seed run via
/// <see cref="IDemoSeedRunResolver"/>, then composes the explanation summary
/// and provenance graph for that run by calling the same application services the
/// <c>/v1/explain</c> and <c>/v1/provenance</c> controllers use — but always under the demo scope.
/// </summary>
public sealed class DemoReadModelClient(
    IDemoSeedRunResolver demoSeedRunResolver,
    IRunExplanationSummaryService runExplanationSummary,
    IProvenanceQueryService provenanceQuery,
    TimeProvider timeProvider,
    ILogger<DemoReadModelClient> logger) : IDemoReadModelClient
{
    private readonly IDemoSeedRunResolver _demoSeedRunResolver =
        demoSeedRunResolver ?? throw new ArgumentNullException(nameof(demoSeedRunResolver));

    private readonly IRunExplanationSummaryService _runExplanationSummary =
        runExplanationSummary ?? throw new ArgumentNullException(nameof(runExplanationSummary));

    private readonly IProvenanceQueryService _provenanceQuery =
        provenanceQuery ?? throw new ArgumentNullException(nameof(provenanceQuery));

    private readonly TimeProvider _timeProvider =
        timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    private readonly ILogger<DemoReadModelClient> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<DemoExplainResponse?> GetLatestCommittedDemoExplainAsync(CancellationToken cancellationToken = default)
    {
        ScopeContext scope = DemoScopes.BuildDemoScope();

        RunRecord? run = await _demoSeedRunResolver.ResolveLatestCommittedDemoRunAsync(cancellationToken);

        if (run is null)
        {
            _logger.LogInformation(
                "Demo explain: no committed demo-seed run found in scope {TenantId}; returning null.",
                scope.TenantId);

            return null;
        }

        RunExplanationSummary? explanation =
            await _runExplanationSummary.GetSummaryAsync(scope, run.RunId, cancellationToken);

        if (explanation is null)
        {
            _logger.LogWarning(
                "Demo explain: run {RunId} has no aggregate explanation summary in scope {TenantId}; returning null.",
                run.RunId,
                scope.TenantId);

            return null;
        }

        GraphViewModel graph = await _provenanceQuery.GetFullGraphAsync(scope, run.RunId, cancellationToken)
                               ?? new GraphViewModel();

        return new DemoExplainResponse
        {
            GeneratedUtc = _timeProvider.GetUtcNow(),
            RunId = run.RunId.ToString("N"),
            ManifestVersion = run.CurrentManifestVersion,
            IsDemoData = true,
            RunExplanation = explanation,
            ProvenanceGraph = graph,
        };
    }
}

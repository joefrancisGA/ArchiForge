using ArchLucid.Core.Explanation;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Caching;
using ArchLucid.Persistence.Queries;

using Microsoft.Extensions.Logging;

namespace ArchLucid.AgentRuntime.Explanation;

/// <summary>
/// Decorates <see cref="IRunExplanationSummaryService"/> with hot-path read caching so repeat run-detail views
/// do not repeat LLM work. Cache keys include <see cref="Models.RunRecord.RowVersion"/> so updates invalidate entries.
/// </summary>
public sealed class CachingRunExplanationSummaryService(
    IRunExplanationSummaryService inner,
    IHotPathReadCache cache,
    IAuthorityQueryService authorityQuery,
    ILogger<CachingRunExplanationSummaryService> logger) : IRunExplanationSummaryService
{
    private readonly IRunExplanationSummaryService _inner =
        inner ?? throw new ArgumentNullException(nameof(inner));

    private readonly IHotPathReadCache _cache =
        cache ?? throw new ArgumentNullException(nameof(cache));

    private readonly IAuthorityQueryService _authorityQuery =
        authorityQuery ?? throw new ArgumentNullException(nameof(authorityQuery));

    private readonly ILogger<CachingRunExplanationSummaryService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<RunExplanationSummary?> GetSummaryAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        RunDetailDto? detail = await _authorityQuery.GetRunDetailAsync(scope, runId, ct);

        if (detail is null)
            return null;

        if (detail.GoldenManifest is null)
            return null;

        byte[]? rowVersion = detail.Run?.RowVersion;

        if (rowVersion is null || rowVersion.Length == 0)
        {
            _logger.LogDebug(
                "Skipping explanation summary cache for run {RunId}: Run.RowVersion missing.",
                runId);

            return await _inner.GetSummaryAsync(scope, runId, ct);
        }

        string key = $"explanation:aggregate:{runId}:{Convert.ToHexString(rowVersion)}";

        return await _cache.GetOrCreateAsync(
            key,
            innerCt => _inner.GetSummaryAsync(scope, runId, innerCt),
            ct);
    }
}

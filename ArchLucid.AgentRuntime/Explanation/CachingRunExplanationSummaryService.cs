using ArchLucid.Application.Explanation;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Explanation;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Caching;
using ArchLucid.Persistence.Queries;

using Microsoft.Extensions.Logging;

namespace ArchLucid.AgentRuntime.Explanation;

/// <summary>
///     Decorates <see cref="IRunExplanationSummaryService" /> with hot-path read caching so repeat run-detail views
///     do not repeat LLM work. Cache keys include the run row <c>ROWVERSION</c> (
///     <see cref="ArchLucid.Persistence.Models.RunRecord.RowVersion" />) so updates invalidate entries.
/// </summary>
public sealed class CachingRunExplanationSummaryService(
    IRunExplanationSummaryService inner,
    IHotPathReadCache cache,
    IAuthorityQueryService authorityQuery,
    ILogger<CachingRunExplanationSummaryService> logger) : IRunExplanationSummaryService
{
    private readonly IAuthorityQueryService _authorityQuery =
        authorityQuery ?? throw new ArgumentNullException(nameof(authorityQuery));

    private readonly IHotPathReadCache _cache =
        cache ?? throw new ArgumentNullException(nameof(cache));

    private readonly IRunExplanationSummaryService _inner =
        inner ?? throw new ArgumentNullException(nameof(inner));

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

        byte[]? rowVersion = detail.Run.RowVersion;

        if (rowVersion is null || rowVersion.Length == 0)
        {
            _logger.LogDebug(
                "Skipping explanation summary cache for run {RunId}: Run.RowVersion missing.",
                runId);

            return await _inner.GetSummaryAsync(scope, runId, ct);
        }

        string key = $"explanation:aggregate:{runId}:{Convert.ToHexString(rowVersion)}";

        bool factoryInvoked = false;

        RunExplanationSummary? summary = await _cache.GetOrCreateAsync(
            key,
            async innerCt =>
            {
                factoryInvoked = true;
                ArchLucidInstrumentation.ExplanationCacheMisses.Add(1);

                return await _inner.GetSummaryAsync(scope, runId, innerCt);
            },
            ct);

        if (!factoryInvoked)
            ArchLucidInstrumentation.ExplanationCacheHits.Add(1);

        return summary;
    }
}

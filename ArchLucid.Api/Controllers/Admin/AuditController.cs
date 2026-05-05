using System.Globalization;
using System.Reflection;

using ArchLucid.Api.Attributes;
using ArchLucid.Api.Formatters;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Pagination;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Audit;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Admin;

/// <summary>
///     Returns a pageable audit event log for the caller's tenant/workspace/project scope.
/// </summary>
/// <remarks>
///     Events are appended by all mutating operations across the ArchLucid API (run creation, governance promotion, alert
///     delivery, etc.).
///     Results are ordered newest-first and capped by the <c>take</c> parameter (max
///     <see cref="PaginationDefaults.MaxListingTake" />).
/// </remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/audit")]
[EnableRateLimiting("fixed")]
public sealed class AuditController(IAuditRepository repo, IScopeContextProvider scopeProvider) : ControllerBase
{
    /// <remarks>
    ///     Returns newest-first audit events capped by <paramref name="take" />; pass <paramref name="cursor" /> from
    ///     <see cref="CursorPagedResponse{T}.NextCursor" /> for the next page.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(CursorPagedResponse<AuditEvent>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAudit(
        [FromQuery] string? cursor = null,
        [FromQuery] int take = 100,
        CancellationToken ct = default)
    {
        int clampedTake = Math.Clamp(take, 1, PaginationDefaults.MaxListingTake);
        ScopeContext scope = scopeProvider.GetCurrentScope();

        (DateTime OccurredUtc, Guid EventId)? cursorPair = AuditEventCursorCodec.TryDecode(cursor);

        if (!string.IsNullOrWhiteSpace(cursor) && cursorPair is null)
            return this.BadRequestProblem("cursor is invalid.", ProblemTypes.ValidationFailed);

        AuditEventFilter filter = new()
        {
            Take = clampedTake + 1, BeforeUtc = cursorPair?.OccurredUtc, BeforeEventId = cursorPair?.EventId
        };

        IReadOnlyList<AuditEvent> rows =
            await repo.GetFilteredAsync(scope.TenantId, scope.WorkspaceId, scope.ProjectId, filter, ct);

        return Ok(ToCursorPage(rows, clampedTake));
    }

    /// <summary>Filtered audit query within the current tenant/workspace/project scope.</summary>
    /// <param name="cursor">
    ///     Opaque keyset token from <see cref="CursorPagedResponse{T}.NextCursor" />; supersedes bare
    ///     <paramref name="beforeUtc" /> / <paramref name="beforeEventId" /> when both are present.
    /// </param>
    /// <param name="beforeUtc">Keyset cursor: only events at or before this instant per ordering (ISO-8601).</param>
    /// <param name="beforeEventId">
    ///     Optional tie-break when multiple events share the same <paramref name="beforeUtc" /> — pass the previous page’s
    ///     last <c>EventId</c> with the same <paramref name="beforeUtc" /> for stable pagination.
    /// </param>
    [HttpGet("search")]
    [ProducesResponseType(typeof(CursorPagedResponse<AuditEvent>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAudit(
        [FromQuery] string? cursor = null,
        [FromQuery] string? eventType = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] DateTime? beforeUtc = null,
        [FromQuery] Guid? beforeEventId = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] string? actorUserId = null,
        [FromQuery] Guid? runId = null,
        [FromQuery] int take = 100,
        CancellationToken ct = default)
    {
        (DateTime OccurredUtc, Guid EventId)? opaque = AuditEventCursorCodec.TryDecode(cursor);

        if (!string.IsNullOrWhiteSpace(cursor) && opaque is null)
            return this.BadRequestProblem("cursor is invalid.", ProblemTypes.ValidationFailed);

        if (beforeEventId.HasValue && !beforeUtc.HasValue && opaque is null)
        {
            return this.BadRequestProblem(
                "beforeEventId requires beforeUtc for stable keyset pagination.",
                ProblemTypes.ValidationFailed);
        }

        int clampedTake = Math.Clamp(take, 1, PaginationDefaults.MaxListingTake);
        ScopeContext scope = scopeProvider.GetCurrentScope();

        DateTime? effectiveBeforeUtc = opaque?.OccurredUtc ?? beforeUtc;
        Guid? effectiveBeforeEventId = opaque?.EventId ?? beforeEventId;

        AuditEventFilter filter = new()
        {
            EventType = eventType,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            BeforeUtc = effectiveBeforeUtc,
            BeforeEventId = effectiveBeforeEventId,
            CorrelationId = correlationId,
            ActorUserId = actorUserId,
            RunId = runId,
            Take = clampedTake + 1
        };

        IReadOnlyList<AuditEvent> rows = await repo.GetFilteredAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            filter,
            ct);

        return Ok(ToCursorPage(rows, clampedTake));
    }

    private static CursorPagedResponse<AuditEvent> ToCursorPage(IReadOnlyList<AuditEvent> rows, int clampedTake)
    {
        List<AuditEvent> materialized = rows.ToList();
        bool hasMore = materialized.Count > clampedTake;

        if (hasMore)

            materialized.RemoveAt(materialized.Count - 1);

        string? nextCursor = hasMore && materialized.Count > 0
            ? AuditEventCursorCodec.Encode(materialized[^1].OccurredUtc, materialized[^1].EventId)
            : null;

        return new CursorPagedResponse<AuditEvent>
        {
            Items = materialized, NextCursor = nextCursor, HasMore = hasMore, RequestedTake = clampedTake
        };
    }

    /// <summary>Lists distinct Core <see cref="AuditEventTypes" /> string constants (dropdown support).</summary>
    [HttpGet("event-types")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public IActionResult GetEventTypes()
    {
        IReadOnlyList<string> types = typeof(AuditEventTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(static f => f is { IsLiteral: true, FieldType: { } t } && t == typeof(string))
            .Select(static f => (string)f.GetRawConstantValue()!)
            .OrderBy(static s => s, StringComparer.Ordinal)
            .ToList();

        return Ok(types);
    }

    /// <summary>Exports audit events in the current scope for a UTC date range as JSON or CSV.</summary>
    /// <param name="fromUtc">Inclusive range start (UTC).</param>
    /// <param name="toUtc">Exclusive range end (UTC).</param>
    /// <param name="maxRows">Maximum rows to return; repository clamps to 1–10,000 (default 10,000).</param>
    /// <param name="format">
    ///     Optional export shape: <c>csv</c>, <c>json</c>, or <c>cef</c> (ArcSight CEF lines). When omitted, the response
    ///     follows standard content negotiation via <c>Accept</c> (JSON vs CSV).
    /// </param>
    [HttpGet("export")]
    [Authorize(Policy = ArchLucidPolicies.RequireAuditor)]
    [RequiresCommercialTenantTier(TenantTier.Enterprise)]
    [Produces("application/json", "text/csv", "text/plain")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditEvent>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EnableRateLimiting("expensive")]
    public async Task<IActionResult> ExportAudit(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int maxRows = 10_000,
        [FromQuery] string? format = null,
        CancellationToken ct = default)
    {
        DateTime from = NormalizeExportInstant(fromUtc);
        DateTime to = NormalizeExportInstant(toUtc);

        if (from >= to)
            return this.BadRequestProblem(
                "fromUtc must be strictly before toUtc.",
                ProblemTypes.ValidationFailed);

        if (to - from > TimeSpan.FromDays(90))
            return this.BadRequestProblem(
                "The requested date range must not exceed 90 days.",
                ProblemTypes.ValidationFailed);

        int exportMaxRows = Math.Clamp(maxRows <= 0 ? 10_000 : maxRows, 1, 10_000);

        ScopeContext scope = scopeProvider.GetCurrentScope();

        IReadOnlyList<AuditEvent> events = await repo.GetExportAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            from,
            to,
            exportMaxRows,
            ct);

        if (!string.IsNullOrWhiteSpace(format))
        {
            string f = format.Trim().ToLowerInvariant();

            if (f is not ("csv" or "json" or "cef"))
                return this.BadRequestProblem(
                    "format must be csv, json, or cef when supplied.",
                    ProblemTypes.ValidationFailed);

            if (f == "cef")
            {
                await using MemoryStream buffer = new();
                await AuditCefLineWriter.WriteAllAsync(buffer, events, ct).ConfigureAwait(false);
                byte[] utf8 = buffer.ToArray();
                string cefName = BuildAuditExportCefFileName(from, to);

                return File(utf8, "text/plain", cefName);
            }
        }

        string attachmentName = BuildAuditExportCsvFileName(from, to);
        HttpContext.Items[AuditEventCsvFormatter.CsvAttachmentFileNameItemKey] = attachmentName;

        return Ok(events);
    }

    private static string BuildAuditExportCefFileName(DateTime fromUtc, DateTime toUtc)
    {
        string fromPart = fromUtc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        string toPart = toUtc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

        return $"audit-export-{fromPart}-{toPart}.cef";
    }

    private static DateTime NormalizeExportInstant(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static string BuildAuditExportCsvFileName(DateTime fromUtc, DateTime toUtc)
    {
        string fromPart = fromUtc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        string toPart = toUtc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

        return $"audit-export-{fromPart}-{toPart}.csv";
    }
}

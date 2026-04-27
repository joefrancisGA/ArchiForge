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
    /// <summary>Returns recent audit events for the current scope, newest first.</summary>
    /// <param name="take">Maximum events to return (1–<see cref="PaginationDefaults.MaxListingTake" />, default 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of <see cref="AuditEvent" /> rows ordered by most-recent first.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AuditEvent>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAudit([FromQuery] int take = 100, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, PaginationDefaults.MaxListingTake);
        ScopeContext scope = scopeProvider.GetCurrentScope();

        IReadOnlyList<AuditEvent> events = await repo.GetByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            take,
            ct);

        return Ok(events);
    }

    /// <summary>Filtered audit query within the current tenant/workspace/project scope.</summary>
    /// <param name="beforeUtc">Keyset cursor: only events at or before this instant per ordering (ISO-8601).</param>
    /// <param name="beforeEventId">
    ///     Optional tie-break when multiple events share the same <paramref name="beforeUtc" /> — pass the previous page’s
    ///     last <c>EventId</c> with the same <paramref name="beforeUtc" /> for stable pagination.
    /// </param>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditEvent>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAudit(
        [FromQuery] string? eventType,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] DateTime? beforeUtc,
        [FromQuery] Guid? beforeEventId,
        [FromQuery] string? correlationId,
        [FromQuery] string? actorUserId,
        [FromQuery] Guid? runId,
        [FromQuery] int take = 100,
        CancellationToken ct = default)
    {
        int clampedTake = Math.Clamp(take, 1, PaginationDefaults.MaxListingTake);
        ScopeContext scope = scopeProvider.GetCurrentScope();

        AuditEventFilter filter = new()
        {
            EventType = eventType,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            BeforeUtc = beforeUtc,
            BeforeEventId = beforeEventId,
            CorrelationId = correlationId,
            ActorUserId = actorUserId,
            RunId = runId,
            Take = clampedTake
        };

        IReadOnlyList<AuditEvent> events = await repo.GetFilteredAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            filter,
            ct);

        return Ok(events);
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
    [HttpGet("export")]
    [Authorize(Policy = ArchLucidPolicies.RequireAuditor)]
    [RequiresCommercialTenantTier(TenantTier.Enterprise)]
    [Produces("application/json", "text/csv")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditEvent>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EnableRateLimiting("expensive")]
    public async Task<IActionResult> ExportAudit(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int maxRows = 10_000,
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

        string attachmentName = BuildAuditExportCsvFileName(from, to);
        HttpContext.Items[AuditEventCsvFormatter.CsvAttachmentFileNameItemKey] = attachmentName;

        ScopeContext scope = scopeProvider.GetCurrentScope();

        IReadOnlyList<AuditEvent> events = await repo.GetExportAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            from,
            to,
            exportMaxRows,
            ct);

        return Ok(events);
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

using ArchLucid.Core.Pagination;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ArchLucid.Api.Swagger;

/// <summary>
/// Documents GET /architecture/comparisons search (query shape and paging semantics).
/// </summary>
public sealed class ComparisonHistoryQueryOperationFilter : IOperationFilter
{
    public void Apply(Microsoft.OpenApi.OpenApiOperation operation, OperationFilterContext context)
    {
        if (!string.Equals(context.MethodInfo?.Name, "SearchComparisonRecords", StringComparison.Ordinal))
            return;

        operation.Summary ??= "Search persisted comparison records";
        operation.Description =
            "Query parameters (all optional unless noted): "
            + "**comparisonType** — `end-to-end-replay` | `export-record-diff`. "
            + "**leftRunId**, **rightRunId**, **leftExportRecordId**, **rightExportRecordId**, **label**. "
            + "**createdFromUtc**, **createdToUtc** (UTC). **tag** (comma-separated) or repeat **tags**. "
            + "**sortBy** — `createdUtc` (default), `type`, `label`, `leftRunId`, `rightRunId`. "
            + "**sortDir** — `asc` | `desc`. "
            + "**cursor** — keyset `<utcTicks>:<comparisonRecordId>` (requires **sortBy**=createdUtc). "
            + $"**skip** (>=0), **limit** (0–{PaginationDefaults.MaxListingTake}; **0** = default page size {PaginationDefaults.DefaultPageSize}). "
            + "Response includes **nextCursor** when sorted by createdUtc.";
    }
}

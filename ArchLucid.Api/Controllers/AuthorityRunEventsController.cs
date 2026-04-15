using System.Text;
using System.Text.Json;

using ArchLucid.Api.Contracts;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Queries;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers;

/// <summary>
/// Server-sent events stream for run summary polling (operator UI). Sends periodic <c>status</c> events and a terminal <c>complete</c> event.
/// </summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/authority")]
[EnableRateLimiting("fixed")]
public sealed class AuthorityRunEventsController(
    IAuthorityQueryService queryService,
    IScopeContextProvider scopeProvider) : ControllerBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Streams <c>text/event-stream</c> with run summary JSON until golden manifest is ready or the server times out (~5 minutes).</summary>
    [HttpGet("runs/{runId:guid}/events")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task GetRunEvents(Guid runId, CancellationToken cancellationToken)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        ScopeContext scope = scopeProvider.GetCurrentScope();
        DateTime startedUtc = DateTime.UtcNow;
        TimeSpan maxDuration = TimeSpan.FromMinutes(5);
        TimeSpan pollInterval = TimeSpan.FromSeconds(2);
        string? lastPayloadFingerprint = null;

        while (!cancellationToken.IsCancellationRequested
            && DateTime.UtcNow - startedUtc <= maxDuration)
        {
            RunSummaryDto? summaryDto = await queryService.GetRunSummaryAsync(scope, runId, cancellationToken);

            if (summaryDto is null)
            {
                await WriteSseEventAsync("error", """{"detail":"Run summary not found"}""", cancellationToken);
                await WriteSseEventAsync("complete", """{"reason":"not-found"}""", cancellationToken);

                return;
            }

            RunSummaryResponse body = ToRunSummaryResponse(summaryDto);
            string json = JsonSerializer.Serialize(body, SerializerOptions);

            if (!string.Equals(json, lastPayloadFingerprint, StringComparison.Ordinal))
            {
                lastPayloadFingerprint = json;
                await WriteSseEventAsync("status", json, cancellationToken);
            }

            if (summaryDto.HasGoldenManifest)
            {
                await WriteSseEventAsync("complete", """{"reason":"golden-manifest-ready"}""", cancellationToken);

                return;
            }

            await Task.Delay(pollInterval, cancellationToken);
        }

        await WriteSseEventAsync("complete", """{"reason":"timeout"}""", cancellationToken);
    }

    private async Task WriteSseEventAsync(string eventName, string data, CancellationToken cancellationToken)
    {
        string id = Guid.NewGuid().ToString("N");
        StringBuilder sb = new();
        sb.Append("id: ").Append(id).Append('\n');
        sb.Append("event: ").Append(eventName).Append('\n');
        sb.Append("data: ");

        foreach (string line in data.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            sb.Append(line).Append('\n');
        }

        sb.Append('\n');
        byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
        await Response.Body.WriteAsync(bytes, cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private static RunSummaryResponse ToRunSummaryResponse(RunSummaryDto x) =>
        new()
        {
            RunId = x.RunId,
            ProjectId = x.ProjectId,
            Description = x.Description,
            CreatedUtc = x.CreatedUtc,
            ContextSnapshotId = x.ContextSnapshotId,
            GraphSnapshotId = x.GraphSnapshotId,
            FindingsSnapshotId = x.FindingsSnapshotId,
            GoldenManifestId = x.GoldenManifestId,
            DecisionTraceId = x.DecisionTraceId,
            ArtifactBundleId = x.ArtifactBundleId,
            HasContextSnapshot = x.HasContextSnapshot,
            HasGraphSnapshot = x.HasGraphSnapshot,
            HasFindingsSnapshot = x.HasFindingsSnapshot,
            HasGoldenManifest = x.HasGoldenManifest,
            HasDecisionTrace = x.HasDecisionTrace,
            HasArtifactBundle = x.HasArtifactBundle,
        };
}

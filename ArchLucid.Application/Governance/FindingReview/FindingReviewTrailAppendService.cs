using System.Globalization;
using System.Text.Json;

using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Serialization;

namespace ArchLucid.Application.Governance.FindingReview;

/// <inheritdoc cref="IFindingReviewTrailAppendService" />
public sealed class FindingReviewTrailAppendService(
    IFindingReviewTrailRepository trailRepository,
    IAuditService auditService) : IFindingReviewTrailAppendService
{
    private readonly IFindingReviewTrailRepository _trailRepository =
        trailRepository ?? throw new ArgumentNullException(nameof(trailRepository));

    private readonly IAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    /// <inheritdoc />
    public async Task AppendAsync(FindingReviewEventRecord reviewEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reviewEvent);

        await _trailRepository.AppendAsync(reviewEvent, cancellationToken);

        string eventType = MapActionToAuditEventType(reviewEvent.Action);

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = eventType,
                ActorUserId = reviewEvent.ReviewerUserId,
                ActorUserName = reviewEvent.ReviewerUserId,
                TenantId = reviewEvent.TenantId,
                WorkspaceId = reviewEvent.WorkspaceId,
                ProjectId = reviewEvent.ProjectId,
                RunId = reviewEvent.RunId,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        reviewEvent.EventId,
                        reviewEvent.FindingId,
                        reviewEvent.Action,
                        reviewEvent.Notes,
                        reviewEvent.OccurredAtUtc,
                    },
                    AuditJsonSerializationOptions.Instance),
            },
            cancellationToken);
    }

    private static string MapActionToAuditEventType(string action)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Review action must be populated.", nameof(action));

        string normalized = action.Trim();

        if (string.Equals(normalized, "Approved", StringComparison.OrdinalIgnoreCase))
            return AuditEventTypes.FindingReviewApproved;

        if (string.Equals(normalized, "Rejected", StringComparison.OrdinalIgnoreCase))
            return AuditEventTypes.FindingReviewRejected;

        if (string.Equals(normalized, "Overridden", StringComparison.OrdinalIgnoreCase))
            return AuditEventTypes.FindingReviewOverridden;

        throw new ArgumentOutOfRangeException(
            nameof(action),
            action,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Unsupported finding review Action '{normalized}'. Extend MapActionToAuditEventType when adding new verbs."));
    }
}

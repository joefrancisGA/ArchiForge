using System.Text.Json;

using ArchLucid.Core.Audit;

namespace ArchLucid.Application.Diagnostics;

public interface ISyntheticOperatorDemoPackWriter
{
    /// <summary>Appends durable marker audit rows tagged for easy purge/filter.</summary>
    Task<int> WriteMarkerEventsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Writes a small batch of synthetic audit events (no runs, no governance mutations) so operators can validate UI
///     surfaces before their first real commit.
/// </summary>
public sealed class SyntheticOperatorDemoPackWriter(IAuditService auditService) : ISyntheticOperatorDemoPackWriter
{
    private readonly IAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    public async Task<int> WriteMarkerEventsAsync(CancellationToken cancellationToken = default)
    {
        int written = 0;

        for (int i = 0; i < 5; i++)
        {
            string payload = JsonSerializer.Serialize(
                new
                {
                    syntheticDemoPack = true,
                    sequence = i + 1,
                    purgeHint =
                        "Filter AuditEventTypes SyntheticOperatorDemoPack.Marker or DataJson.syntheticDemoPack=true",
                    createdBy = "POST /v1/diagnostics/synthetic-operator-demo-pack"
                });

            await _auditService.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.SyntheticOperatorDemoPackMarker,
                        ActorUserName = "SyntheticDemoPack",
                        DataJson = payload
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            written++;
        }

        return written;
    }
}

using System.Text.Json.Serialization;

namespace ArchLucid.Contracts.DecisionTraces;

/// <summary>
/// Coordinator pipeline trace: append-only merge and agent steps for a string <c>RunId</c> (correlates with authority <c>dbo.Runs</c>).
/// Stored by the coordinator decision-trace repository port (string run id).
/// </summary>
[JsonConverter(typeof(DecisionTraceJsonConverter))]
public sealed class RunEventTrace : DecisionTrace
{
    /// <inheritdoc />
    [JsonIgnore]
    public override DecisionTraceKind Kind => DecisionTraceKind.RunEvent;

    /// <summary>Event payload (also embedded in SQL <c>EventJson</c> for coordinator rows).</summary>
    public required RunEventTracePayload RunEvent { get; set; }

    public static RunEventTrace From(RunEventTracePayload body) =>
        new()
        {
            RunEvent = body ?? throw new ArgumentNullException(nameof(body))
        };
}

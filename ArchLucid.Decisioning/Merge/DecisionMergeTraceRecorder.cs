using ArchLucid.Contracts.DecisionTraces;

namespace ArchLucid.Decisioning.Merge;

/// <summary>
///     Appends <see cref="RunEventTrace" /> entries to a <see cref="DecisionMergeResult" /> during merge.
/// </summary>
public static class DecisionMergeTraceRecorder
{
    public static void AddTrace(
        DecisionMergeResult output,
        string runId,
        string eventType,
        string description,
        IReadOnlyDictionary<string, string>? metadata)
    {
        ArgumentNullException.ThrowIfNull(output);

        Dictionary<string, string> snapshot = metadata is null || metadata.Count == 0
            ? []
            : new Dictionary<string, string>(metadata);

        output.DecisionTraces.Add(RunEventTrace.From(new RunEventTracePayload
        {
            TraceId = Guid.NewGuid().ToString("N"),
            RunId = runId,
            EventType = eventType,
            EventDescription = description,
            CreatedUtc = DateTime.UtcNow,
            Metadata = snapshot
        }));
    }
}

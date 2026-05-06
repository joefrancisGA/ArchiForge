namespace ArchLucid.Application.Audit;
/// <summary>One audit row for operator pipeline timeline (run-scoped).</summary>
public sealed record RunPipelineTimelineItemDto(Guid EventId, DateTime OccurredUtc, string EventType, string ActorUserName, string? CorrelationId)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(EventType, ActorUserName, CorrelationId);
    private static byte __ValidatePrimaryConstructorArguments(System.String EventType, System.String ActorUserName, System.String? CorrelationId)
    {
        ArgumentNullException.ThrowIfNull(EventType);
        ArgumentNullException.ThrowIfNull(ActorUserName);
        return (byte)0;
    }
}
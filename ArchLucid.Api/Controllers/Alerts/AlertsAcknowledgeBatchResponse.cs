namespace ArchLucid.Api.Controllers;

/// <summary>Per-alert outcome for batch acknowledge.</summary>
public sealed class AlertsAcknowledgeBatchItemResult
{
    public Guid AlertId { get; set; }

    public bool Succeeded { get; set; }

    public string? Message { get; set; }
}

/// <summary>Response for <c>POST /v1/alerts/acknowledge-batch</c>.</summary>
public sealed class AlertsAcknowledgeBatchResponse
{
    public IReadOnlyList<AlertsAcknowledgeBatchItemResult> Results { get; set; } = [];
}

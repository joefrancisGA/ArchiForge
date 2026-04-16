namespace ArchLucid.Api.Controllers;

/// <summary>Body for <c>POST /v1/alerts/acknowledge-batch</c>.</summary>
public sealed class AlertsAcknowledgeBatchRequest
{
    /// <summary>Alert ids in the current scope (max 100).</summary>
    public IReadOnlyList<Guid> AlertIds { get; set; } = [];

    /// <summary>Optional operator comment stored on acknowledge.</summary>
    public string? Comment { get; set; }
}

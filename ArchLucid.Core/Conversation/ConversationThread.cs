namespace ArchiForge.Core.Conversation;

/// <summary>
/// Scoped Ask conversation header: identity, optional run anchors for manifest/comparison context, and activity timestamps.
/// </summary>
/// <remarks>Persisted in <c>dbo.ConversationThreads</c> when SQL storage is enabled.</remarks>
public class ConversationThread
{
    /// <summary>Primary key for the thread.</summary>
    public Guid ThreadId { get; set; } = Guid.NewGuid();

    /// <summary>Tenant dimension (must match HTTP scope).</summary>
    public Guid TenantId { get; set; }

    /// <summary>Workspace dimension.</summary>
    public Guid WorkspaceId { get; set; }

    /// <summary>Project dimension.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Primary manifest run when the thread is anchored to authority data.</summary>
    public Guid? RunId { get; set; }

    /// <summary>Optional comparison base run (paired with <see cref="TargetRunId"/>).</summary>
    public Guid? BaseRunId { get; set; }

    /// <summary>Optional comparison target run.</summary>
    public Guid? TargetRunId { get; set; }

    /// <summary>Operator-visible label.</summary>
    public string Title { get; set; } = "New Conversation";

    /// <summary>Row creation time (UTC).</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Updated when messages are appended.</summary>
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>When set, thread list/get hide this row (soft archival by retention job).</summary>
    public DateTime? ArchivedUtc { get; set; }
}

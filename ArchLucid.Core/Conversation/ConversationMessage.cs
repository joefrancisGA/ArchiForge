namespace ArchiForge.Core.Conversation;

/// <summary>
/// One turn in a <see cref="ConversationThread"/> (user, assistant, or system role string).
/// </summary>
/// <remarks>
/// <see cref="Role"/> is typically <see cref="ConversationMessageRole.User"/>, <see cref="ConversationMessageRole.Assistant"/>, or <see cref="ConversationMessageRole.System"/>.
/// Assistant rows may store citation metadata in <see cref="MetadataJson"/>.
/// </remarks>
public class ConversationMessage
{
    /// <summary>Message primary key.</summary>
    public Guid MessageId { get; set; } = Guid.NewGuid();

    /// <summary>Owning thread.</summary>
    public Guid ThreadId { get; set; }

    /// <summary>Role label (see <see cref="ConversationMessageRole"/>).</summary>
    public string Role { get; set; } = null!;

    /// <summary>Plain text content for prompts and UI.</summary>
    public string Content { get; set; } = null!;

    /// <summary>When the message was persisted (UTC).</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>JSON sidecar (e.g. referenced decisions/findings for assistant turns).</summary>
    public string MetadataJson { get; set; } = "{}";
}

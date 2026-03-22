namespace ArchiForge.Core.Conversation;

public class ConversationMessage
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public Guid ThreadId { get; set; }

    public string Role { get; set; } = null!;
    public string Content { get; set; } = null!;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public string MetadataJson { get; set; } = "{}";
}

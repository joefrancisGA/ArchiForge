namespace ArchiForge.Core.Conversation;

public class ConversationThread
{
    public Guid ThreadId { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public Guid? RunId { get; set; }
    public Guid? BaseRunId { get; set; }
    public Guid? TargetRunId { get; set; }

    public string Title { get; set; } = "New Conversation";

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}

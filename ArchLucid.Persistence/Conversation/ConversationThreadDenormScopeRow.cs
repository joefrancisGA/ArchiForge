namespace ArchLucid.Persistence.Conversation;

/// <summary>
///     Row shape for reading denormalized RLS scope from <c>dbo.ConversationThreads</c> when inserting messages.
/// </summary>
internal sealed record ConversationThreadDenormScopeRow(Guid? TenantId, Guid? WorkspaceId, Guid? ProjectId);

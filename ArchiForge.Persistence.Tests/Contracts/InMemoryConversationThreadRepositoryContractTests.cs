using ArchiForge.Persistence.Conversation;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Runs <see cref="ConversationThreadRepositoryContractTests"/> against <see cref="InMemoryConversationThreadRepository"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryConversationThreadRepositoryContractTests : ConversationThreadRepositoryContractTests
{
    protected override IConversationThreadRepository CreateRepository()
    {
        return new InMemoryConversationThreadRepository();
    }
}

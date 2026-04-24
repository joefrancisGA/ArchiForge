using ArchLucid.Persistence.Conversation;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="ConversationMessageRepositoryContractTests" /> against
///     <see cref="InMemoryConversationMessageRepository" />.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryConversationMessageRepositoryContractTests : ConversationMessageRepositoryContractTests
{
    protected override IConversationMessageRepository CreateRepository()
    {
        return new InMemoryConversationMessageRepository();
    }
}

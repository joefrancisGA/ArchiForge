using ArchLucid.Persistence.Conversation;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="ConversationThreadRepositoryContractTests" /> against
///     <see cref="InMemoryConversationThreadRepository" />.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryConversationThreadRepositoryContractTests : ConversationThreadRepositoryContractTests
{
    protected override IConversationThreadRepository CreateRepository()
    {
        return new InMemoryConversationThreadRepository();
    }
}

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
public sealed class EchoAgentCompletionClientTests
{
    [Fact]
    public async Task CompleteJsonAsync_topology_prompt_returns_topology_agent_type()
    {
        EchoAgentCompletionClient sut = new();

        string json = await sut.CompleteJsonAsync(
            """
            You are the ArchLucid Topology Agent.
            Return JSON.
            """,
            """
            RunId: RUN-X
            TaskId: TASK-Y
            """,
            CancellationToken.None);

        json.Should().Contain("\"agentType\":\"Topology\"");
        json.Should().Contain("RUN-X");
        json.Should().Contain("TASK-Y");
    }

    [Fact]
    public async Task CompleteJsonAsync_compliance_prompt_returns_compliance_agent_type()
    {
        EchoAgentCompletionClient sut = new();

        string json = await sut.CompleteJsonAsync(
            """
            You are the ArchLucid Compliance Agent.
            Return JSON.
            """,
            """
            RunId: RUN-C
            TaskId: TASK-C
            """,
            CancellationToken.None);

        json.Should().Contain("\"agentType\":\"Compliance\"");
    }

    [Fact]
    public async Task CompleteJsonAsync_critic_prompt_returns_critic_agent_type()
    {
        EchoAgentCompletionClient sut = new();

        string json = await sut.CompleteJsonAsync(
            """
            You are the ArchLucid Critic Agent.
            Return JSON.
            """,
            """
            RunId: RUN-K
            TaskId: TASK-K
            """,
            CancellationToken.None);

        json.Should().Contain("\"agentType\":\"Critic\"");
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchiForge.Core.Ask;
using ArchiForge.Core.Conversation;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

/// <summary>
/// End-to-end: seed authority run → POST <c>v1/ask</c> with fake LLM → verify response includes thread and answer → list conversations via <c>GET v1/conversations</c>.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AskThreadIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task Ask_with_seeded_run_returns_answer_and_creates_thread()
    {
        await using AlertLifecycleWebAppFactory factory = new();
        Guid runId = await AdvisoryIntegrationSeed.SeedDefaultScopeAuthorityRunAsync(
            factory.Services, CancellationToken.None);

        HttpClient client = factory.CreateClient();

        HttpResponseMessage askResponse = await client.PostAsJsonAsync(
            "v1/ask",
            new AskRequest
            {
                RunId = runId,
                Question = "What is the primary architecture topology?"
            },
            JsonOptions,
            CancellationToken.None);

        askResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AskResponse? result = await askResponse.Content
            .ReadFromJsonAsync<AskResponse>(JsonOptions, CancellationToken.None);

        result.Should().NotBeNull();
        result.ThreadId.Should().NotBeEmpty();
        result.Answer.Should().NotBeNullOrWhiteSpace();

        HttpResponseMessage threadsResponse = await client.GetAsync(
            new Uri("v1/conversations?take=10", UriKind.Relative),
            CancellationToken.None);

        threadsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<ConversationThread>? threads = await threadsResponse.Content
            .ReadFromJsonAsync<List<ConversationThread>>(JsonOptions, CancellationToken.None);

        threads.Should().NotBeNull();
        threads.Should().Contain(t => t.ThreadId == result.ThreadId);
    }

    [Fact]
    public async Task Ask_follow_up_continues_same_thread()
    {
        await using AlertLifecycleWebAppFactory factory = new();
        Guid runId = await AdvisoryIntegrationSeed.SeedDefaultScopeAuthorityRunAsync(
            factory.Services, CancellationToken.None);

        HttpClient client = factory.CreateClient();

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync(
            "v1/ask",
            new AskRequest { RunId = runId, Question = "How many decisions exist?" },
            JsonOptions,
            CancellationToken.None);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AskResponse? first = await firstResponse.Content
            .ReadFromJsonAsync<AskResponse>(JsonOptions, CancellationToken.None);

        first.Should().NotBeNull();
        Guid threadId = first.ThreadId;

        HttpResponseMessage followUpResponse = await client.PostAsJsonAsync(
            "v1/ask",
            new AskRequest { ThreadId = threadId, Question = "What are the security concerns?" },
            JsonOptions,
            CancellationToken.None);

        followUpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AskResponse? followUp = await followUpResponse.Content
            .ReadFromJsonAsync<AskResponse>(JsonOptions, CancellationToken.None);

        followUp.Should().NotBeNull();
        followUp.ThreadId.Should().Be(threadId, "follow-up should reuse the same thread");

        HttpResponseMessage messagesResponse = await client.GetAsync(
            new Uri($"v1/conversations/{threadId:D}/messages?take=50", UriKind.Relative),
            CancellationToken.None);

        messagesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<ConversationMessage>? messages = await messagesResponse.Content
            .ReadFromJsonAsync<List<ConversationMessage>>(JsonOptions, CancellationToken.None);

        messages.Should().NotBeNull();
        messages.Should().HaveCountGreaterThanOrEqualTo(4, "two user + two assistant messages expected");
    }

    [Fact]
    public async Task Ask_without_question_returns_bad_request()
    {
        await using AlertLifecycleWebAppFactory factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "v1/ask",
            new AskRequest { RunId = Guid.NewGuid(), Question = "" },
            JsonOptions,
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ask_without_runId_or_threadId_returns_bad_request()
    {
        await using AlertLifecycleWebAppFactory factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "v1/ask",
            new AskRequest { Question = "Some question without anchor" },
            JsonOptions,
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

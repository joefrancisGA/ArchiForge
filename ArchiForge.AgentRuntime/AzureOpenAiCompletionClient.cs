using System.ClientModel;
using System.Diagnostics.CodeAnalysis;

using Azure.AI.OpenAI;

using OpenAI.Chat;

namespace ArchiForge.AgentRuntime;

/// <summary>
/// Azure OpenAI chat client using JSON object response format and low temperature for deterministic structured outputs.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Thin wrapper around Azure OpenAI SDK; requires live Azure endpoint to exercise.")]
public sealed class AzureOpenAiCompletionClient : IAgentCompletionClient
{
    /// <summary>Used when <c>AzureOpenAI:MaxCompletionTokens</c> is omitted or zero.</summary>
    public const int DefaultMaxCompletionTokens = 4096;

    private readonly ChatClient _chatClient;
    private readonly int _maxOutputTokens;

    /// <summary>
    /// Creates a client for the given deployment (model) on the Azure OpenAI resource.
    /// </summary>
    /// <param name="endpoint">Azure OpenAI endpoint URI.</param>
    /// <param name="apiKey">API key credential.</param>
    /// <param name="deploymentName">Chat deployment name.</param>
    /// <param name="maxCompletionTokens">Positive cap on completion tokens (output).</param>
    public AzureOpenAiCompletionClient(
        string endpoint,
        string apiKey,
        string deploymentName,
        int maxCompletionTokens)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentName);

        if (maxCompletionTokens < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCompletionTokens), maxCompletionTokens, "Must be at least 1.");
        }

        AzureOpenAIClient azureClient = new(
            new Uri(endpoint),
            new ApiKeyCredential(apiKey));

        _chatClient = azureClient.GetChatClient(deploymentName);
        _maxOutputTokens = maxCompletionTokens;
    }

    /// <inheritdoc />
    /// <remarks>Uses <c>Temperature = 0.1</c>, <c>MaxOutputTokenCount</c>, and <c>ChatResponseFormat.CreateJsonObjectFormat()</c>.</remarks>
    public async Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(userPrompt);
        List<ChatMessage> messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        ];

        ChatCompletionOptions options = new()
        {
            Temperature = 0.1f,
            MaxOutputTokenCount = _maxOutputTokens,
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        ClientResult<ChatCompletion> response = await _chatClient.CompleteChatAsync(
            messages,
            options,
            cancellationToken);

        ChatCompletion completion = response.Value;
        IReadOnlyList<ChatMessageContentPart> parts = completion.Content;

        if (parts == null || parts.Count < 1)
        {
            throw new InvalidOperationException("Azure OpenAI returned no message content.");
        }

        string? text = parts[0].Text;

        if (string.IsNullOrEmpty(text))
        {
            throw new InvalidOperationException("Azure OpenAI returned an empty assistant message.");
        }

        return text;
    }
}

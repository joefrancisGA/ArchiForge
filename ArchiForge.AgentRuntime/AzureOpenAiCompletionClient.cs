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
    private readonly ChatClient _chatClient;

    /// <summary>
    /// Creates a client for the given deployment (model) on the Azure OpenAI resource.
    /// </summary>
    /// <param name="endpoint">Azure OpenAI endpoint URI.</param>
    /// <param name="apiKey">API key credential.</param>
    /// <param name="deploymentName">Chat deployment name.</param>
    public AzureOpenAiCompletionClient(
        string endpoint,
        string apiKey,
        string deploymentName)
    {
        AzureOpenAIClient azureClient = new(
            new Uri(endpoint),
            new ApiKeyCredential(apiKey));

        _chatClient = azureClient.GetChatClient(deploymentName);
    }

    /// <inheritdoc />
    /// <remarks>Uses <c>Temperature = 0.1</c> and <c>ChatResponseFormat.CreateJsonObjectFormat()</c>.</remarks>
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
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        ClientResult<ChatCompletion>? response = await _chatClient.CompleteChatAsync(
            messages,
            options,
            cancellationToken);

        return response.Value.Content[0].Text;
    }
}

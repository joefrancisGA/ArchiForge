using System.ClientModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.Core.Diagnostics;

using Azure.AI.OpenAI;

using OpenAI.Chat;

namespace ArchLucid.AgentRuntime;

/// <summary>
/// Azure OpenAI chat client using JSON object response format and low temperature for deterministic structured outputs.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Thin wrapper around Azure OpenAI SDK; requires live Azure endpoint to exercise.")]
public sealed class AzureOpenAiCompletionClient : IAgentCompletionClient
{
    private static readonly AsyncLocal<(int Prompt, int Completion)?> LastCompletionTokenUsage = new();

    /// <summary>Used when <c>AzureOpenAI:MaxCompletionTokens</c> is omitted or zero.</summary>
    public const int DefaultMaxCompletionTokens = 4096;

    private readonly ChatClient _chatClient;
    private readonly int _maxOutputTokens;
    private readonly LlmProviderDescriptor _descriptor;

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

        Uri endpointUri = new(endpoint);
        AzureOpenAIClient azureClient = new(
            endpointUri,
            new ApiKeyCredential(apiKey));

        _chatClient = azureClient.GetChatClient(deploymentName);
        _maxOutputTokens = maxCompletionTokens;
        _descriptor = LlmProviderDescriptor.ForAzureOpenAi(endpointUri, deploymentName);
    }

    /// <inheritdoc />
    public LlmProviderDescriptor Descriptor => _descriptor;

    /// <summary>Consumes token usage from the last successful <see cref="CompleteJsonAsync"/> on this async flow, if any.</summary>
    public static bool TryConsumeLastCompletionTokenUsage(out int promptTokens, out int completionTokens)
    {
        (int Prompt, int Completion)? raw = LastCompletionTokenUsage.Value;
        LastCompletionTokenUsage.Value = null;

        if (raw is { } v)
        {
            promptTokens = v.Prompt;
            completionTokens = v.Completion;

            return true;
        }

        promptTokens = 0;
        completionTokens = 0;

        return false;
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
        LastCompletionTokenUsage.Value = null;

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

        using Activity? llmActivity = ArchLucidInstrumentation.AgentLlmCompletion.StartActivity(
            "gen_ai.chat.completion",
            ActivityKind.Client);

        llmActivity?.SetTag("gen_ai.system", "azure_openai");

        ClientResult<ChatCompletion> response;

        try
        {
            response = await _chatClient.CompleteChatAsync(
                messages,
                options,
                cancellationToken);
        }
        catch (Exception ex)
        {
            llmActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            llmActivity?.AddException(ex);

            throw;
        }

        ChatCompletion completion = response.Value;

        if (completion.Usage is { } usage)
        {
            int inTok = usage.InputTokenCount is var ip ? ip : 0;
            int outTok = usage.OutputTokenCount is var op ? op : 0;

            if (inTok > 0 || outTok > 0)
            {
                LastCompletionTokenUsage.Value = (inTok, outTok);
            }

            if (llmActivity is not null)
            {
                llmActivity.SetTag("gen_ai.usage.input_tokens", usage.InputTokenCount);
                llmActivity.SetTag("gen_ai.usage.output_tokens", usage.OutputTokenCount);
                llmActivity.SetTag("gen_ai.usage.total_tokens", usage.TotalTokenCount);
            }
        }

        IReadOnlyList<ChatMessageContentPart> parts = completion.Content;

        if (parts == null || parts.Count < 1)
        {
            throw new InvalidOperationException("Azure OpenAI returned no message content.");
        }

        string? text = parts[0].Text;

        return string.IsNullOrEmpty(text) ? throw new InvalidOperationException("Azure OpenAI returned an empty assistant message.") : text;
    }
}

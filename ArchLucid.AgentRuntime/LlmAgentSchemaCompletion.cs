using System.Linq;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Core.Diagnostics;

using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime;

/// <summary>
///     Shared LLM → JSON schema validation loop for topology / compliance / critic handlers when the model emits invalid
///     structured payloads.
/// </summary>
public static class LlmAgentSchemaCompletion
{
    /// <summary>
    ///     Calls <paramref name="completionClient" />; on <see cref="AgentResultSchemaViolationException" /> retries with remediation
    ///     text until attempts are exhausted or output validates.
    /// </summary>
    public static async Task<(string RawJson, AgentResult Parsed)> CompleteAsync(
        IAgentCompletionClient completionClient,
        IAgentResultParser resultParser,
        IOptionsMonitor<AgentSchemaRemediationOptions> remediationOptions,
        AgentType agentType,
        string runId,
        string taskId,
        string systemPrompt,
        string baseUserPrompt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(completionClient);
        ArgumentNullException.ThrowIfNull(resultParser);
        ArgumentNullException.ThrowIfNull(remediationOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt);

        int maxAttempts = remediationOptions.CurrentValue.MaxCompletionAttempts;

        if (maxAttempts < 1)
            maxAttempts = 1;

        if (maxAttempts > AgentSchemaRemediationOptions.MaxCompletionAttemptsCeiling)
            maxAttempts = AgentSchemaRemediationOptions.MaxCompletionAttemptsCeiling;

        AgentResultSchemaViolationException? lastViolation = null;

        for (int attemptIndex = 0; attemptIndex < maxAttempts; attemptIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string userPrompt = BuildUserPrompt(baseUserPrompt, lastViolation);

            string rawJson = await completionClient
                .CompleteJsonAsync(systemPrompt, userPrompt, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                AgentResult parsed = resultParser.ParseAndValidate(rawJson, runId, taskId, agentType);

                return (rawJson, parsed);
            }
            catch (AgentResultSchemaViolationException ex)
            {
                bool moreAttemptsRemain = attemptIndex < maxAttempts - 1;

                if (!moreAttemptsRemain)
                    throw;


                ArchLucidInstrumentation.RecordAgentSchemaRemediationRetry(agentType.ToString());

                lastViolation = ex;
            }
        }

        throw new InvalidOperationException(
            $"Unexpected exit from agent schema completion loop ({agentType}, maxAttempts={maxAttempts}).");
    }

    private static string BuildUserPrompt(string baseUserPrompt, AgentResultSchemaViolationException? violation)
    {
        if (violation is null)
            return baseUserPrompt;


        IEnumerable<string> lines = violation.SchemaErrors.Select(static e =>
            "- " + e.Trim());

        return $"{baseUserPrompt.TrimEnd()}\n\nRemediation: Correct the JSON ONLY. Previous output failed validation.\n{string.Join("\n", lines)}";
    }
}

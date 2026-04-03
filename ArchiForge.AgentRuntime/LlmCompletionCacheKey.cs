using System.Security.Cryptography;
using System.Text;

using ArchiForge.Core.Scoping;

namespace ArchiForge.AgentRuntime;

/// <summary>
/// Builds a stable cache key for LLM JSON completion requests.
/// </summary>
public static class LlmCompletionCacheKey
{
    /// <summary>
    /// SHA-256 hex digest over deployment name, prompts, and optional scope partition.
    /// </summary>
    public static string Compute(
        bool partitionByScope,
        string deploymentName,
        string systemPrompt,
        string userPrompt,
        ScopeContext scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentName);
        ArgumentNullException.ThrowIfNull(systemPrompt);
        ArgumentNullException.ThrowIfNull(userPrompt);
        ArgumentNullException.ThrowIfNull(scope);

        string scopePart = string.Empty;

        if (partitionByScope)
        {
            scopePart =
                $"{scope.TenantId:N}|{scope.WorkspaceId:N}|{scope.ProjectId:N}|";
        }

        string payload =
            scopePart
            + deploymentName
            + '\0'
            + systemPrompt
            + '\0'
            + userPrompt;

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));

        return Convert.ToHexString(hash);
    }
}

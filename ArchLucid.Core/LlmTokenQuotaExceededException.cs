namespace ArchLucid.Core;

/// <summary>Thrown when a tenant exceeds configured LLM token quota for the current window or UTC day.</summary>
public sealed class LlmTokenQuotaExceededException(string message) : InvalidOperationException(message);

using System.Text.Json.Serialization;

namespace ArchLucid.Cli;

/// <summary>HTTP retry policy for <see cref="CliRetryDelegatingHandler"/> (CLI outbound API calls).</summary>
public sealed class CliResilienceOptions
{
    public const int DefaultMaxRetryAttempts = 3;

    public const int DefaultInitialDelaySeconds = 1;

    /// <summary>Polly retry attempts after the first try (0 disables retries).</summary>
    [JsonPropertyName("maxRetryAttempts")]
    public int MaxRetryAttempts { get; set; } = DefaultMaxRetryAttempts;

    /// <summary>Initial delay before exponential backoff with jitter.</summary>
    [JsonPropertyName("initialDelaySeconds")]
    public int InitialDelaySeconds { get; set; } = DefaultInitialDelaySeconds;

    /// <summary>Clamps values to safe ranges.</summary>
    public void Normalize()
    {
        MaxRetryAttempts = Math.Clamp(MaxRetryAttempts, 0, 10);
        InitialDelaySeconds = Math.Clamp(InitialDelaySeconds, 0, 300);
    }

    /// <summary>Merges optional JSON config into defaults.</summary>
    public static CliResilienceOptions FromCliConfig(ArchLucidProjectScaffolder.ArchLucidCliConfig? config)
    {
        CliResilienceOptions result = new();
        ArchLucidProjectScaffolder.CliHttpResilienceConfig? section = config?.HttpResilience;
        if (section is null)
        {
            result.Normalize();

            return result;
        }

        if (section.MaxRetryAttempts is { } maxRetries)
        {
            result.MaxRetryAttempts = maxRetries;
        }

        if (section.InitialDelaySeconds is { } delaySec)
        {
            result.InitialDelaySeconds = delaySec;
        }

        result.Normalize();

        return result;
    }
}

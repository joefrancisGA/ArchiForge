namespace ArchLucid.Persistence.Connections;

/// <summary>Configuration for SQL connection open retries in <see cref="ResilientSqlConnectionFactory" />.</summary>
public sealed class SqlOpenResilienceOptions
{
    public const string SectionName = "Persistence:SqlOpenResilience";

    /// <summary>Polly retry attempts after the first open attempt (0 disables retries).</summary>
    public int MaxRetryAttempts
    {
        get;
        set;
    } = 3;

    /// <summary>Base delay for exponential backoff with jitter between retries.</summary>
    public int BaseDelayMilliseconds
    {
        get;
        set;
    } = 200;

    /// <summary>Clamps values to safe ranges before building the pipeline.</summary>
    public void Normalize()
    {
        MaxRetryAttempts = Math.Clamp(MaxRetryAttempts, 0, 32);
        BaseDelayMilliseconds = Math.Clamp(BaseDelayMilliseconds, 1, 120_000);
    }
}

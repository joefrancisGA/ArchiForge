namespace ArchiForge.Api.Configuration;

/// <summary>Background export jobs: in-process channel vs durable SQL + Azure Storage Queue + worker.</summary>
public sealed class BackgroundJobsOptions
{
    public const string SectionName = "BackgroundJobs";

    /// <summary><c>InMemory</c> (default) or <c>Durable</c>.</summary>
    public string Mode { get; set; } = "InMemory";

    public string QueueName { get; set; } = "archiforge-export-jobs";

    /// <summary>Queue service URI, e.g. <c>https://{account}.queue.core.windows.net</c>. When empty, derived from <see cref="ArtifactLargePayloadOptions.AzureBlobServiceUri"/>.</summary>
    public string? QueueServiceUri { get; set; }

    public string ResultsContainerName { get; set; } = "background-job-results";

    public int MaxPendingJobs { get; set; } = 500;

    public int ProcessorVisibilityMinutes { get; set; } = 15;

    public int ProcessorIdlePollMilliseconds { get; set; } = 750;
}

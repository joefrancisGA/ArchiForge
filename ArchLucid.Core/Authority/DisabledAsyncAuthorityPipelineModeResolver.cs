namespace ArchiForge.Core.Authority;

/// <summary>Never queues authority stages (used for <c>InMemory</c> storage and tests).</summary>
public sealed class DisabledAsyncAuthorityPipelineModeResolver : IAsyncAuthorityPipelineModeResolver
{
    /// <inheritdoc />
    public Task<bool> ShouldQueueContextAndGraphStagesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}

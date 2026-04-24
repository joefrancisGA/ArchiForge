namespace ArchLucid.Persistence.Orchestration;

/// <summary>Drains <see cref="IAuthorityPipelineWorkRepository" /> and completes deferred authority runs.</summary>
public interface IAuthorityPipelineWorkProcessor
{
    Task ProcessPendingBatchAsync(CancellationToken cancellationToken = default);
}

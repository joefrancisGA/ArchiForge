namespace ArchiForge.Persistence.Orchestration.Pipeline;

/// <summary>
/// Runs authority pipeline steps after the <see cref="Models.RunRecord"/> row exists (context ingestion through artifact persistence).
/// </summary>
public interface IAuthorityPipelineStagesExecutor
{
    /// <summary>
    /// Executes context ingestion, graph, findings, decisioning, and artifact synthesis inside <see cref="AuthorityPipelineContext.UnitOfWork"/>.
    /// </summary>
    Task ExecuteAfterRunPersistedAsync(AuthorityPipelineContext context, CancellationToken cancellationToken);
}

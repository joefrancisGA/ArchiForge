namespace ArchiForge.ContextIngestion.Interfaces;

using Models;

public interface IContextIngestionService
{
    Task<ContextSnapshot> IngestAsync(
        ContextIngestionRequest request,
        CancellationToken ct);
}


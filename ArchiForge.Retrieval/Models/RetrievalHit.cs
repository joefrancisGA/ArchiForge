namespace ArchiForge.Retrieval.Models;

public class RetrievalHit
{
    public string ChunkId { get; set; } = default!;
    public string DocumentId { get; set; } = default!;
    public string SourceType { get; set; } = default!;
    public string SourceId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Text { get; set; } = default!;
    public double Score { get; set; }
}

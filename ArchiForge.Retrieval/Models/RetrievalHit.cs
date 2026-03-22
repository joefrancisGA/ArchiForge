namespace ArchiForge.Retrieval.Models;

public class RetrievalHit
{
    public string ChunkId { get; set; } = null!;
    public string DocumentId { get; set; } = null!;
    public string SourceType { get; set; } = null!;
    public string SourceId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Text { get; set; } = null!;
    public double Score { get; set; }
}

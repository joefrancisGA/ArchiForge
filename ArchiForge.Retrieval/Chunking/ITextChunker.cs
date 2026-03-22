namespace ArchiForge.Retrieval.Chunking;

public interface ITextChunker
{
    IReadOnlyList<string> Chunk(string text, int maxChars = 1200, int overlap = 150);
}

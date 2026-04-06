namespace ArchiForge.Retrieval.Chunking;

/// <summary>
/// Splits long document text into overlapping windows before embedding in <see cref="ArchiForge.Retrieval.Indexing.RetrievalIndexingService"/>.
/// </summary>
public interface ITextChunker
{
    /// <summary>
    /// Returns non-empty trimmed slices of <paramref name="text"/>; empty or whitespace input yields an empty list.
    /// </summary>
    /// <param name="text">Full document content.</param>
    /// <param name="maxChars">Maximum characters per chunk (excluding trim).</param>
    /// <param name="overlap">Characters reused between consecutive windows (<paramref name="maxChars"/> − <paramref name="overlap"/> advance step).</param>
    /// <returns>Trimmed, non-empty text slices; empty list when <paramref name="text"/> is blank.</returns>
    IReadOnlyList<string> Chunk(string text, int maxChars = 1200, int overlap = 150);
}

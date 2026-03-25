namespace ArchiForge.Retrieval.Chunking;

/// <summary>
/// Character-window chunker with fixed stride <c>max(1, maxChars − overlap)</c>; no sentence awareness.
/// </summary>
public sealed class SimpleTextChunker : ITextChunker
{
    /// <inheritdoc />
    public IReadOnlyList<string> Chunk(string text, int maxChars = 1200, int overlap = 150)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        List<string> chunks = new List<string>();
        int index = 0;

        while (index < text.Length)
        {
            int length = Math.Min(maxChars, text.Length - index);
            string chunk = text.Substring(index, length).Trim();

            if (!string.IsNullOrWhiteSpace(chunk))
                chunks.Add(chunk);

            if (index + length >= text.Length)
                break;

            index += Math.Max(1, maxChars - overlap);
        }

        return chunks;
    }
}

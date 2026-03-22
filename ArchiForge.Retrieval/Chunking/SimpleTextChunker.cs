namespace ArchiForge.Retrieval.Chunking;

public sealed class SimpleTextChunker : ITextChunker
{
    public IReadOnlyList<string> Chunk(string text, int maxChars = 1200, int overlap = 150)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var chunks = new List<string>();
        var index = 0;

        while (index < text.Length)
        {
            var length = Math.Min(maxChars, text.Length - index);
            var chunk = text.Substring(index, length).Trim();

            if (!string.IsNullOrWhiteSpace(chunk))
                chunks.Add(chunk);

            if (index + length >= text.Length)
                break;

            index += Math.Max(1, maxChars - overlap);
        }

        return chunks;
    }
}

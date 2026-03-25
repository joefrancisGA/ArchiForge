using ArchiForge.ContextIngestion.Contracts;
using ArchiForge.ContextIngestion.Models;

using static global::ArchiForge.ContextIngestion.SupportedContextDocumentContentTypes;

namespace ArchiForge.ContextIngestion.Parsing;

public class PlainTextContextDocumentParser : IContextDocumentParser
{
    public bool CanParse(string contentType) => IsSupported(contentType);

    public Task<IReadOnlyList<CanonicalObject>> ParseAsync(
        ContextDocumentReference document,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);
        _ = ct;
        List<CanonicalObject> results = new();

        string[] lines = (document.Content ?? string.Empty)
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string line in lines)
        {
            if (line.StartsWith("REQ:", StringComparison.OrdinalIgnoreCase))
            {
                string text = line.Substring(4).Trim();

                results.Add(new CanonicalObject
                {
                    ObjectType = "Requirement",
                    Name = text.Length > 80 ? text[..80] : text,
                    SourceType = "Document",
                    SourceId = document.DocumentId,
                    Properties = new Dictionary<string, string>
                    {
                        ["text"] = text
                    }
                });
            }
            else if (line.StartsWith("POL:", StringComparison.OrdinalIgnoreCase))
            {
                string text = line.Substring(4).Trim();

                results.Add(new CanonicalObject
                {
                    ObjectType = "PolicyControl",
                    Name = text.Length > 80 ? text[..80] : text,
                    SourceType = "Document",
                    SourceId = document.DocumentId,
                    Properties = new Dictionary<string, string>
                    {
                        ["text"] = text
                    }
                });
            }
            else if (line.StartsWith("TOP:", StringComparison.OrdinalIgnoreCase))
            {
                string text = line.Substring(4).Trim();

                results.Add(new CanonicalObject
                {
                    ObjectType = "TopologyResource",
                    Name = text.Length > 80 ? text[..80] : text,
                    SourceType = "Document",
                    SourceId = document.DocumentId,
                    Properties = new Dictionary<string, string>
                    {
                        ["text"] = text
                    }
                });
            }
            else if (line.StartsWith("SEC:", StringComparison.OrdinalIgnoreCase))
            {
                string text = line.Substring(4).Trim();

                results.Add(new CanonicalObject
                {
                    ObjectType = "SecurityBaseline",
                    Name = text.Length > 80 ? text[..80] : text,
                    SourceType = "Document",
                    SourceId = document.DocumentId,
                    Properties = new Dictionary<string, string>
                    {
                        ["text"] = text,
                        ["status"] = "declared"
                    }
                });
            }
        }

        return Task.FromResult<IReadOnlyList<CanonicalObject>>(results);
    }
}

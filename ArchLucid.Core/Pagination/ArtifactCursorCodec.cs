using System.Text.Json;

namespace ArchLucid.Core.Pagination;

/// <summary>Opaque cursors for artifact metadata rows (<c>SortOrder ASC, ArtifactId ASC</c>).</summary>
public static class ArtifactCursorCodec
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string Encode(int sortOrder, Guid artifactId)
    {
        ArtifactListCursorDto dto = new() { So = sortOrder, Ai = artifactId };
        byte[] utf8 = JsonSerializer.SerializeToUtf8Bytes(dto, SerializerOptions);
        return Base64UrlEncode(utf8);
    }

    public static (int SortOrder, Guid ArtifactId)? TryDecode(string? encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
            return null;

        byte[] bytes = Base64UrlDecode(encoded.Trim());
        ArtifactListCursorDto? dto = JsonSerializer.Deserialize<ArtifactListCursorDto>(bytes, SerializerOptions);

        if (dto is null || dto.Ai == Guid.Empty)
            return null;

        return (dto.So, dto.Ai);
    }

    private static string Base64UrlEncode(byte[] utf8Bytes)
    {
        string b64 = Convert.ToBase64String(utf8Bytes);
        return b64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string b64Url)
    {
        string padded = b64Url.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
        }

        return Convert.FromBase64String(padded);
    }

    private sealed class ArtifactListCursorDto
    {
        public int So { get; set; }

        public Guid Ai { get; set; }
    }
}

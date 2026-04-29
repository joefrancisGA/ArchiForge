using System.Globalization;
using System.Text.Json;

namespace ArchLucid.Core.Pagination;

/// <summary>Opaque cursors for findings keyset pagination (<c>SortOrder ASC, FindingRecordId ASC</c>).</summary>
public static class FindingCursorCodec
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Encodes the cursor after the last item on the previous page.</summary>
    public static string Encode(int sortOrder, Guid findingRecordId)
    {
        FindingListCursorDto dto = new() { So = sortOrder, Fri = findingRecordId };
        byte[] utf8 = JsonSerializer.SerializeToUtf8Bytes(dto, SerializerOptions);
        return Base64UrlEncode(utf8);
    }

    public static (int SortOrder, Guid FindingRecordId)? TryDecode(string? encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
            return null;

        byte[] bytes = Base64UrlDecode(encoded.Trim());
        FindingListCursorDto? dto = JsonSerializer.Deserialize<FindingListCursorDto>(bytes, SerializerOptions);

        if (dto is null || dto.Fri == Guid.Empty)
            return null;

        return (dto.So, dto.Fri);
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

    private sealed class FindingListCursorDto
    {
        public int So { get; set; }

        public Guid Fri { get; set; }
    }
}

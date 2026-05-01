using System.Globalization;
using System.Text.Json;

namespace ArchLucid.Core.Pagination;

/// <summary>
///     Encodes/decodes opaque run-list cursors (<see cref="RunListCursorDto" /> as Base64-url JSON).
/// </summary>
public static class RunCursorCodec
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Opaque Base64-url encoded cursor for subsequent keyset reads.</summary>
    public static string Encode(DateTime createdUtc, Guid runId)
    {
        RunListCursorDto dto = new() { Cu = FormatRoundTrip(createdUtc), Ri = runId };
        byte[] utf8 = JsonSerializer.SerializeToUtf8Bytes(dto, SerializerOptions);

        return Base64UrlEncode(utf8);
    }

    /// <summary>Returns <see langword="null" /> when <paramref name="encoded" /> is null/whitespace or invalid.</summary>
    public static (DateTime CreatedUtc, Guid RunId)? TryDecode(string? encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
            return null;

        byte[] bytes = Base64UrlDecode(encoded.Trim());
        RunListCursorDto? dto =
            JsonSerializer.Deserialize<RunListCursorDto>(bytes, SerializerOptions);

        if (dto is null || string.IsNullOrWhiteSpace(dto.Cu) || dto.Ri == Guid.Empty)

            return null;

        if (!DateTime.TryParse(dto.Cu, null, DateTimeStyles.RoundtripKind, out DateTime createdUtc))

            return null;

        return (NormalizeDateTimeUtc(createdUtc), dto.Ri);
    }

    private static string FormatRoundTrip(DateTime dt)
    {
        return DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToString("o");
    }

    private static DateTime NormalizeDateTimeUtc(DateTime dt) =>
        dt.Kind is DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt.ToUniversalTime(), DateTimeKind.Utc);

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

    private sealed class RunListCursorDto
    {
        public string Cu
        {
            get;
            set;
        } = "";

        public Guid Ri
        {
            get;
            set;
        }
    }
}

using System.Globalization;
using System.Text.Json;

namespace ArchLucid.Core.Pagination;

/// <summary>Opaque cursors for audit search (<c>OccurredUtc DESC, EventId DESC</c>).</summary>
public static class AuditEventCursorCodec
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string Encode(DateTime occurredUtc, Guid eventId)
    {
        AuditListCursorDto dto = new() { Ou = FormatRoundTrip(occurredUtc), Ei = eventId };
        byte[] utf8 = JsonSerializer.SerializeToUtf8Bytes(dto, SerializerOptions);
        return Base64UrlEncode(utf8);
    }

    public static (DateTime OccurredUtc, Guid EventId)? TryDecode(string? encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
            return null;

        byte[] bytes = Base64UrlDecode(encoded.Trim());
        AuditListCursorDto? dto = JsonSerializer.Deserialize<AuditListCursorDto>(bytes, SerializerOptions);

        if (dto is null || string.IsNullOrWhiteSpace(dto.Ou) || dto.Ei == Guid.Empty)
            return null;

        if (!DateTime.TryParse(dto.Ou, null, DateTimeStyles.RoundtripKind, out DateTime oc))
            return null;

        return (Normalize(oc), dto.Ei);
    }

    private static string FormatRoundTrip(DateTime dt) =>
        DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToString("o");

    private static DateTime Normalize(DateTime dt) =>
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

    private sealed class AuditListCursorDto
    {
        public string Ou
        {
            get;
            set;
        } = "";

        public Guid Ei
        {
            get;
            set;
        }
    }
}

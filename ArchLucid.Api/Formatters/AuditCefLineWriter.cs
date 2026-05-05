using System.Globalization;
using System.Text;

using ArchLucid.Core.Audit;

namespace ArchLucid.Api.Formatters;

/// <summary>
///     Builds ArcSight CEF 1.0 lines for SIEM export (one event per line).
/// </summary>
public static class AuditCefLineWriter
{
    private const string HeaderPrefix = "CEF:0|ArchLucid|ArchLucid API|1.0|";

    public static async Task WriteAllAsync(Stream stream, IEnumerable<AuditEvent> events, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(events);

        await using StreamWriter writer = new(stream, Encoding.UTF8, 16_384, leaveOpen: true);
        writer.NewLine = "\n";

        foreach (AuditEvent e in events)
        {
            ArgumentNullException.ThrowIfNull(e);

            string line = BuildLine(e);
            await writer.WriteLineAsync(line).ConfigureAwait(false);
        }

        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    internal static string BuildLine(AuditEvent e)
    {
        string signature = EscapeCefHeaderField(e.EventType);
        string name = EscapeCefHeaderField(e.EventType);
        int severity = 3;
        string ext = BuildExtension(e);

        return $"{HeaderPrefix}{signature}|{name}|{severity}|{ext}";
    }

    private static string BuildExtension(AuditEvent e)
    {
        List<string> parts =
        [
            $"eventId={EscapeCefExtensionValue(e.EventId.ToString("D", CultureInfo.InvariantCulture))}",
            $"rt={EscapeCefExtensionValue(FormatRt(e.OccurredUtc))}",
            $"correlationId={EscapeCefExtensionValue(e.CorrelationId ?? string.Empty)}",
            $"actorUserId={EscapeCefExtensionValue(e.ActorUserId ?? string.Empty)}",
            $"actorUserName={EscapeCefExtensionValue(e.ActorUserName ?? string.Empty)}",
            $"runId={EscapeCefExtensionValue(e.RunId?.ToString("D", CultureInfo.InvariantCulture) ?? string.Empty)}",
            $"manifestId={EscapeCefExtensionValue(e.ManifestId?.ToString("D", CultureInfo.InvariantCulture) ?? string.Empty)}",
            $"msg={EscapeCefExtensionValue(TruncateDataJson(e.DataJson))}"
        ];

        return string.Join(' ', parts);
    }

    private static string TruncateDataJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return string.Empty;

        const int max = 2048;

        return json.Length <= max ? json : json[..max] + "…";
    }

    private static string FormatRt(DateTime occurredUtc)
    {
        DateTime utc = occurredUtc.Kind switch
        {
            DateTimeKind.Utc => occurredUtc,
            DateTimeKind.Local => occurredUtc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(occurredUtc, DateTimeKind.Utc)
        };

        long ms = new DateTimeOffset(utc).ToUnixTimeMilliseconds();

        return ms.ToString(CultureInfo.InvariantCulture);
    }

    private static string EscapeCefHeaderField(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("|", "\\|", StringComparison.Ordinal);
    }

    private static string EscapeCefExtensionValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("=", "\\=", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }
}

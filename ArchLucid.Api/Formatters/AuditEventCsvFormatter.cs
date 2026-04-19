using System.Globalization;
using System.Text;

using ArchLucid.Core.Audit;

using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace ArchLucid.Api.Formatters;

/// <summary>
/// Serializes <see cref="AuditEvent"/> collections as RFC 4180-style CSV (<c>text/csv</c>) for audit exports.
/// </summary>
public sealed class AuditEventCsvFormatter : TextOutputFormatter
{
    /// <summary>Populated by <c>AuditController.ExportAudit</c> so CSV responses can emit <c>Content-Disposition</c>.</summary>
    public const string CsvAttachmentFileNameItemKey = "ArchLucid.AuditExport.CsvAttachmentFileName";

    private const string HeaderLine =
        "EventId,OccurredUtc,EventType,ActorUserId,ActorUserName,RunId,ManifestId,CorrelationId,DataJson";

    public AuditEventCsvFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/csv"));
        SupportedEncodings.Add(Encoding.UTF8);
    }

    protected override bool CanWriteType(Type? type)
    {
        if (type is null)
            return false;


        if (type == typeof(string))
            return false;


        return typeof(IEnumerable<AuditEvent>).IsAssignableFrom(type);
    }

    public override async Task WriteResponseBodyAsync(
        OutputFormatterWriteContext context,
        Encoding selectedEncoding)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(selectedEncoding);

        if (context.Object is not IEnumerable<AuditEvent> events)
            throw new InvalidOperationException(
                $"{nameof(AuditEventCsvFormatter)} expected {nameof(IEnumerable<AuditEvent>)}.");


        if (context.HttpContext.Items.TryGetValue(CsvAttachmentFileNameItemKey, out object? nameObj)
            && nameObj is string fileName
            && !string.IsNullOrWhiteSpace(fileName))
        {
            ContentDispositionHeaderValue disposition = new("attachment")
            {
                FileName = fileName,
            };
            context.HttpContext.Response.Headers.ContentDisposition = disposition.ToString();
        }

        Stream responseStream = context.HttpContext.Response.Body;
        await using StreamWriter writer = new(responseStream, selectedEncoding, bufferSize: 16_384, leaveOpen: true)
        {
            NewLine = "\n",
        };

        await writer.WriteLineAsync(HeaderLine);


        foreach (AuditEvent auditEvent in events)
        {
            ArgumentNullException.ThrowIfNull(auditEvent);

            string line = string.Join(
                ',',
                EscapeCsvField(auditEvent.EventId.ToString("D", CultureInfo.InvariantCulture)),
                EscapeCsvField(FormatOccurredUtc(auditEvent.OccurredUtc)),
                EscapeCsvField(auditEvent.EventType),
                EscapeCsvField(auditEvent.ActorUserId),
                EscapeCsvField(auditEvent.ActorUserName),
                EscapeCsvField(FormatNullableGuid(auditEvent.RunId)),
                EscapeCsvField(FormatNullableGuid(auditEvent.ManifestId)),
                EscapeCsvField(auditEvent.CorrelationId),
                EscapeCsvField(auditEvent.DataJson));

            await writer.WriteLineAsync(line);
        }

        await writer.FlushAsync();
    }

    private static string FormatOccurredUtc(DateTime occurredUtc)
    {
        DateTime utc = occurredUtc.Kind switch
        {
            DateTimeKind.Utc => occurredUtc,
            DateTimeKind.Local => occurredUtc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(occurredUtc, DateTimeKind.Utc),
        };

        return utc.ToString("O", CultureInfo.InvariantCulture);
    }

    private static string FormatNullableGuid(Guid? value)
    {
        if (!value.HasValue)
            return string.Empty;


        return value.Value.ToString("D", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// RFC 4180-style escaping: double quotes around fields that contain comma, quote, or newline; quotes doubled.
    /// </summary>
    internal static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;


        bool mustQuote =
            value.Contains(',')
            || value.Contains('"')
            || value.Contains('\r')
            || value.Contains('\n');

        if (!mustQuote)
            return value;


        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}

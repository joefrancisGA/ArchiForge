using System.Globalization;
using System.Text;

using ArchLucid.Api.Formatters;
using ArchLucid.Core.Audit;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace ArchLucid.Api.Tests;

public sealed class AuditEventCsvFormatterTests
{
    [Fact]
    public async Task WriteResponseBodyAsync_WritesHeaderRow()
    {
        AuditEventCsvFormatter formatter = new();
        DefaultHttpContext httpContext = new();
        MemoryStream stream = new();
        httpContext.Response.Body = stream;
        List<AuditEvent> events = [];

        OutputFormatterWriteContext context = new(
            httpContext,
            (s, enc) => new StreamWriter(s, enc, leaveOpen: true),
            typeof(List<AuditEvent>),
            events);

        await formatter.WriteResponseBodyAsync(context, Encoding.UTF8);

        stream.Position = 0;
        using StreamReader reader = new(stream, Encoding.UTF8, leaveOpen: true);
        string text = await reader.ReadToEndAsync();

        string firstLine = text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];
        firstLine.Should().Be(
            "EventId,OccurredUtc,EventType,ActorUserId,ActorUserName,RunId,ManifestId,CorrelationId,DataJson");
    }

    [Fact]
    public async Task WriteResponseBodyAsync_WritesIso8601Utc_ForOccurredUtc()
    {
        AuditEventCsvFormatter formatter = new();
        DefaultHttpContext httpContext = new();
        MemoryStream stream = new();
        httpContext.Response.Body = stream;

        DateTime occurred = new(2026, 3, 15, 14, 30, 45, 123, DateTimeKind.Utc);
        List<AuditEvent> events =
        [
            new()
            {
                EventType = "T",
                ActorUserId = "u",
                ActorUserName = "U",
                TenantId = Guid.Empty,
                WorkspaceId = Guid.Empty,
                ProjectId = Guid.Empty,
                OccurredUtc = occurred,
                DataJson = "{}"
            }
        ];

        OutputFormatterWriteContext context = new(
            httpContext,
            (s, enc) => new StreamWriter(s, enc, leaveOpen: true),
            typeof(List<AuditEvent>),
            events);

        await formatter.WriteResponseBodyAsync(context, Encoding.UTF8);

        stream.Position = 0;
        using StreamReader reader = new(stream, Encoding.UTF8, leaveOpen: true);
        string text = await reader.ReadToEndAsync();

        string[] lines = text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCountGreaterThanOrEqualTo(2);
        string dataLine = lines[1];
        string[] cols = SplitCsvLine(dataLine);
        cols[1].Should().Be("2026-03-15T14:30:45.1230000Z");
        DateTime parsed = DateTime.Parse(cols[1], null, DateTimeStyles.RoundtripKind);
        parsed.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [InlineData("line1\rline2", "\"line1\rline2\"")]
    [InlineData("x\ny", "\"x\ny\"")]
    public void EscapeCsvField_EscapesSpecialCharacters(string input, string expected)
    {
        AuditEventCsvFormatter.EscapeCsvField(input).Should().Be(expected);
    }

    /// <summary>Minimal CSV split for single-line test rows (no embedded newlines in fields for this helper).</summary>
    private static string[] SplitCsvLine(string line)
    {
        List<string> fields = [];
        StringBuilder current = new();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        fields.Add(current.ToString());

        return fields.ToArray();
    }
}

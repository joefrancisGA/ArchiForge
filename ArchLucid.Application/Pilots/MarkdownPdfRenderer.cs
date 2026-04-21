using System.Text.RegularExpressions;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ArchLucid.Application.Pilots;

/// <summary>
/// Minimal Markdown→QuestPDF renderer scoped to the subset emitted by <see cref="FirstValueReportBuilder"/>:
/// ATX headings (#, ##, ###), GFM pipe tables, horizontal rules, unordered/ordered lists, paragraphs,
/// inline <c>**bold**</c>, <c>_italic_</c>, <c>`code`</c>, and <c>[text](url)</c> links.
/// Lives next to the builder so the PDF endpoint cannot drift from the canonical Markdown body.
/// </summary>
internal static class MarkdownPdfRenderer
{
    private static readonly Regex InlinePattern = new(
        @"(?<bold>\*\*[^*]+\*\*)|(?<italic>(?<![A-Za-z0-9_])_[^_]+_(?![A-Za-z0-9_]))|(?<code>`[^`]+`)|(?<link>\[[^\]]+\]\([^)]+\))",
        RegexOptions.Compiled);

    /// <summary>Renders <paramref name="markdown"/> into the supplied QuestPDF <paramref name="column"/>.</summary>
    public static void Render(ColumnDescriptor column, string markdown)
    {
        if (column is null) throw new ArgumentNullException(nameof(column));
        if (markdown is null) throw new ArgumentNullException(nameof(markdown));

        IReadOnlyList<string> lines = NormalizeLines(markdown);
        int index = 0;

        while (index < lines.Count)
        {
            string line = lines[index];

            if (string.IsNullOrWhiteSpace(line))
            {
                index++;

                continue;
            }

            if (IsHorizontalRule(line))
            {
                column.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                index++;

                continue;
            }

            if (TryParseHeading(line, out int level, out string headingText))
            {
                RenderHeading(column, level, headingText);
                index++;

                continue;
            }

            if (IsTableHeader(line, lines, index))
            {
                int consumed = RenderTable(column, lines, index);
                index += consumed;

                continue;
            }

            if (IsListItem(line))
            {
                int consumed = RenderList(column, lines, index);
                index += consumed;

                continue;
            }

            int paragraphConsumed = RenderParagraph(column, lines, index);
            index += paragraphConsumed;
        }
    }

    private static IReadOnlyList<string> NormalizeLines(string markdown) =>
        markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

    private static bool IsHorizontalRule(string line) => line.Trim() is "---" or "***" or "___";

    private static bool TryParseHeading(string line, out int level, out string text)
    {
        level = 0;
        text = string.Empty;
        string trimmed = line.TrimStart();

        while (level < trimmed.Length && trimmed[level] == '#' && level < 6)
            level++;


        if (level == 0 || level >= trimmed.Length || trimmed[level] != ' ')
        {
            level = 0;

            return false;
        }

        text = trimmed[(level + 1)..].Trim();

        return text.Length > 0;
    }

    private static void RenderHeading(ColumnDescriptor column, int level, string text)
    {
        float size = level switch
        {
            1 => 16f,
            2 => 13f,
            3 => 11f,
            _ => 10f
        };

        float topPadding = level == 1 ? 0f : 8f;
        column.Item().PaddingTop(topPadding).Text(t => RenderInline(t, text, baseSize: size, bold: true));
    }

    private static bool IsListItem(string line)
    {
        string t = line.TrimStart();

        if (t.StartsWith("- ", StringComparison.Ordinal) || t.StartsWith("* ", StringComparison.Ordinal))
            return true;

        return Regex.IsMatch(t, @"^\d+\.\s");
    }

    private static int RenderList(ColumnDescriptor column, IReadOnlyList<string> lines, int start)
    {
        int i = start;

        while (i < lines.Count && IsListItem(lines[i]))
        {
            string raw = lines[i].TrimStart();
            string itemText = raw.StartsWith("- ", StringComparison.Ordinal) || raw.StartsWith("* ", StringComparison.Ordinal)
                ? raw[2..]
                : Regex.Replace(raw, @"^\d+\.\s+", string.Empty);

            column.Item().PaddingLeft(8).Row(row =>
            {
                row.ConstantItem(10).Text("•");
                row.RelativeItem().Text(t => RenderInline(t, itemText));
            });
            i++;
        }

        return i - start;
    }

    private static bool IsTableHeader(string line, IReadOnlyList<string> lines, int index)
    {
        if (!IsTableRow(line)) return false;
        if (index + 1 >= lines.Count) return false;

        return IsTableSeparator(lines[index + 1]);
    }

    private static bool IsTableRow(string line)
    {
        string t = line.TrimStart();

        return t.StartsWith('|') && t.Contains('|', StringComparison.Ordinal);
    }

    private static bool IsTableSeparator(string line)
    {
        string t = line.Trim();

        if (!t.StartsWith('|')) return false;
        IEnumerable<string> cells = SplitRow(t).Select(c => c.Trim());

        return cells.All(c => c.Length > 0 && c.Replace(":", string.Empty, StringComparison.Ordinal).All(ch => ch == '-'));
    }

    private static List<string> SplitRow(string line)
    {
        string t = line.Trim();

        if (t.StartsWith('|')) t = t[1..];
        if (t.EndsWith('|')) t = t[..^1];

        return t.Split('|').Select(c => c.Trim()).ToList();
    }

    private static int RenderTable(ColumnDescriptor column, IReadOnlyList<string> lines, int start)
    {
        List<string> headers = SplitRow(lines[start]);
        int columnsCount = headers.Count;
        int i = start + 2;
        List<List<string>> rows = [];

        while (i < lines.Count && IsTableRow(lines[i]))
        {
            List<string> cells = SplitRow(lines[i]);

            while (cells.Count < columnsCount) cells.Add(string.Empty);
            rows.Add(cells);
            i++;
        }

        column.Item().PaddingVertical(4).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                for (int c = 0; c < columnsCount; c++) cols.RelativeColumn();
            });

            foreach (string header in headers)
                table.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(t => RenderInline(t, header, bold: true));


            foreach (List<string> row in rows)
                foreach (string cell in row)
                    table.Cell().BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(t => RenderInline(t, cell));
        });

        return i - start;
    }

    private static int RenderParagraph(ColumnDescriptor column, IReadOnlyList<string> lines, int start)
    {
        int i = start;
        List<string> chunk = [];

        while (i < lines.Count
            && !string.IsNullOrWhiteSpace(lines[i])
            && !IsHorizontalRule(lines[i])
            && !TryParseHeading(lines[i], out _, out _)
            && !IsListItem(lines[i])
            && !IsTableRow(lines[i]))
        {
            chunk.Add(lines[i].TrimEnd());
            i++;
        }

        string joined = string.Join(' ', chunk).Trim();

        if (joined.Length > 0)
            column.Item().PaddingVertical(2).Text(t => RenderInline(t, joined));


        return Math.Max(1, i - start);
    }

    /// <summary>Emits inline-formatted spans into a QuestPDF text descriptor.</summary>
    private static void RenderInline(TextDescriptor text, string content, float baseSize = 10f, bool bold = false)
    {
        text.DefaultTextStyle(s => bold ? s.FontSize(baseSize).Bold() : s.FontSize(baseSize));
        int cursor = 0;

        foreach (Match m in InlinePattern.Matches(content).Cast<Match>())
        {
            if (m.Index > cursor)
                text.Span(content[cursor..m.Index]);


            string raw = m.Value;

            if (m.Groups["bold"].Success)
                text.Span(raw[2..^2]).Bold();
            else if (m.Groups["italic"].Success)
                text.Span(raw[1..^1]).Italic();
            else if (m.Groups["code"].Success)
                text.Span(raw[1..^1]).FontFamily("Courier New").BackgroundColor(Colors.Grey.Lighten4);
            else if (m.Groups["link"].Success)
            {
                int splitIndex = raw.IndexOf("](", StringComparison.Ordinal);
                string linkText = raw[1..splitIndex];
                string linkUrl = raw[(splitIndex + 2)..^1];
                text.Hyperlink(linkText, linkUrl).Underline().FontColor(Colors.Blue.Medium);
            }

            cursor = m.Index + m.Length;
        }

        if (cursor < content.Length)
            text.Span(content[cursor..]);
    }
}

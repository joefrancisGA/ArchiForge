using ArchiForge.Decisioning.Manifest.Sections;

using DocumentFormat.OpenXml.Wordprocessing;

namespace ArchiForge.ArtifactSynthesis.Docx.Builders;

public static class WordDocumentBuilder
{
    private static string Sanitize(string? text) =>
        text?.Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace('\r', ' ')
        ?? string.Empty;

    public static void AddParagraph(Body body, string text) =>
        body.AppendChild(new Paragraph(new Run(new Text(Sanitize(text)))));

    public static void AddStyledParagraph(Body body, string text, string styleId)
    {
        Paragraph p = new(
            new ParagraphProperties(new ParagraphStyleId { Val = styleId }),
            new Run(new Text(Sanitize(text))));
        body.AppendChild(p);
    }

    public static void AddHeading(Body body, string text, string styleId = DocxStyleIds.Heading1) =>
        AddStyledParagraph(body, text, styleId);

    public static void AddBodyText(Body body, string text) =>
        AddStyledParagraph(body, text, DocxStyleIds.BodyText);

    /// <summary>Splits on blank lines into styled body paragraphs.</summary>
    public static void AddMultilineBodyText(Body body, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            AddBodyText(body, string.Empty);
            return;
        }

        string[] blocks = text.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.None);
        foreach (string block in blocks)
        {
            string line = block.Trim();
            if (line.Length > 0)
                AddBodyText(body, line);
        }
    }

    public static void AddSpacer(Body body, int lines = 1)
    {
        for (int i = 0; i < lines; i++)
            body.AppendChild(new Paragraph(new Run(new Text(string.Empty))));
    }

    /// <summary>Simple bullets (Unicode bullet + Normal).</summary>
    public static void AddBulletList(Body body, IEnumerable<string> items)
    {
        foreach (string item in items)
            AddStyledParagraph(body, "\u2022 " + Sanitize(item), DocxStyleIds.BodyText);
    }

    public static void AddSimpleTable(Body body, IEnumerable<(string, string)> rows, bool headerRow = false)
    {
        List<(string, string)> list = rows.ToList();
        if (list.Count == 0)
            return;

        Table table = CreateTableGrid();
        if (headerRow)
        {
            TableCell[] headerCells =
            [
                CreateHeaderCell(list[0].Item1),
                CreateHeaderCell(list[0].Item2)
            ];
            table.AppendChild(new TableRow(headerCells[0], headerCells[1]));
            list = list.Skip(1).ToList();
        }

        foreach ((string, string) row in list)
        
            table.AppendChild(
                new TableRow(
                    new TableCell(new Paragraph(new Run(new Text(Sanitize(row.Item1))))),
                    new TableCell(new Paragraph(new Run(new Text(Sanitize(row.Item2)))))));
        

        body.AppendChild(table);
    }

    public static void AddThreeColumnTable(
        Body body,
        IReadOnlyList<(string C1, string C2, string C3)> rows,
        (string C1, string C2, string C3) header)
    {
        Table table = CreateTableGrid();
        table.AppendChild(
            new TableRow(
                CreateHeaderCell(header.C1),
                CreateHeaderCell(header.C2),
                CreateHeaderCell(header.C3)));

        foreach ((string c1, string c2, string c3) in rows)
        
            table.AppendChild(
                new TableRow(
                    new TableCell(new Paragraph(new Run(new Text(Sanitize(c1))))),
                    new TableCell(new Paragraph(new Run(new Text(Sanitize(c2))))),
                    new TableCell(new Paragraph(new Run(new Text(Sanitize(c3)))))));
        

        body.AppendChild(table);
    }

    public static void AddIssuesTable(Body body, IEnumerable<ManifestIssue> issues)
    {
        Table table = CreateTableGrid();
        table.AppendChild(
            new TableRow(
                CreateHeaderCell("Severity"),
                CreateHeaderCell("Title"),
                CreateHeaderCell("Description")));

        foreach (ManifestIssue issue in issues)
        {
            Run severityRun = new(new Text(Sanitize(issue.Severity)));
            if (IsHighSeverity(issue.Severity))
                severityRun.RunProperties = new RunProperties(new Bold(), new Color { Val = "C00000" });

            table.AppendChild(
                new TableRow(
                    new TableCell(new Paragraph(severityRun)),
                    new TableCell(new Paragraph(new Run(new Text(Sanitize(issue.Title))))),
                    new TableCell(new Paragraph(new Run(new Text(Sanitize(issue.Description)))))));
        }

        body.AppendChild(table);
    }

    public static void AddFourColumnTable(
        Body body,
        (string A, string B, string C, string D) header,
        IReadOnlyList<(string A, string B, string C, string D)> rows)
    {
        Table table = CreateTableGrid();
        table.AppendChild(
            new TableRow(
                CreateHeaderCell(header.A),
                CreateHeaderCell(header.B),
                CreateHeaderCell(header.C),
                CreateHeaderCell(header.D)));

        foreach ((string a, string b, string c, string d) in rows)
        
            table.AppendChild(
                new TableRow(
                    new TableCell(new Paragraph(new Run(new Text(Sanitize(a))))),
                    new TableCell(new Paragraph(new Run(new Text(Sanitize(b))))),
                    new TableCell(new Paragraph(new Run(new Text(Sanitize(c))))),
                    new TableCell(new Paragraph(new Run(new Text(Sanitize(d)))))));
        

        body.AppendChild(table);
    }

    private static bool IsHighSeverity(string severity)
    {
        string s = severity.ToUpperInvariant();
        return s.Contains("CRITICAL", StringComparison.Ordinal) ||
               s.Contains("HIGH", StringComparison.Ordinal) ||
               s.Contains("BLOCK", StringComparison.Ordinal);
    }

    private static Table CreateTableGrid()
    {
        Table table = new();
        table.AppendChild(
            new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 8 },
                    new BottomBorder { Val = BorderValues.Single, Size = 8 },
                    new LeftBorder { Val = BorderValues.Single, Size = 8 },
                    new RightBorder { Val = BorderValues.Single, Size = 8 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })));
        return table;
    }

    private static TableCell CreateHeaderCell(string text) =>
        new(
            new TableCellProperties(
                new Shading
                {
                    Val = ShadingPatternValues.Clear,
                    Color = "auto",
                    Fill = "D9D9D9"
                }),
            new Paragraph(
                new ParagraphProperties(new ParagraphStyleId { Val = DocxStyleIds.TableHeader }),
                new Run(
                    new RunProperties(new Bold()),
                    new Text(Sanitize(text)))));
}

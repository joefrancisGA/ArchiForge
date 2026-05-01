using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using DrBlip = DocumentFormat.OpenXml.Drawing.Blip;
using DrBlipFill = DocumentFormat.OpenXml.Drawing.Pictures.BlipFill;
using DrFillRectangle = DocumentFormat.OpenXml.Drawing.FillRectangle;
using DrGraphicFrameLocks = DocumentFormat.OpenXml.Drawing.GraphicFrameLocks;
using DrNonVisualDrawingProperties = DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties;
using DrNonVisualPictureDrawingProperties = DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties;
using DrNonVisualPictureProperties = DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties;
using DrPicture = DocumentFormat.OpenXml.Drawing.Pictures.Picture;
using DrShapeProperties = DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties;
using DrStretch = DocumentFormat.OpenXml.Drawing.Stretch;
using WpBottomBorder = DocumentFormat.OpenXml.Wordprocessing.BottomBorder;
using WpBreak = DocumentFormat.OpenXml.Wordprocessing.Break;
using WpInsideHorizontalBorder = DocumentFormat.OpenXml.Wordprocessing.InsideHorizontalBorder;
using WpInsideVerticalBorder = DocumentFormat.OpenXml.Wordprocessing.InsideVerticalBorder;
using WpLeftBorder = DocumentFormat.OpenXml.Wordprocessing.LeftBorder;
using WpNonVisualGraphicFrameDrawingProperties =
    DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties;
using WpParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using WpParagraphProperties = DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties;
using WpRightBorder = DocumentFormat.OpenXml.Wordprocessing.RightBorder;
using WpRun = DocumentFormat.OpenXml.Wordprocessing.Run;
using WpRunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using WpShading = DocumentFormat.OpenXml.Wordprocessing.Shading;
using WpSpacingBetweenLines = DocumentFormat.OpenXml.Wordprocessing.SpacingBetweenLines;
using WpTable = DocumentFormat.OpenXml.Wordprocessing.Table;
using WpTableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;
using WpTableCellProperties = DocumentFormat.OpenXml.Wordprocessing.TableCellProperties;
using WpTableCellWidth = DocumentFormat.OpenXml.Wordprocessing.TableCellWidth;
using WpTableProperties = DocumentFormat.OpenXml.Wordprocessing.TableProperties;
using WpTableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;
using WpText = DocumentFormat.OpenXml.Wordprocessing.Text;
using WpTopBorder = DocumentFormat.OpenXml.Wordprocessing.TopBorder;

namespace ArchLucid.Application.Analysis;

/// <summary>Low-level OpenXML paragraph, table, image, and style helpers for consulting DOCX export.</summary>
internal static class ConsultingDocxOpenXmlPrimitives
{
    internal const string MermaidLanguage = "mermaid";

    internal static void AddStylesPart(
        MainDocumentPart mainPart,
        ConsultingDocxTemplateOptions options)
    {
        StyleDefinitionsPart stylePart = mainPart.StyleDefinitionsPart ?? mainPart.AddNewPart<StyleDefinitionsPart>();
        stylePart.Styles = new Styles(
            BuildParagraphStyle("Title", "Title", options.PrimaryColorHex, "36"),
            BuildParagraphStyle("Subtitle", "Subtitle", options.SecondaryColorHex, "24"),
            BuildParagraphStyle("Strong", "Strong", options.BodyColorHex, "22", true),
            BuildParagraphStyle("Subtle", "Subtle", options.SubtleColorHex, "18"),
            BuildParagraphStyle("BodyText", "BodyText", options.BodyColorHex, "22"));
        stylePart.Styles.Save();
    }

    private static Style BuildParagraphStyle(
        string styleId,
        string styleName,
        string colorHex,
        string fontSizeHalfPoints,
        bool bold = false)
    {
        Style style = new() { Type = StyleValues.Paragraph, StyleId = styleId, CustomStyle = true };

        style.Append(new StyleName { Val = styleName });
        style.Append(new BasedOn { Val = "Normal" });
        style.Append(new UIPriority { Val = 1 });
        style.Append(new PrimaryStyle());

        StyleRunProperties runProps = new(
            new Color { Val = colorHex },
            new FontSize { Val = fontSizeHalfPoints });

        if (bold)

            runProps.Append(new Bold());

        style.Append(new StyleParagraphProperties(
            new SpacingBetweenLines
            {
                Before = "120", After = "120", Line = "300", LineRule = LineSpacingRuleValues.Auto
            }));

        style.Append(runProps);

        return style;
    }

    internal static void AddHeading(Body body, string text, int level)
    {
        body.AppendChild(new WpParagraph(
            new WpParagraphProperties(
                new ParagraphStyleId { Val = $"Heading{level}" }),
            new WpRun(new WpText(text) { Space = SpaceProcessingModeValues.Preserve })));
    }

    internal static void AddStyledParagraph(Body body, string text, string styleId)
    {
        body.AppendChild(new WpParagraph(
            new WpParagraphProperties(
                new ParagraphStyleId { Val = styleId }),
            new WpRun(new WpText(text) { Space = SpaceProcessingModeValues.Preserve })));
    }

    internal static void AddBullet(Body body, string text)
    {
        body.AppendChild(new WpParagraph(
            new WpParagraphProperties(
                new SpacingBetweenLines { After = "40" }),
            new WpRun(new WpText($"• {text}") { Space = SpaceProcessingModeValues.Preserve })));
    }

    internal static void AddSpacer(Body body, int count = 1)
    {
        for (int i = 0; i < count; i++)

            body.AppendChild(new WpParagraph(new WpRun(new WpText(string.Empty))));
    }

    internal static void AddPageBreak(Body body)
    {
        body.AppendChild(new WpParagraph(
            new WpRun(new WpBreak { Type = BreakValues.Page })));
    }

    internal static void AddCallout(Body body, string text, ConsultingDocxTemplateOptions options)
    {
        WpParagraph paragraph = new(
            new WpParagraphProperties(
                new WpShading { Val = ShadingPatternValues.Clear, Fill = options.AccentFillHex },
                new WpSpacingBetweenLines
                {
                    Before = "120", After = "120", Line = "280", LineRule = LineSpacingRuleValues.Auto
                }),
            new WpRun(
                new WpRunProperties(new Bold(), new Color { Val = options.SecondaryColorHex }),
                new WpText(text) { Space = SpaceProcessingModeValues.Preserve }));

        body.AppendChild(paragraph);
    }

    internal static void AddCodeBlock(Body body, string text, string language)
    {
        AddStyledParagraph(body, $"[{language}]", "Subtle");

        foreach (string line in text.Replace("\r\n", "\n").Split('\n'))
        {
            WpRun run = new(new WpText(line) { Space = SpaceProcessingModeValues.Preserve })
            {
                RunProperties = new WpRunProperties(
                    new RunFonts { Ascii = "Consolas" },
                    new FontSize { Val = "18" })
            };

            body.AppendChild(new WpParagraph(
                new WpParagraphProperties(
                    new WpShading { Val = ShadingPatternValues.Clear, Fill = "F4F6F6" }),
                run));
        }
    }

    internal static void AddKeyValueTable(Body body, IEnumerable<(string Key, string Value)> rows)
    {
        WpTable table = new();

        WpTableProperties props = new(
            new TableBorders(
                new WpTopBorder { Val = BorderValues.Single, Size = 8 },
                new WpBottomBorder { Val = BorderValues.Single, Size = 8 },
                new WpLeftBorder { Val = BorderValues.Single, Size = 8 },
                new WpRightBorder { Val = BorderValues.Single, Size = 8 },
                new WpInsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                new WpInsideVerticalBorder { Val = BorderValues.Single, Size = 6 }),
            new TableWidth { Width = "9000", Type = TableWidthUnitValues.Dxa });

        table.AppendChild(props);

        foreach ((string key, string value) in rows)
        {
            WpTableRow tr = new();

            tr.Append(
                BuildCell(key, true, "2800"),
                BuildCell(value, false, "6200"));

            table.Append(tr);
        }

        body.Append(table);
        AddSpacer(body);
    }

    private static WpTableCell BuildCell(string text, bool bold, string width)
    {
        WpRun run = new(new WpText(text) { Space = SpaceProcessingModeValues.Preserve });

        if (bold)

            run.RunProperties = new WpRunProperties(new Bold());

        return new WpTableCell(
            new WpTableCellProperties(
                new WpTableCellWidth { Type = TableWidthUnitValues.Dxa, Width = width }),
            new WpParagraph(
                new WpParagraphProperties(
                    new WpSpacingBetweenLines { Before = "80", After = "80" }),
                run));
    }

    internal static void AddImageToBody(
        MainDocumentPart mainPart,
        Body body,
        byte[] imageBytes,
        string imageName,
        long widthEmus,
        long heightEmus)
    {
        ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Png);

        using (MemoryStream stream = new(imageBytes))

            imagePart.FeedData(stream);

        string relationshipId = mainPart.GetIdOfPart(imagePart);

        Drawing drawing = new(
            new Inline(
                new Extent { Cx = widthEmus, Cy = heightEmus },
                new EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new DocProperties { Id = 1U, Name = imageName },
                new WpNonVisualGraphicFrameDrawingProperties(
                    new DrGraphicFrameLocks { NoChangeAspect = true }),
                new Graphic(
                    new GraphicData(
                        new DrPicture(
                            new DrNonVisualPictureProperties(
                                new DrNonVisualDrawingProperties { Id = 0U, Name = imageName },
                                new DrNonVisualPictureDrawingProperties()),
                            new DrBlipFill(
                                new DrBlip { Embed = relationshipId },
                                new DrStretch(new DrFillRectangle())),
                            new DrShapeProperties(
                                new Transform2D(
                                    new Offset { X = 0L, Y = 0L },
                                    new Extents { Cx = widthEmus, Cy = heightEmus }),
                                new PresetGeometry(new AdjustValueList()) { Preset = ShapeTypeValues.Rectangle }))
                    ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
            {
                DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U
            });

        body.AppendChild(new WpParagraph(new WpRun(drawing)));
    }
}

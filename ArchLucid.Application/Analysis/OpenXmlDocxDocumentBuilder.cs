using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DrBlip = DocumentFormat.OpenXml.Drawing.Blip;
using DrBlipFill = DocumentFormat.OpenXml.Drawing.BlipFill;
using DrFillRectangle = DocumentFormat.OpenXml.Drawing.FillRectangle;
using DrGraphicFrameLocks = DocumentFormat.OpenXml.Drawing.GraphicFrameLocks;
using DrNonVisualDrawingProperties = DocumentFormat.OpenXml.Drawing.NonVisualDrawingProperties;
using DrNonVisualPictureDrawingProperties = DocumentFormat.OpenXml.Drawing.NonVisualPictureDrawingProperties;
using DrNonVisualPictureProperties = DocumentFormat.OpenXml.Drawing.NonVisualPictureProperties;
using DrPicture = DocumentFormat.OpenXml.Drawing.Picture;
using DrRun = DocumentFormat.OpenXml.Drawing.Run;
using DrShapeProperties = DocumentFormat.OpenXml.Drawing.ShapeProperties;
using DrStretch = DocumentFormat.OpenXml.Drawing.Stretch;
using WpNonVisualGraphicFrameDrawingProperties = DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties;
using WpParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using WpParagraphProperties = DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties;
using WpRun = DocumentFormat.OpenXml.Wordprocessing.Run;
using WpRunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using WpText = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace ArchLucid.Application.Analysis;
public sealed class OpenXmlDocxDocumentBuilder : IDocxDocumentBuilder, IDisposable
{
    private readonly WordprocessingDocument _document;
    private readonly MemoryStream _stream;
    public OpenXmlDocxDocumentBuilder()
    {
        _stream = new MemoryStream();
        _document = WordprocessingDocument.Create(_stream, WordprocessingDocumentType.Document, true);
        MainPart = _document.AddMainDocumentPart();
        MainPart.Document = new Document(new Body());
        Body = MainPart.Document.Body!;
    }

    public void Dispose()
    {
        _document.Dispose();
        _stream.Dispose();
    }

    public Body Body { get; }
    public MainDocumentPart MainPart { get; }

    public void AddHeading(string text, int level)
    {
        ArgumentNullException.ThrowIfNull(text);
        Body.AppendChild(new WpParagraph(new WpParagraphProperties(new ParagraphStyleId { Val = $"Heading{level}" }), new WpRun(new WpText(text) { Space = SpaceProcessingModeValues.Preserve })));
    }

    public void AddParagraph(string text, bool bold = false)
    {
        ArgumentNullException.ThrowIfNull(text);
        WpRun run = new(new WpText(text) { Space = SpaceProcessingModeValues.Preserve });
        if (bold)
            run.RunProperties = new WpRunProperties(new Bold());
        Body.AppendChild(new WpParagraph(run));
    }

    public void AddBullet(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Body.AppendChild(new WpParagraph(new WpRun(new WpText($"• {text}") { Space = SpaceProcessingModeValues.Preserve })));
    }

    public void AddSpacer(int lines = 1)
    {
        for (int i = 0; i < lines; i++)
            Body.AppendChild(new WpParagraph(new WpRun(new WpText(string.Empty))));
    }

    public void AddMultilineParagraphs(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        foreach (string line in lines)
            AddParagraph(line);
    }

    public void AddCodeBlock(string text, string language)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(language);
        AddParagraph($"[{language}]");
        foreach (string line in text.Replace("\r\n", "\n").Split('\n'))
        {
            WpRun run = new(new WpText(line) { Space = SpaceProcessingModeValues.Preserve })
            {
                RunProperties = new WpRunProperties(new RunFonts { Ascii = "Consolas" })
            };
            Body.AppendChild(new WpParagraph(run));
        }
    }

    public void AddDiffSection(string title, IReadOnlyCollection<string> items)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(items);
        AddParagraph(title, true);
        if (items.Count == 0)
        {
            AddBullet("None");
            return;
        }

        foreach (string item in items)
            AddBullet(item);
    }

    public void AddImage(byte[] imageBytes, string imageName, long widthEmus, long heightEmus)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        ArgumentNullException.ThrowIfNull(imageName);
        ImagePart imagePart = MainPart.AddImagePart(ImagePartType.Png);
        using (MemoryStream stream = new(imageBytes))
            imagePart.FeedData(stream);
        string relationshipId = MainPart.GetIdOfPart(imagePart);
        Drawing drawing = new(new Inline(new Extent { Cx = widthEmus, Cy = heightEmus }, new EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L }, new DocProperties { Id = 1U, Name = imageName }, new WpNonVisualGraphicFrameDrawingProperties(new DrGraphicFrameLocks { NoChangeAspect = true }), new Graphic(new GraphicData(new DrPicture(new DrNonVisualPictureProperties(new DrNonVisualDrawingProperties { Id = 0U, Name = imageName }, new DrNonVisualPictureDrawingProperties()), new DrBlipFill(new DrBlip { Embed = relationshipId }, new DrStretch(new DrFillRectangle())), new DrShapeProperties(new Transform2D(new Offset { X = 0L, Y = 0L }, new Extents { Cx = widthEmus, Cy = heightEmus }), new PresetGeometry(new AdjustValueList()) { Preset = ShapeTypeValues.Rectangle }))) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })) { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U });
        Body.AppendChild(new WpParagraph(new DrRun(drawing)));
    }

    public byte[] Build()
    {
        MainPart.Document.Save();
        _document.Dispose();
        return _stream.ToArray();
    }
}
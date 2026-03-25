using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using DrPicture = DocumentFormat.OpenXml.Drawing.Picture;
using WpDrawing = DocumentFormat.OpenXml.Wordprocessing.Drawing;
using WpNonVisualGraphicFrameDrawingProperties =
    DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties;
using WpParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using WpRun = DocumentFormat.OpenXml.Wordprocessing.Run;

namespace ArchiForge.ArtifactSynthesis.Docx.Helpers;

/// <summary>Embeds PNG images into WordprocessingML (body or header).</summary>
public static class ImageHelper
{
    /// <summary>EMU: ~5.5&quot; wide at default DPI for placeholder diagrams.</summary>
    public const long DefaultDiagramWidthEmu = 5_500_000L;

    public const long DefaultDiagramHeightEmu = 3_000_000L;

    public static void AddPngToBody(
        WordprocessingDocument doc,
        Body body,
        byte[] imageBytes,
        string imageName = "Diagram",
        long widthEmu = DefaultDiagramWidthEmu,
        long heightEmu = DefaultDiagramHeightEmu)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(imageBytes);

        MainDocumentPart mainPart = doc.MainDocumentPart
                                    ?? throw new InvalidOperationException("Main document part is missing.");

        ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Png);
        using (MemoryStream s = new(imageBytes))
            imagePart.FeedData(s);

        string relationshipId = mainPart.GetIdOfPart(imagePart);
        WpDrawing drawing = CreateInlineDrawing(relationshipId, imageName, widthEmu, heightEmu);
        body.AppendChild(new WpParagraph(new WpRun(drawing)));
    }

    public static void AddPngToHeader(
        HeaderPart headerPart,
        byte[] imageBytes,
        string imageName = "Logo",
        long widthEmu = 990_000L,
        long heightEmu = 396_000L)
    {
        ArgumentNullException.ThrowIfNull(headerPart);
        ArgumentNullException.ThrowIfNull(imageBytes);

        ImagePart imagePart = headerPart.AddImagePart(ImagePartType.Png);
        using (MemoryStream s = new(imageBytes))
            imagePart.FeedData(s);

        string relationshipId = headerPart.GetIdOfPart(imagePart);
        WpDrawing drawing = CreateInlineDrawing(relationshipId, imageName, widthEmu, heightEmu);
        headerPart.Header.AppendChild(new WpParagraph(new WpRun(drawing)));
    }

    private static WpDrawing CreateInlineDrawing(
        string relationshipId,
        string imageName,
        long widthEmu,
        long heightEmu) =>
        new(
            new Inline(
                new Extent { Cx = widthEmu, Cy = heightEmu },
                new EffectExtent
                {
                    LeftEdge = 0L,
                    TopEdge = 0L,
                    RightEdge = 0L,
                    BottomEdge = 0L
                },
                new DocProperties
                {
                    Id = 1U,
                    Name = imageName
                },
                new WpNonVisualGraphicFrameDrawingProperties(
                    new GraphicFrameLocks { NoChangeAspect = true }),
                new Graphic(
                    new GraphicData(
                        new DrPicture(
                            new NonVisualPictureProperties(
                                new NonVisualDrawingProperties
                                {
                                    Id = 0U,
                                    Name = imageName
                                },
                                new NonVisualPictureDrawingProperties()),
                            new BlipFill(
                                new Blip { Embed = relationshipId },
                                new Stretch(new FillRectangle())),
                            new ShapeProperties(
                                new Transform2D(
                                    new Offset { X = 0L, Y = 0L },
                                    new Extents { Cx = widthEmu, Cy = heightEmu }),
                                new PresetGeometry(new AdjustValueList())
                                {
                                    Preset = ShapeTypeValues.Rectangle
                                }))
                    )
                    {
                        Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
                    }))
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U
            });
}

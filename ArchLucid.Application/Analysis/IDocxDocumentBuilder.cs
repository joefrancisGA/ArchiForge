using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using JetBrains.Annotations;

namespace ArchiForge.Application.Analysis;

public interface IDocxDocumentBuilder
{
    [UsedImplicitly]
    Body Body { get; }
    [UsedImplicitly]
    MainDocumentPart MainPart { get; }

    [UsedImplicitly]
    void AddHeading(string text, int level);
    [UsedImplicitly]
    void AddParagraph(string text, bool bold = false);
    [UsedImplicitly]
    void AddBullet(string text);
    [UsedImplicitly]
    void AddSpacer(int lines = 1);
    [UsedImplicitly]
    void AddMultilineParagraphs(string text);
    [UsedImplicitly]
    void AddCodeBlock(string text, string language);
    [UsedImplicitly]
    void AddDiffSection(string title, IReadOnlyCollection<string> items);
    [UsedImplicitly]
    void AddImage(byte[] imageBytes, string imageName, long widthEmus, long heightEmus);
}


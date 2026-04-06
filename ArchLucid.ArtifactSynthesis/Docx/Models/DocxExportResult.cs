namespace ArchiForge.ArtifactSynthesis.Docx.Models;

/// <summary>Binary DOCX payload returned from <see cref="IDocxExportService.ExportAsync"/>.</summary>
public class DocxExportResult
{
    /// <summary>Suggested download file name.</summary>
    public string FileName { get; set; } = null!;

    /// <summary>OpenXML document bytes.</summary>
    public byte[] Content { get; set; } = [];

    /// <summary>MIME type for <see cref="Content"/> (Word OpenXML).</summary>
    public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
}

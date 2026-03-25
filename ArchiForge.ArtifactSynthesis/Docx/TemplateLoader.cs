using System.Reflection;

namespace ArchiForge.ArtifactSynthesis.Docx;

public static class TemplateLoader
{
    private const string FileName = "architecture-template.docx";

    /// <summary>Writable copy suitable for opening with the Open XML SDK (read/write).</summary>
    public static MemoryStream OpenWritableTemplate()
    {
        byte[] bytes = TryLoadFromDisk() ?? BrandedArchitectureTemplateGenerator.CreateTemplateBytes();
        MemoryStream ms = new();
        ms.Write(bytes, 0, bytes.Length);
        ms.Position = 0;
        return ms;
    }

    private static byte[]? TryLoadFromDisk()
    {
        return (from dir in GetSearchDirectories() select Path.Combine(dir, "Docx", "Templates", FileName) into path where File.Exists(path) select File.ReadAllBytes(path)).FirstOrDefault();
    }

    private static IEnumerable<string> GetSearchDirectories()
    {
        yield return AppContext.BaseDirectory;

        string loc = Assembly.GetExecutingAssembly().Location;
        
        if (string.IsNullOrEmpty(loc)) yield break;
        
        string? bin = Path.GetDirectoryName(loc);
        if (!string.IsNullOrEmpty(bin))
            yield return bin;
    }
}

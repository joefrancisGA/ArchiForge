namespace ArchiForge.ArtifactSynthesis.Packaging;

public static class FileNameSanitizer
{
    public static string Sanitize(string fileName)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string sanitized = new string(fileName.Select(c => invalid.Contains(c) ? '_' : c).ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "artifact.txt" : sanitized;
    }
}

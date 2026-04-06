namespace ArchiForge.ArtifactSynthesis.Packaging;

/// <summary>
/// Produces file names safe for archives and for common client OSes.
/// <see cref="Path.GetInvalidFileNameChars"/> is OS-specific (e.g. <c>|</c> and <c>?</c> are valid on Linux but
/// invalid on Windows), so we union platform invalid chars with a Windows-style denylist for exports.
/// </summary>
public static class FileNameSanitizer
{
    private static readonly HashSet<char> InvalidChars = CreateInvalidCharSet();

    public static string Sanitize(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        string sanitized = new(fileName.Select(c => InvalidChars.Contains(c) ? '_' : c).ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "artifact.txt" : sanitized;
    }

    private static HashSet<char> CreateInvalidCharSet()
    {
        HashSet<char> set = new(Path.GetInvalidFileNameChars());

        // Invalid on Windows; often still present in CI (Linux) unless explicitly stripped.
        foreach (char c in "<>:\"/\\|?*")
        
            set.Add(c);
        

        return set;
    }
}

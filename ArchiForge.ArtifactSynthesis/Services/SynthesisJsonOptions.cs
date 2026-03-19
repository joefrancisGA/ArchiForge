using System.Text.Json;

namespace ArchiForge.ArtifactSynthesis.Services;

internal static class SynthesisJsonOptions
{
    public static readonly JsonSerializerOptions WriteIndented = new() { WriteIndented = true };
}

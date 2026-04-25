using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using ArchLucid.Contracts.ProductLearning;

namespace ArchLucid.Persistence.Coordination.ProductLearning.Planning;

/// <summary>Stable <see cref="Guid" /> values derived from scope + canonical keys (no randomness).</summary>
internal static class ImprovementThemeExtractionDeterministicIds
{
    internal static Guid ThemeId(ProductLearningScope scope, string canonicalKey)
    {
        return GuidFromUtf8(
            "59R.theme",
            scope.TenantId.ToString("N"),
            scope.WorkspaceId.ToString("N"),
            scope.ProjectId.ToString("N"),
            canonicalKey);
    }

    internal static Guid EvidenceId(Guid themeId, string discriminator, int sequence)
    {
        return GuidFromUtf8(
            "59R.evidence",
            themeId.ToString("N"),
            discriminator,
            sequence.ToString(CultureInfo.InvariantCulture));
    }

    private static Guid GuidFromUtf8(string purpose, params string[] segments)
    {
        StringBuilder builder = new();
        builder.Append(purpose);

        foreach (string segment in segments)
        {
            builder.Append('\u001e');
            builder.Append(segment);
        }

        byte[] utf8 = Encoding.UTF8.GetBytes(builder.ToString());

        using SHA256 sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(utf8);
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);

        return new Guid(guidBytes);
    }
}

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ArchiForge.Persistence.Evolution;

internal static class CandidateChangeSetDeterministicIds
{
    internal static Guid AggregateChangeSetId(Guid planId)
    {
        return GuidFromUtf8("60R.candidate", planId.ToString("N"), "aggregate");
    }

    internal static Guid StepSliceChangeSetId(Guid planId, int stepOrdinal)
    {
        return GuidFromUtf8(
            "60R.candidate",
            planId.ToString("N"),
            "step",
            stepOrdinal.ToString(CultureInfo.InvariantCulture));
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

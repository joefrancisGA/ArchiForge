using System.Security.Cryptography;
using System.Text;

namespace ArchLucid.Application.Bootstrap;

/// <summary>
///     Deterministic GUIDs for the Contoso demo authority FK chain (idempotent re-seed per
///     <see cref="ContosoRetailDemoIds" /> run row).
/// </summary>
internal static class AuthorityDemoChainIds
{
    internal static Guid Manifest(Guid authorityRunId)
    {
        return Derive("ArchLucid.Demo.Authority.Manifest", authorityRunId);
    }

    internal static Guid ContextSnapshot(Guid authorityRunId)
    {
        return Derive("ArchLucid.Demo.Authority.ContextSnapshot", authorityRunId);
    }

    internal static Guid GraphSnapshot(Guid authorityRunId)
    {
        return Derive("ArchLucid.Demo.Authority.GraphSnapshot", authorityRunId);
    }

    internal static Guid FindingsSnapshot(Guid authorityRunId)
    {
        return Derive("ArchLucid.Demo.Authority.FindingsSnapshot", authorityRunId);
    }

    internal static Guid DecisionTrace(Guid authorityRunId)
    {
        return Derive("ArchLucid.Demo.Authority.DecisionTrace", authorityRunId);
    }

    private static Guid Derive(string purpose, Guid authorityRunId)
    {
        StringBuilder builder = new();
        builder.Append(purpose);
        builder.Append('\u001e');
        builder.Append(authorityRunId.ToString("N"));

        byte[] utf8 = Encoding.UTF8.GetBytes(builder.ToString());

        using SHA256 sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(utf8);
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);

        return new Guid(guidBytes);
    }
}

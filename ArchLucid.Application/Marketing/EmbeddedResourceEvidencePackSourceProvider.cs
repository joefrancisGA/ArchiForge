using System.Reflection;

namespace ArchLucid.Application.Marketing;

/// <summary>
///     Default <see cref="IEvidencePackSourceProvider" /> — reads the canonical evidence-pack
///     source files from manifest resources embedded in <c>ArchLucid.Application.dll</c>
///     (see <c>ArchLucid.Application.csproj</c> &lt;EmbeddedResource&gt; entries with logical
///     name prefix <c>ArchLucid.Application.Marketing.EvidencePack.</c>).
/// </summary>
/// <remarks>
///     <para>
///         <b>Why embed instead of read the docs/ tree at runtime?</b> The endpoint must
///         work in three environments: dev (<c>dotnet run</c>), integration tests
///         (<c>WebApplicationFactory&lt;Program&gt;</c> where <c>ContentRootPath</c> resolves
///         to the API project, not the test bin dir), and published Docker images (where
///         the <c>docs/</c> tree is not shipped). Embedding the canonical source files at
///         build time is the only resolution that works in all three.
///     </para>
///     <para>
///         The order returned matches <see cref="OrderedZipNames" /> — that order IS the
///         canonical entry order, feeds the ETag, and is the order callers will see entries
///         when enumerating the ZIP.
///     </para>
/// </remarks>
public sealed class EmbeddedResourceEvidencePackSourceProvider : IEvidencePackSourceProvider
{
    /// <summary>Manifest resource name prefix the embedded resources use (LogicalName in the csproj).</summary>
    public const string ResourceNamePrefix = "ArchLucid.Application.Marketing.EvidencePack.";

    /// <summary>
    ///     Canonical evidence-pack entry order. Mirrors the list in
    ///     <c>docs/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_23_73_20.md</c> § Prompt 6
    ///     (DPA → SUBPROCESSORS → SLA → security.txt → CAIQ → SIG → Owner Sec Assessment →
    ///     Pen-test SoW → Audit Coverage Matrix). README.md is NOT in this list — the
    ///     <see cref="EvidencePackBuilder" /> auto-generates that and prepends it.
    /// </summary>
    public static readonly IReadOnlyList<string> OrderedZipNames =
    [
        "DPA-template.md",
        "SUBPROCESSORS.md",
        "SLA-summary.md",
        "security.txt",
        "CAIQ-Lite.md",
        "SIG-Core.md",
        "OWNER_SECURITY_ASSESSMENT_2026_Q2.md",
        "PEN_TEST_SOW_2026_Q2.md",
        "AUDIT_COVERAGE_MATRIX.md",
    ];

    private static readonly Assembly OwnerAssembly = typeof(EmbeddedResourceEvidencePackSourceProvider).Assembly;

    /// <inheritdoc />
    public Task<IReadOnlyList<EvidencePackEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        List<EvidencePackEntry> entries = new(OrderedZipNames.Count);

        foreach (string zipName in OrderedZipNames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            entries.Add(LoadEntry(zipName));
        }

        return Task.FromResult<IReadOnlyList<EvidencePackEntry>>(entries);
    }

    private static EvidencePackEntry LoadEntry(string zipName)
    {
        string resourceName = ResourceNamePrefix + zipName;
        using Stream? stream = OwnerAssembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Trust Center evidence-pack resource '{resourceName}' is not embedded in {OwnerAssembly.GetName().Name}. " +
                "Check the <EmbeddedResource> entries in ArchLucid.Application.csproj.");
        }

        using MemoryStream buffer = new(checked((int)stream.Length));
        stream.CopyTo(buffer);
        return new EvidencePackEntry(zipName, buffer.ToArray());
    }
}

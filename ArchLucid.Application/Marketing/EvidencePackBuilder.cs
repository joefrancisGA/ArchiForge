using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace ArchLucid.Application.Marketing;
/// <summary>
///     Default <see cref = "IEvidencePackBuilder"/> — assembles the canonical Trust Center
///     evidence-pack ZIP from an <see cref = "IEvidencePackSourceProvider"/>, prepends an
///     auto-generated <c>README.md</c>, and stamps a content-driven SHA-256 ETag.
/// </summary>
/// <remarks>
///     <para>
///         <b>Determinism.</b> The README is generated from the source entries with no
///         timestamp (only stable per-file SHA-256 fingerprints), and every ZIP entry's
///         last-write-time is pinned to <see cref = "DeterministicEntryTimestamp"/>. As a
///         result, the same source content produces byte-identical ZIPs across runs and
///         hosts, which keeps the <c>If-None-Match</c> 304 negotiation honest.
///     </para>
///     <para>
///         <b>ETag scope.</b> The ETag is computed over the source entries (NOT over the
///         README). That keeps the README purely descriptive and lets us extend it later
///         (e.g. add a build-id line) without invalidating the ETag for buyers who already
///         downloaded the same source content.
///     </para>
/// </remarks>
public sealed class EvidencePackBuilder(IEvidencePackSourceProvider sourceProvider, TimeProvider timeProvider) : IEvidencePackBuilder
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(sourceProvider, timeProvider);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Application.Marketing.IEvidencePackSourceProvider sourceProvider, System.TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(sourceProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        return (byte)0;
    }

    /// <summary>Pinned last-write-time used for every ZIP entry so the bytes are reproducible.</summary>
    public static readonly DateTimeOffset DeterministicEntryTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly IEvidencePackSourceProvider _sourceProvider = sourceProvider ?? throw new ArgumentNullException(nameof(sourceProvider));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    /// <inheritdoc/>
    public async Task<EvidencePackArtifact> BuildAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EvidencePackEntry> sourceEntries = await _sourceProvider.GetEntriesAsync(cancellationToken);
        if (sourceEntries is null || sourceEntries.Count == 0)
            throw new InvalidOperationException("Evidence-pack source provider returned no entries.");
        string etag = EvidencePackEtag.Compute(sourceEntries);
        EvidencePackEntry readmeEntry = new("README.md", BuildReadmeBytes(sourceEntries));
        List<EvidencePackEntry> finalEntries = new(sourceEntries.Count + 1)
        {
            readmeEntry
        };
        finalEntries.AddRange(sourceEntries);
        byte[] zipBytes = WriteDeterministicZip(finalEntries);
        return new EvidencePackArtifact(zipBytes, etag, "application/zip", _timeProvider.GetUtcNow());
    }

    private static byte[] BuildReadmeBytes(IReadOnlyList<EvidencePackEntry> sourceEntries)
    {
        StringBuilder readme = new();
        readme.AppendLine("# ArchLucid Trust Center evidence pack");
        readme.AppendLine();
        readme.AppendLine("This ZIP is the consolidated procurement-evidence bundle exposed by");
        readme.AppendLine("`GET /v1/marketing/trust-center/evidence-pack.zip`. Contents are sourced from");
        readme.AppendLine("the canonical files maintained in the ArchLucid repository — the same files");
        readme.AppendLine("linked from `docs/trust-center.md`.");
        readme.AppendLine();
        readme.AppendLine("> Status posture: this pack is **self-asserted documentation** plus");
        readme.AppendLine("> **in-flight engagement** artefacts. It is NOT a SOC 2 attestation and is NOT");
        readme.AppendLine("> a completed third-party penetration-test report. Read each file's own");
        readme.AppendLine("> status banner before redistributing.");
        readme.AppendLine();
        readme.AppendLine("## Contents");
        readme.AppendLine();
        readme.AppendLine("| File | SHA-256 (first 16 hex) | Notes |");
        readme.AppendLine("|------|------------------------|-------|");
        foreach (EvidencePackEntry entry in sourceEntries)
        {
            string fingerprint = ComputeShortFingerprint(entry.Content);
            string notes = DescribeEntry(entry.ZipName);
            readme.AppendLine($"| `{entry.ZipName}` | `{fingerprint}` | {notes} |");
        }

        readme.AppendLine();
        readme.AppendLine("## What is intentionally NOT included");
        readme.AppendLine();
        readme.AppendLine("- The **redacted** pen-test summary (`docs/security/pen-test-summaries/2026-Q2-REDACTED-SUMMARY.md`) — that artefact is V1.1-gated per `docs/PENDING_QUESTIONS.md` Q10. Only the SoW (`PEN_TEST_SOW_2026_Q2.md`) is in this pack.");
        readme.AppendLine("- The **PGP key** (`docs/security/PGP_KEY_GENERATION_RECIPE.md` is a recipe, not a key). Key publication is also V1.1.");
        readme.AppendLine();
        readme.AppendLine("## Verifying the pack content");
        readme.AppendLine();
        readme.AppendLine("The HTTP response `ETag` header is the SHA-256 fingerprint of the source-file");
        readme.AppendLine("contents (length-prefixed name + content per entry, in the order listed above).");
        readme.AppendLine("Re-download with `If-None-Match: <etag>` to receive `304 Not Modified` when the");
        readme.AppendLine("contents have not changed.");
        readme.AppendLine();
        return Encoding.UTF8.GetBytes(readme.ToString());
    }

    private static string ComputeShortFingerprint(byte[] content)
    {
        byte[] digest = SHA256.HashData(content);
        return Convert.ToHexString(digest, 0, 8).ToLowerInvariant();
    }

    private static string DescribeEntry(string zipName)
    {
        return zipName switch
        {
            "DPA-template.md" => "Data Processing Agreement template (`docs/go-to-market/DPA_TEMPLATE.md`).",
            "SUBPROCESSORS.md" => "Subprocessors register (`docs/go-to-market/SUBPROCESSORS.md`).",
            "SLA-summary.md" => "SLA summary (`docs/go-to-market/SLA_SUMMARY.md`).",
            "security.txt" => "RFC 9116 security contact file (`archlucid-ui/public/.well-known/security.txt`).",
            "CAIQ-Lite.md" => "CAIQ Lite pre-fill 2026 (`docs/security/CAIQ_LITE_2026.md`).",
            "SIG-Core.md" => "SIG Core pre-fill 2026 (`docs/security/SIG_CORE_2026.md`).",
            "OWNER_SECURITY_ASSESSMENT_2026_Q2.md" => "Owner-led security self-assessment (owner-conducted, not third-party audited).",
            "PEN_TEST_SOW_2026_Q2.md" => "2026-Q2 pen-test Statement of Work (engagement in flight).",
            "AUDIT_COVERAGE_MATRIX.md" => "Mapping of state-changing workflows to durable audit signals.",
            _ => "(see file)."
        };
    }

    private static byte[] WriteDeterministicZip(IReadOnlyList<EvidencePackEntry> entries)
    {
        using MemoryStream ms = new();
        using (ZipArchive archive = new(ms, ZipArchiveMode.Create, true))
        {
            foreach (EvidencePackEntry entry in entries)
            {
                ZipArchiveEntry zipEntry = archive.CreateEntry(entry.ZipName, CompressionLevel.Optimal);
                zipEntry.LastWriteTime = DeterministicEntryTimestamp;
                using Stream writer = zipEntry.Open();
                writer.Write(entry.Content, 0, entry.Content.Length);
            }
        }

        return ms.ToArray();
    }
}
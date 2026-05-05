using ArchLucid.Application.Marketing;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Marketing;

[Trait("Category", "Unit")]
public sealed class EmbeddedResourceEvidencePackSourceProviderTests
{
    [SkippableFact]
    public async Task GetEntriesAsync_ReturnsEveryCanonicalEntryInOrderWithNonEmptyContent()
    {
        EmbeddedResourceEvidencePackSourceProvider sut = new();

        IReadOnlyList<EvidencePackEntry> entries = await sut.GetEntriesAsync(CancellationToken.None);

        entries.Select(e => e.ZipName)
            .Should()
            .Equal(EmbeddedResourceEvidencePackSourceProvider.OrderedZipNames);

        foreach (EvidencePackEntry entry in entries)
        {
            entry.Content.Should().NotBeNullOrEmpty(because: $"{entry.ZipName} should be a non-empty embedded resource");
        }
    }

    [SkippableFact]
    public void OrderedZipNames_ContainsTheNineExpectedProcurementArtefacts()
    {
        IReadOnlyList<string> ordered = EmbeddedResourceEvidencePackSourceProvider.OrderedZipNames;

        ordered.Should().Equal("DPA-template.md", "SUBPROCESSORS.md", "SLA-summary.md", "security.txt", "CAIQ-Lite.md", "SIG-Core.md",
            "OWNER_SECURITY_ASSESSMENT_2026_Q2.md", "PEN_TEST_SOW_2026_Q2.md", "AUDIT_COVERAGE_MATRIX.md");
    }

    [SkippableFact]
    public void OrderedZipNames_DoesNotIncludeStopAndAskBoundaryArtefacts()
    {
        IReadOnlyList<string> ordered = EmbeddedResourceEvidencePackSourceProvider.OrderedZipNames;

        ordered.Should().NotContain(name => name.Contains("REDACTED", StringComparison.OrdinalIgnoreCase),
            because: "the redacted pen-test summary is V1.1-gated per PENDING_QUESTIONS Q10.");

        ordered.Should().NotContain(name => name.Contains("PGP", StringComparison.OrdinalIgnoreCase),
            because: "the PGP key is V1.1-gated.");
    }
}

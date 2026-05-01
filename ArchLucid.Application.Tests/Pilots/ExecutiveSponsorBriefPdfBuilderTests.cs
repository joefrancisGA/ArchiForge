using ArchLucid.Application.Pilots;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Pilots;

[Trait("Suite", "Core")]
public sealed class ExecutiveSponsorBriefPdfBuilderTests
{
    [SkippableFact]
    public void Build_returns_pdf_magic_bytes()
    {
        ExecutiveSponsorBriefPdfBuilder sut = new();

        byte[] pdf = sut.Build("# Title\n\nBody.");

        pdf.Length.Should().BeGreaterThan(200);
        ReadOnlySpan<byte> head = pdf.AsSpan(0, 5);
        head.SequenceEqual("%PDF-"u8).Should().BeTrue();
    }
}

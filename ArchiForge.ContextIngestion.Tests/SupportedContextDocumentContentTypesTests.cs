using FluentAssertions;

namespace ArchiForge.ContextIngestion.Tests;

[Trait("Suite", "Core")]
public sealed class SupportedContextDocumentContentTypesTests
{
    [Theory]
    [InlineData("text/plain", true)]
    [InlineData("TEXT/PLAIN", true)]
    [InlineData("text/markdown", true)]
    [InlineData("application/pdf", false)]
    [InlineData("", false)]
    public void IsSupported_MatchesCanonicalList(string contentType, bool expected) =>
        SupportedContextDocumentContentTypes.IsSupported(contentType).Should().Be(expected);

    [Fact]
    public void All_AlignsWithPlainTextParserExpectations() =>
        SupportedContextDocumentContentTypes.All.Should().Contain(["text/plain", "text/markdown"]);
}

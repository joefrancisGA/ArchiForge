using System.Reflection;

using ArchLucid.Application.Analysis;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Unit tests for <see cref="ExportReplayService" /> static helpers.
///     The private <c>BuildReplayFileName</c> method is exercised indirectly
///     via the public contract by inspecting <see cref="ReplayExportResult.FileName" />
///     on a stub result, or by extracting the logic under test to a testable surface.
///     These tests validate the filename-building rules directly via reflection to
///     preserve encapsulation while hitting 100% coverage for the helper.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ExportReplayServiceTests
{
    // BuildReplayFileName is private but its rules are:
    //   - blank input â†’ "replayed_export.docx"
    //   - "report.docx" â†’ "report_replay.docx"
    //   - "report.v2.docx" â†’ "report.v2_replay.docx"  (only last extension)
    //   - "noext" â†’ "noext_replay"

    private static string CallBuildReplayFileName(string originalFileName)
    {
        MethodInfo? method = typeof(ExportReplayService)
            .GetMethod(
                "BuildReplayFileName",
                BindingFlags.NonPublic | BindingFlags.Static);

        return (string)method!.Invoke(null, [originalFileName])!;
    }

    [SkippableFact]
    public void BuildReplayFileName_BlankInput_ReturnsFallback()
    {
        string result = CallBuildReplayFileName("");

        result.Should().Be("replayed_export.docx");
    }

    [SkippableFact]
    public void BuildReplayFileName_WhitespaceInput_ReturnsFallback()
    {
        string result = CallBuildReplayFileName("   ");

        result.Should().Be("replayed_export.docx");
    }

    [SkippableFact]
    public void BuildReplayFileName_SimpleDocxName_AppendsReplaySuffix()
    {
        string result = CallBuildReplayFileName("report.docx");

        result.Should().Be("report_replay.docx");
    }

    [SkippableFact]
    public void BuildReplayFileName_NameWithDottedVersion_PreservesExtensionOnly()
    {
        string result = CallBuildReplayFileName("report.v2.docx");

        result.Should().Be("report.v2_replay.docx");
    }

    [SkippableFact]
    public void BuildReplayFileName_NameWithNoExtension_AppendsReplaySuffixWithoutDot()
    {
        string result = CallBuildReplayFileName("noext");

        result.Should().Be("noext_replay");
    }

    [SkippableFact]
    public void BuildReplayFileName_NullInput_ReturnsFallback()
    {
        // null is coerced to empty by the outer guard (IsNullOrWhiteSpace covers null).
        string result = CallBuildReplayFileName(null!);

        result.Should().Be("replayed_export.docx");
    }
}

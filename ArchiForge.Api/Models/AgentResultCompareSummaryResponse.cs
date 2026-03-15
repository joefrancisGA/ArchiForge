using ArchiForge.Application.Diffs;

namespace ArchiForge.Api.Models;

public sealed class AgentResultCompareSummaryResponse
{
    public string Format { get; set; } = "markdown";

    public string Summary { get; set; } = string.Empty;

    public AgentResultDiffResult Diff { get; set; } = new();
}

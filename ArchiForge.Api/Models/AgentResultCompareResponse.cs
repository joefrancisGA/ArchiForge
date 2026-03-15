using ArchiForge.Application.Diffs;

namespace ArchiForge.Api.Models;

public sealed class AgentResultCompareResponse
{
    public AgentResultDiffResult Diff { get; set; } = new();
}

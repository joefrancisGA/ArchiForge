using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Metadata;

namespace ArchLucid.Application;
public sealed record GetRunResult(ArchitectureRun Run, IReadOnlyList<AgentTask> Tasks, IReadOnlyList<AgentResult> Results)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(Run, Tasks, Results);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Contracts.Metadata.ArchitectureRun Run, System.Collections.Generic.IReadOnlyList<ArchLucid.Contracts.Agents.AgentTask> Tasks, System.Collections.Generic.IReadOnlyList<ArchLucid.Contracts.Agents.AgentResult> Results)
    {
        ArgumentNullException.ThrowIfNull(Run);
        ArgumentNullException.ThrowIfNull(Tasks);
        ArgumentNullException.ThrowIfNull(Results);
        return (byte)0;
    }
}
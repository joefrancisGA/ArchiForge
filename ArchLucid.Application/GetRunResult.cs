using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Metadata;

namespace ArchLucid.Application;

public sealed record GetRunResult(ArchitectureRun Run, IReadOnlyList<AgentTask> Tasks, IReadOnlyList<AgentResult> Results);

using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Core.Configuration;

/// <summary>Bound from <c>AgentExecution</c> configuration (host-level agent execution mode).</summary>
[ExcludeFromCodeCoverage(Justification = "Configuration binding DTO with no logic.")]
public sealed class AgentExecutionOptions
{
    public const string SectionName = "AgentExecution";

    /// <summary><c>Simulator</c> (default) or <c>Real</c> (Azure OpenAI completion path).</summary>
    public string Mode
    {
        get;
        set;
    } = "Simulator";
}

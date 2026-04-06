using System.ComponentModel.DataAnnotations;

using ArchiForge.Contracts.Common;

namespace ArchiForge.Contracts.Agents;

/// <summary>
/// A unit of work assigned to a specific agent type as part of an architecture run.
/// Each task represents one agent's mandate: what it must evaluate or propose,
/// which tools it may use, and which evidence sources it may consult.
/// </summary>
public sealed class AgentTask
{
    /// <summary>Unique task identifier, generated at creation time.</summary>
    [Required]
    public string TaskId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Identifier of the run this task belongs to.</summary>
    [Required]
    public string RunId { get; set; } = string.Empty;

    /// <summary>Type of agent responsible for completing this task.</summary>
    [Required]
    public AgentType AgentType { get; set; }

    /// <summary>
    /// Optional override for handler dispatch (e.g. <c>custom-risk</c>). When null or empty, <see cref="AgentType"/> maps via <see cref="Common.AgentTypeKeys.FromEnum"/>.
    /// </summary>
    public string? AgentTypeKey { get; set; }

    /// <summary>Human-readable description of what the agent must analyse or propose.</summary>
    [Required]
    public string Objective { get; set; } = string.Empty;

    /// <summary>Current lifecycle status of this task.</summary>
    [Required]
    public AgentTaskStatus Status { get; set; } = AgentTaskStatus.Created;

    /// <summary>UTC timestamp when the task was created.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp when the task reached a terminal state. <see langword="null"/> while still in progress.</summary>
    public DateTime? CompletedUtc { get; set; }

    /// <summary>
    /// Optional reference to the evidence bundle the agent should use.
    /// <see langword="null"/> when the agent should use the run-level evidence package directly.
    /// </summary>
    public string? EvidenceBundleRef { get; set; }

    /// <summary>Tool identifiers the agent is permitted to invoke (empty = unrestricted).</summary>
    public List<string> AllowedTools { get; set; } = [];

    /// <summary>Evidence source identifiers the agent may consult (empty = unrestricted).</summary>
    public List<string> AllowedSources { get; set; } = [];
}

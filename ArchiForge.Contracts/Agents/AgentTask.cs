using System.ComponentModel.DataAnnotations;
using ArchiForge.Contracts.Common;

namespace ArchiForge.Contracts.Agents;

public sealed class AgentTask
{
    [Required]
    public string TaskId { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string RunId { get; set; } = string.Empty;

    [Required]
    public AgentType AgentType { get; set; }

    [Required]
    public string Objective { get; set; } = string.Empty;

    [Required]
    public AgentTaskStatus Status { get; set; } = AgentTaskStatus.Created;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedUtc { get; set; }

    public string? EvidenceBundleRef { get; set; }

    public List<string> AllowedTools { get; set; } = [];

    public List<string> AllowedSources { get; set; } = [];
}

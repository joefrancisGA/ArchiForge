using ArchiForge.Contracts.Agents;

namespace ArchiForge.Contracts.Common;

/// <summary>Stable string keys for <see cref="IAgentHandler"/> registration and optional <see cref="Agents.AgentTask.AgentTypeKey"/> dispatch.</summary>
public static class AgentTypeKeys
{
    /// <summary>Topology agent role.</summary>
    public const string Topology = "topology";

    /// <summary>Cost agent role.</summary>
    public const string Cost = "cost";

    /// <summary>Compliance agent role.</summary>
    public const string Compliance = "compliance";

    /// <summary>Critic agent role.</summary>
    public const string Critic = "critic";

    /// <summary>Maps a persisted <see cref="AgentType"/> enum to its canonical dispatch key.</summary>
    public static string FromEnum(AgentType agentType) =>
        agentType switch
        {
            AgentType.Topology => Topology,
            AgentType.Cost => Cost,
            AgentType.Compliance => Compliance,
            AgentType.Critic => Critic,
            _ => throw new ArgumentOutOfRangeException(nameof(agentType), agentType, "Unknown agent type.")
        };

    /// <summary>Resolves the handler lookup key: explicit <see cref="AgentTask.AgentTypeKey"/> wins, else enum mapping.</summary>
    public static string ResolveDispatchKey(AgentTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (!string.IsNullOrWhiteSpace(task.AgentTypeKey))
        {
            return task.AgentTypeKey.Trim();
        }

        return FromEnum(task.AgentType);
    }

    /// <summary>When <paramref name="agentTypeKey"/> matches a built-in key, returns the enum; otherwise <see langword="null"/>.</summary>
    public static AgentType? TryMapToEnum(string agentTypeKey)
    {
        if (string.IsNullOrWhiteSpace(agentTypeKey))
        {
            return null;
        }

        string k = agentTypeKey.Trim();

        if (string.Equals(k, Topology, StringComparison.OrdinalIgnoreCase))
        {
            return AgentType.Topology;
        }

        if (string.Equals(k, Cost, StringComparison.OrdinalIgnoreCase))
        {
            return AgentType.Cost;
        }

        if (string.Equals(k, Compliance, StringComparison.OrdinalIgnoreCase))
        {
            return AgentType.Compliance;
        }

        if (string.Equals(k, Critic, StringComparison.OrdinalIgnoreCase))
        {
            return AgentType.Critic;
        }

        return null;
    }

    /// <summary>Sort order for stable batch execution (lexicographic on keys).</summary>
    public static int CompareDispatchKeys(string a, string b) =>
        string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
}

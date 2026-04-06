namespace ArchiForge.Contracts.Common;

/// <summary>Identifies the specialized role of an agent within the decision pipeline.</summary>
public enum AgentType
{
    /// <summary>Proposes service topology, relationships, and patterns.</summary>
    Topology = 1,
    /// <summary>Estimates and validates cost implications of the topology.</summary>
    Cost = 2,
    /// <summary>Evaluates governance and security-control compliance.</summary>
    Compliance = 3,
    /// <summary>Reviews and challenges proposals from other agents (peer evaluation).</summary>
    Critic = 4
}

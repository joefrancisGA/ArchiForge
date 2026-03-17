using ArchiForge.Contracts.Decisions;

namespace ArchiForge.Api.Models;

public sealed class DecisionNodeResponse
{
    public List<DecisionNode> Decisions { get; set; } = [];
}


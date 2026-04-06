using System.Diagnostics.CodeAnalysis;

using ArchiForge.Contracts.Decisions;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class DecisionNodeResponse
{
    public List<DecisionNode> Decisions { get; set; } = [];
}

